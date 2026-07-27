namespace BlobPhotoGallery.Options;

public sealed class GalleryOptions
{
    public const string SectionName = "Gallery";
    public string AccessKey { get; set; } = "change-me";
    public string ContainerUrl { get; set; } = "https://account.blob.core.windows.net/photos";
    public string SharedAccessToken { get; set; } = "";
    public string ThumbnailCachePath { get; set; } = "thumbnail-cache";
    public int ThumbnailWidth { get; set; } = 640;
}
