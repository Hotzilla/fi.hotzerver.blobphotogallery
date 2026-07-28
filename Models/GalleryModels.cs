namespace BlobPhotoGallery.Models;

public sealed record GallerySummary(string Slug, string Name, string? CoverThumbnailName, int PhotoCount, bool IsMain);

public sealed record GalleryPhoto(
    string BlobName,
    string ThumbnailName,
    DateTimeOffset TakenAt,
    int Width,
    int Height);

public sealed record GalleryAlbum(string Slug, string Name, string? CoverThumbnailName, IReadOnlyList<GalleryPhoto> Photos);
