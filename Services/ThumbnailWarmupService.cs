namespace BlobPhotoGallery.Services;

public sealed class ThumbnailWarmupService(GalleryCatalog catalog, ILogger<ThumbnailWarmupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await catalog.LoadCacheAsync(cancellationToken);
            logger.LogInformation("Wedding gallery loaded {AlbumCount} albums from the thumbnail cache.", catalog.Albums.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load the wedding gallery thumbnail cache.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
