using BlobPhotoGallery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlobPhotoGallery.Pages;

public sealed class PhotoModel(GalleryCatalog catalog) : PageModel
{
    public IActionResult OnGet(string album, string name)
    {
        var photo = catalog.Find(album)?.Photos.FirstOrDefault(item => item.ThumbnailName == name);
        return photo is null ? NotFound() : Redirect(catalog.GetPhotoUri(photo.BlobName).AbsoluteUri);
    }
}
