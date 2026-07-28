using System.Collections.Concurrent;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobPhotoGallery.Models;
using BlobPhotoGallery.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;

namespace BlobPhotoGallery.Services;

public sealed class GalleryCatalog
{
    private readonly GalleryOptions _options;
    private readonly BlobContainerClient _container;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<GalleryCatalog> _logger;
    private readonly ConcurrentDictionary<string, GalleryAlbum> _albums = new(StringComparer.OrdinalIgnoreCase);

    public GalleryCatalog(IOptions<GalleryOptions> options, IWebHostEnvironment environment, ILogger<GalleryCatalog> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
        _container = new BlobContainerClient(CreateContainerUri());
    }

    public IReadOnlyList<GallerySummary> Albums
    {
        get
        {
            var albums = _albums.Values
                .OrderBy(album => album.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            var mainAlbum = albums.FirstOrDefault(album => album.Slug.StartsWith("main-", StringComparison.OrdinalIgnoreCase));

            if (mainAlbum is not null)
            {
                albums.Remove(mainAlbum);
                albums.Insert(0, mainAlbum);
            }

            return albums
                .Select(album => new GallerySummary(
                    album.Slug,
                    GetDisplayName(album),
                    album.CoverThumbnailName,
                    album.Photos.Count,
                    ReferenceEquals(album, mainAlbum)))
                .ToList();
        }
    }

    public GalleryAlbum? Find(string slug)
    {
        var album = _albums.GetValueOrDefault(slug);
        return album is null ? null : album with { Name = GetDisplayName(album) };
    }

    public string? GetThumbnailPath(string albumSlug, string thumbnailName)
    {
        var album = Find(albumSlug);
        if (album is null || Path.GetFileName(thumbnailName) != thumbnailName) return null;
        var isPhoto = album.Photos.Any(photo => photo.ThumbnailName == thumbnailName);
        if (!isPhoto && album.CoverThumbnailName != thumbnailName) return null;
        return Path.Combine(GetAlbumCachePath(albumSlug), thumbnailName);
    }

    public Uri GetPhotoUri(string blobName)
    {
        var escapedPath = string.Join('/', blobName.Split('/').Select(Uri.EscapeDataString));
        var baseUrl = _options.ContainerUrl.TrimEnd('/');
        var sas = _options.SharedAccessToken.TrimStart('?');
        return new Uri($"{baseUrl}/{escapedPath}{(string.IsNullOrWhiteSpace(sas) ? "" : $"?{sas}")}");
    }

    public async Task LoadCacheAsync(CancellationToken cancellationToken)
    {
        _albums.Clear();
        var cacheRoot = GetCacheRoot();
        if (!Directory.Exists(cacheRoot))
        {
            _logger.LogWarning("Thumbnail cache {CachePath} does not exist. Run the application with --generate-thumbnails first.", cacheRoot);
            return;
        }

        foreach (var albumCachePath in Directory.EnumerateDirectories(cacheRoot, "album-*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var album = await ReadManifestAsync(albumCachePath, cancellationToken);
            if (album is not null) _albums[album.Slug] = album;
            else _logger.LogWarning("Thumbnail cache folder {CachePath} does not contain a valid manifest.", albumCachePath);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(GetCacheRoot());
        var grouped = new Dictionary<string, List<BlobItem>>(StringComparer.OrdinalIgnoreCase);
        var rootImages = new Dictionary<string, BlobItem>(StringComparer.OrdinalIgnoreCase);
        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.Metadata, cancellationToken: cancellationToken))
        {
            var slash = blob.Name.IndexOf('/');
            if (slash < 0)
            {
                if (IsJpeg(blob.Name)) rootImages[blob.Name] = blob;
                continue;
            }
            if (slash == 0 || slash == blob.Name.Length - 1 || blob.Name[(slash + 1)..].Contains('/') || !IsJpeg(blob.Name)) continue;
            var folder = blob.Name[..slash];
            if (!grouped.TryGetValue(folder, out var items)) grouped[folder] = items = [];
            items.Add(blob);
        }

        foreach (var (folder, blobs) in grouped)
        {
            var albumCachePath = GetAlbumCachePath(folder);
            if (Directory.Exists(albumCachePath))
            {
                var cachedAlbum = await ReadManifestAsync(albumCachePath, cancellationToken);
                if (cachedAlbum is not null) _albums[folder] = cachedAlbum;
                else _logger.LogWarning("Thumbnail cache folder {CachePath} exists without a valid manifest; delete it to regenerate the album.", albumCachePath);
                continue;
            }

            var temporaryPath = $"{albumCachePath}.tmp-{Guid.NewGuid():N}";
            Directory.CreateDirectory(temporaryPath);
            var photos = new List<GalleryPhoto>();
            try
            {
                foreach (var blob in blobs)
                {
                    try { photos.Add(await CachePhotoAsync(blob, temporaryPath, cancellationToken)); }
                    catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException)
                    {
                        // Ignore an incorrectly named or damaged image without hiding the rest of the album.
                    }
                }
                photos.Sort((left, right) => left.TakenAt.CompareTo(right.TakenAt));
                string? coverName = null;
                if (rootImages.TryGetValue($"{folder}.jpg", out var cover))
                    coverName = await CacheCoverAsync(cover, temporaryPath, cancellationToken);
                var album = new GalleryAlbum(folder, Humanize(folder), coverName, photos);
                await WriteManifestAsync(temporaryPath, album, cancellationToken);
                Directory.Move(temporaryPath, albumCachePath);
                _albums[folder] = album;
            }
            catch
            {
                Directory.Delete(temporaryPath, recursive: true);
                throw;
            }
        }
    }

