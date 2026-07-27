using BlobPhotoGallery.Models;
using BlobPhotoGallery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlobPhotoGallery.Pages;

public sealed class IndexModel(GalleryCatalog catalog) : PageModel
{
    public IReadOnlyList<GallerySummary> Albums { get; private set; } = [];
    public void OnGet() => Albums = catalog.Albums;
    public string ThumbnailUrl(GalleryPhoto photo) => Url.Page("/Thumbnail", new { name = photo.ThumbnailName })!;
}
