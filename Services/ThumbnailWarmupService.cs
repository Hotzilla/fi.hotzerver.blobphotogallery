namespace BlobPhotoGallery.Services;

public sealed class ThumbnailWarmupService(GalleryCatalog catalog, ILogger<ThumbnailWarmupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await catalog.RefreshAsync(cancellationToken);
            logger.LogInformation("Wedding gallery thumbnail cache is ready with {AlbumCount} albums.", catalog.Albums.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load the wedding gallery. Check the Azure Blob settings.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
