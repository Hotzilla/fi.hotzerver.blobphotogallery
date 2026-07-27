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

At startup the application lists blobs, reads EXIF `DateTimeOriginal`, creates local JPEG thumbnails, and sorts each album oldest-first. When a guest opens a photograph, the app validates that it belongs to the album and redirects the browser to the private blob URL carrying the configured SAS. Consequently full-resolution bytes travel from Azure directly to the browser, not through this host.

> Azure SAS tokens are time-limited bearer credentials, not truly single-use tokens. This implementation exposes the configured read-only SAS only after an authenticated photo click. Rotate it regularly and give it a short expiry. True one-use links require an additional stateful token service; Azure Storage does not enforce single-use SAS URLs.

## Run

```bash
dotnet restore
dotnet run
```

The thumbnail cache must be writable and should be placed on persistent storage in production. A changed blob ETag creates a fresh cached thumbnail automatically.
