namespace BlobPhotoGallery.Services;

public sealed class ThumbnailWarmupService(GalleryCatalog catalog, ILogger<ThumbnailWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await catalog.RefreshAsync(stoppingToken);
            logger.LogInformation("Wedding gallery thumbnail cache is ready with {AlbumCount} albums.", catalog.Albums.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load the wedding gallery. Check the Azure Blob settings.");
        }
    }
}