    private async Task<GalleryPhoto> CachePhotoAsync(BlobItem blob, string albumCachePath, CancellationToken cancellationToken)
    {
        var cacheName = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{blob.Name}:{blob.Properties.ETag}"))) + ".jpg";
        var path = Path.Combine(albumCachePath, cacheName);
        await using var source = await _container.GetBlobClient(blob.Name).OpenReadAsync(cancellationToken: cancellationToken);
        using var image = await Image.LoadAsync(source, cancellationToken);
        var takenAt = ReadTakenAt(image.Metadata.ExifProfile) ?? blob.Properties.LastModified ?? DateTimeOffset.MinValue;
        var width = image.Width;
        var height = image.Height;

        await SaveThumbnailAsync(image, path, cancellationToken);
        return new GalleryPhoto(blob.Name, cacheName, takenAt, width, height);
    }

    private async Task<string> CacheCoverAsync(BlobItem blob, string albumCachePath, CancellationToken cancellationToken)
    {
        const string cacheName = "cover.jpg";
        await using var source = await _container.GetBlobClient(blob.Name).OpenReadAsync(cancellationToken: cancellationToken);
        using var image = await Image.LoadAsync(source, cancellationToken);
        await SaveThumbnailAsync(image, Path.Combine(albumCachePath, cacheName), cancellationToken);
        return cacheName;
    }

    private async Task SaveThumbnailAsync(Image image, string path, CancellationToken cancellationToken)
    {
        image.Mutate(context => context.AutoOrient().Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(_options.ThumbnailWidth, _options.ThumbnailWidth)
        }));
        await image.SaveAsJpegAsync(path, cancellationToken);
    }

    private static async Task<GalleryAlbum?> ReadManifestAsync(string cachePath, CancellationToken cancellationToken)
    {
        var path = Path.Combine(cachePath, "album.json");
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<GalleryAlbum>(stream, cancellationToken: cancellationToken);
    }

    private static async Task WriteManifestAsync(string cachePath, GalleryAlbum album, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(Path.Combine(cachePath, "album.json"));
        await JsonSerializer.SerializeAsync(stream, album, cancellationToken: cancellationToken);
    }

    private static DateTimeOffset? ReadTakenAt(ExifProfile? profile)
    {
        IExifValue<string>? exifValue = null;
        if (profile is not null) profile.TryGetValue(ExifTag.DateTimeOriginal, out exifValue);
        var value = exifValue?.Value;
        return DateTime.TryParseExact(value, "yyyy:MM:dd HH:mm:ss", null,
            System.Globalization.DateTimeStyles.AssumeLocal, out var date) ? date : null;
    }

    private Uri CreateContainerUri()
    {
        var token = _options.SharedAccessToken.TrimStart('?');
        return new Uri($"{_options.ContainerUrl.TrimEnd('/')}{(string.IsNullOrWhiteSpace(token) ? "" : $"?{token}")}");
    }

    private string GetCacheRoot() => Path.IsPathRooted(_options.ThumbnailCachePath)
        ? _options.ThumbnailCachePath
        : Path.Combine(_environment.ContentRootPath, _options.ThumbnailCachePath);

    private string GetAlbumCachePath(string albumSlug) =>
        Path.Combine(GetCacheRoot(), $"album-{Uri.EscapeDataString(albumSlug)}");

    private static bool IsJpeg(string name) => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
    private static string GetDisplayName(GalleryAlbum album) => album.Slug.StartsWith("main-", StringComparison.OrdinalIgnoreCase)
        ? Humanize(album.Slug["main-".Length..])
        : album.Name;
    private static string Humanize(string folder) => System.Globalization.CultureInfo.CurrentCulture.TextInfo
        .ToTitleCase(Uri.UnescapeDataString(folder).Replace('-', ' ').Replace('_', ' '));
}
