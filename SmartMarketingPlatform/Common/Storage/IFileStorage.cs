namespace SmartMarketingPlatform.Common.Storage;

// Object storage behind an interface: local disk today, S3 / Azure Blob in production,
// without changing any service. The database keeps only the ObjectKey.
public interface IFileStorage
{
    Task SaveAsync(string objectKey, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
