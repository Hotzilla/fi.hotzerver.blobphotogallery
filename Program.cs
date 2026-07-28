using BlobPhotoGallery.Options;
using BlobPhotoGallery.Services;

const string generateThumbnailsFlag = "--generate-thumbnails";
var generateThumbnails = args.Any(argument =>
    string.Equals(argument, generateThumbnailsFlag, StringComparison.OrdinalIgnoreCase));
var builder = WebApplication.CreateBuilder(args.Where(argument =>
    !string.Equals(argument, generateThumbnailsFlag, StringComparison.OrdinalIgnoreCase)).ToArray());
builder.Services.AddRazorPages();
builder.Services.Configure<GalleryOptions>(builder.Configuration.GetSection(GalleryOptions.SectionName));
builder.Services.AddSingleton<GalleryCatalog>();
builder.Services.AddHostedService<ThumbnailWarmupService>();

var app = builder.Build();
if (generateThumbnails)
{
    var catalog = app.Services.GetRequiredService<GalleryCatalog>();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ThumbnailGenerator");

    logger.LogInformation("Generating the wedding gallery thumbnail cache.");
    await catalog.RefreshAsync(CancellationToken.None);
    logger.LogInformation("Wedding gallery thumbnail cache is ready with {AlbumCount} albums.", catalog.Albums.Count);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<QueryKeyMiddleware>();
app.MapRazorPages();
app.Run();

public partial class Program;
