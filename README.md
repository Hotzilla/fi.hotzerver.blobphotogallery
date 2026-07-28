# Wedding thank-you gallery

A responsive .NET 10 Razor Pages gallery backed by a private Azure Blob container. Albums are the first-level folders in the container; JPG/JPEG files directly inside each folder become photographs.

## Configure

Put production secrets in `appsettings.Production.json`, environment variables, or your secret manager (never commit real values):

```json
{
  "Gallery": {
    "AccessKey": "a-long-random-link-secret",
    "ContainerUrl": "https://account.blob.core.windows.net/container",
    "SharedAccessToken": "sv=...&sp=rl&se=...&sig=...",
    "ThumbnailCachePath": "/var/cache/wedding-gallery",
    "ThumbnailWidth": 640
  }
}
```

The SAS needs only **read** and **list** permissions. Give guests `https://your-host/?key=a-long-random-link-secret`. A successful query stores the key in a secure, HTTP-only cookie so internal navigation and image requests continue to work. Invalid requests return 404 rather than advertising the private gallery.

At startup the application lists blobs, reads EXIF `DateTimeOriginal`, creates local JPEG thumbnails, and sorts each album oldest-first. Put an album cover at the container root using the folder name plus `.jpg` (for example, `seremonia.jpg` for the `seremonia/` album). Gallery photos are shown in three equal-width masonry columns while retaining their row-wise chronological order. When a guest opens a photograph, the app validates that it belongs to the album and redirects the browser to the private blob URL carrying the configured SAS. Consequently full-resolution bytes travel from Azure directly to the browser, not through this host.

Name the featured album folder with a `main-` prefix (for example, `main-hääpäivä/`). The first matching folder is moved to the top and highlighted as the main gallery, but the technical `main-` prefix is hidden from guests; all remaining albums keep their normal alphabetical order.

> Azure SAS tokens are time-limited bearer credentials, not truly single-use tokens. This implementation exposes the configured read-only SAS only after an authenticated photo click. Rotate it regularly and give it a short expiry. True one-use links require an additional stateful token service; Azure Storage does not enforce single-use SAS URLs.

## Run

```bash
dotnet restore
dotnet run
```

The thumbnail cache must be writable and should be placed on persistent storage in production. Each album is generated once into its own `album-<folder>` cache directory together with an `album.json` manifest. If that directory already exists, startup reads the manifest and does not download or regenerate its thumbnails. After changing album photos or the root cover, manually delete that album's cache directory before restarting the application.
