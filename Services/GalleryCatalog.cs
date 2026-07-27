using System.Collections.Concurrent;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobPhotoGallery.Models;
using BlobPhotoGallery.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace BlobPhotoGallery.Services;

public sealed class GalleryCatalog
{
    private readonly GalleryOptions _options;
    private readonly BlobContainerClient _container;
    private readonly IWebHostEnvironment _environment;
    private readonly ConcurrentDictionary<string, GalleryAlbum> _albums = new(StringComparer.OrdinalIgnoreCase);

    public GalleryCatalog(IOptions<GalleryOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
        _container = new BlobContainerClient(CreateContainerUri());
    }

    public IReadOnlyList<GallerySummary> Albums => _albums.Values
        .OrderBy(album => album.Name, StringComparer.CurrentCultureIgnoreCase)
        .Select(album => new GallerySummary(album.Slug, album.Name, album.Photos.FirstOrDefault(), album.Photos.Count))
        .ToList();

    public GalleryAlbum? Find(string slug) => _albums.GetValueOrDefault(slug);

    public string GetThumbnailPath(string thumbnailName) =>
        Path.Combine(GetCacheRoot(), thumbnailName);

    public Uri GetPhotoUri(string blobName)
    {
        var escapedPath = string.Join('/', blobName.Split('/').Select(Uri.EscapeDataString));
        var baseUrl = _options.ContainerUrl.TrimEnd('/');
        var sas = _options.SharedAccessToken.TrimStart('?');
        return new Uri($"{baseUrl}/{escapedPath}{(string.IsNullOrWhiteSpace(sas) ? "" : $"?{sas}")}");
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(GetCacheRoot());
        var grouped = new Dictionary<string, List<BlobItem>>(StringComparer.OrdinalIgnoreCase);
        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.Metadata, cancellationToken: cancellationToken))
        {
            var slash = blob.Name.IndexOf('/');
            if (slash <= 0 || slash == blob.Name.Length - 1 || !IsJpeg(blob.Name)) continue;
            var folder = blob.Name[..slash];
            if (!grouped.TryGetValue(folder, out var items)) grouped[folder] = items = [];
            items.Add(blob);
        }

        foreach (var (folder, blobs) in grouped)
        {
            var photos = new List<GalleryPhoto>();
            foreach (var blob in blobs)
            {
                try { photos.Add(await CachePhotoAsync(blob, cancellationToken)); }
                catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException)
                {
                    // Ignore an incorrectly named or damaged image without hiding the rest of the album.
                }
            }
            photos.Sort((left, right) => left.TakenAt.CompareTo(right.TakenAt));
            _albums[folder] = new GalleryAlbum(folder, Humanize(folder), photos);
        }
    }

    private async Task<GalleryPhoto> CachePhotoAsync(BlobItem blob, CancellationToken cancellationToken)
    {
        var cacheName = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{blob.Name}:{blob.Properties.ETag}"))) + ".jpg";
        var path = GetThumbnailPath(cacheName);
        await using var source = await _container.GetBlobClient(blob.Name).OpenReadAsync(cancellationToken: cancellationToken);
        using var image = await Image.LoadAsync(source, cancellationToken);
        var takenAt = ReadTakenAt(image.Metadata.ExifProfile) ?? blob.Properties.LastModified ?? DateTimeOffset.MinValue;
        var width = image.Width;
        var height = image.Height;

        if (!File.Exists(path))
        {
            image.Mutate(context => context.AutoOrient().Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(_options.ThumbnailWidth, _options.ThumbnailWidth)
            }));
            await image.SaveAsJpegAsync(path, cancellationToken);
        }
        return new GalleryPhoto(blob.Name, cacheName, takenAt, width, height);
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

    private static bool IsJpeg(string name) => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
    private static string Humanize(string folder) => System.Globalization.CultureInfo.CurrentCulture.TextInfo
        .ToTitleCase(Uri.UnescapeDataString(folder).Replace('-', ' ').Replace('_', ' '));
}
