using BlobPhotoGallery.Models;
using BlobPhotoGallery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlobPhotoGallery.Pages;

public sealed class GalleryModel(GalleryCatalog catalog) : PageModel
{
    private const int PageSize = 30;

    public GalleryAlbum Album { get; private set; } = null!;
    public IActionResult OnGet(string slug)
    {
        var album = catalog.Find(slug);
        if (album is null) return NotFound();
        Album = album;
        return Page();
    }
    public IActionResult OnGetPhotos(string slug, int page = 0)
    {
        var album = catalog.Find(slug);
        if (album is null) return NotFound();
        if (page < 0 || page > int.MaxValue / PageSize) return BadRequest();

        var start = page * PageSize;
        var photos = album.Photos
            .Skip(start)
            .Take(PageSize)
            .Select((photo, offset) => new
            {
                order = start + offset,
                thumbnailUrl = ThumbnailUrl(album, photo),
                photoUrl = PhotoUrl(album, photo),
                photo.Width,
                photo.Height
            });

        return new JsonResult(new
        {
            photos,
            hasMore = start + PageSize < album.Photos.Count
        });
    }
    public string PhotosUrl() => Url.Page("/Gallery", "Photos", new { slug = Album.Slug })!;
    public string ThumbnailUrl(GalleryPhoto photo) => Url.Page("/Thumbnail", new { album = Album.Slug, name = photo.ThumbnailName })!;
    public string PhotoUrl(GalleryPhoto photo) => Url.Page("/Photo", new { album = Album.Slug, name = photo.ThumbnailName })!;
    private string ThumbnailUrl(GalleryAlbum album, GalleryPhoto photo) => Url.Page("/Thumbnail", new { album = album.Slug, name = photo.ThumbnailName })!;
    private string PhotoUrl(GalleryAlbum album, GalleryPhoto photo) => Url.Page("/Photo", new { album = album.Slug, name = photo.ThumbnailName })!;
}
