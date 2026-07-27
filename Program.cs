using BlobPhotoGallery.Options;
using BlobPhotoGallery.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.Configure<GalleryOptions>(builder.Configuration.GetSection(GalleryOptions.SectionName));
builder.Services.AddSingleton<GalleryCatalog>();
builder.Services.AddHostedService<ThumbnailWarmupService>();

var app = builder.Build();
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
