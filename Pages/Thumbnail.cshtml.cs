using BlobPhotoGallery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlobPhotoGallery.Pages;

[ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
public sealed class ThumbnailModel(GalleryCatalog catalog) : PageModel
{
    public IActionResult OnGet(string name)
    {
        if (Path.GetFileName(name) != name) return BadRequest();
        var path = catalog.GetThumbnailPath(name);
        return System.IO.File.Exists(path) ? PhysicalFile(path, "image/jpeg") : NotFound();
    }
}
