using System.Security.Cryptography;
using System.Text;
using BlobPhotoGallery.Options;
using Microsoft.Extensions.Options;

namespace BlobPhotoGallery.Services;

public sealed class QueryKeyMiddleware(RequestDelegate next, IOptions<GalleryOptions> options)
{
    private const string CookieName = "wedding-gallery-access";
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.AccessKey);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/favicon"))
        {
            await next(context);
            return;
        }

        var supplied = context.Request.Query["key"].ToString();
        var cookie = context.Request.Cookies[CookieName] ?? string.Empty;
        if (Matches(supplied) || Matches(cookie))
        {
            if (Matches(supplied))
            {
                context.Response.Cookies.Append(CookieName, supplied, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromDays(14),
                    IsEssential = true
                });
            }
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Galleriaa ei löytynyt.");
    }

    private bool Matches(string candidate)
    {
        var bytes = Encoding.UTF8.GetBytes(candidate);
        return bytes.Length == _key.Length && CryptographicOperations.FixedTimeEquals(bytes, _key);
    }
}
