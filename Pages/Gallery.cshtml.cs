using BlobPhotoGallery.Models;
using BlobPhotoGallery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlobPhotoGallery.Pages;

public sealed class GalleryModel(GalleryCatalog catalog) : PageModel
{
    public GalleryAlbum Album { get; private set; } = null!;
    public IActionResult OnGet(string slug)
    {
        var album = catalog.Find(slug);
        if (album is null) return NotFound();
        Album = album;
        return Page();
    }
    public string ThumbnailUrl(GalleryPhoto photo) => Url.Page("/Thumbnail", new { album = Album.Slug, name = photo.ThumbnailName })!;
    public string PhotoUrl(GalleryPhoto photo) => Url.Page("/Photo", new { album = Album.Slug, name = photo.ThumbnailName })!;
}
