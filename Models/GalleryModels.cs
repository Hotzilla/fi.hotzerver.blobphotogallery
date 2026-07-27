namespace BlobPhotoGallery.Models;

public sealed record GallerySummary(string Slug, string Name, GalleryPhoto? Cover, int PhotoCount);

public sealed record GalleryPhoto(
    string BlobName,
    string ThumbnailName,
    DateTimeOffset TakenAt,
    int Width,
    int Height);

public sealed record GalleryAlbum(string Slug, string Name, IReadOnlyList<GalleryPhoto> Photos);
