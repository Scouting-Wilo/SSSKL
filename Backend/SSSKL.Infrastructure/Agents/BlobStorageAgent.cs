using System.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SSSKL.Contracts.Agents;
using SSSKL.Domain.Entities;

namespace SSSKL.Infrastructure.Agents;

public partial class BlobStorageAgent : IBlobStorageAgent
{
    private readonly string _blobContainerName;
    private readonly BlobServiceClient _storage;
    private readonly ILogger<BlobStorageAgent> _logger;

    public BlobStorageAgent(
        IConfiguration configuration, 
        BlobServiceClient storage,
        ILogger<BlobStorageAgent> logger)
    {
        _blobContainerName = configuration.GetSection("Storage")["ContainerName"] 
                             ?? throw new ConfigurationErrorsException("No Blob Container name found in config");
        _storage = storage;
        _logger = logger;
    }
    
    public async Task<BlobStorageItem> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream)
    {
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();

        var container = _storage.GetBlobContainerClient(_blobContainerName);
        var createResponse = await container.CreateIfNotExistsAsync();
        if (createResponse != null && createResponse.GetRawResponse().Status == 201)
            await container.SetAccessPolicyAsync(PublicAccessType.None);
        var blob = container.GetBlobClient(strFileName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        await blob.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = normalizedContentType });
        var urlString = blob.Uri.ToString();
        LogFileUploaded(_logger, strFileName, normalizedContentType);
        return new BlobStorageItem() { FileName = strFileName, Uri = urlString, ContentType = normalizedContentType };
    }

    public async Task<bool> DeleteFileToBlobAsync(string strFileName)
    {
        var container = _storage.GetBlobContainerClient(_blobContainerName);
        var createResponse = await container.CreateIfNotExistsAsync();
        if (createResponse != null && createResponse.GetRawResponse().Status == 201)
            await container.SetAccessPolicyAsync(PublicAccessType.None);
        var blob = container.GetBlobClient(strFileName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        LogFileDeleted(_logger, strFileName);
        return true;
    }

    public async Task<Stream> OpenDownloadStreamAsync(string strFileName)
    {
        var container = _storage.GetBlobContainerClient(_blobContainerName);
        var blob = container.GetBlobClient(strFileName);
        return await blob.OpenReadAsync();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "File uploaded to Blob Storage: {FileName} ({ContentType})")]
    private static partial void LogFileUploaded(ILogger logger, string fileName, string contentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "File deleted from Blob Storage: {FileName}")]
    private static partial void LogFileDeleted(ILogger logger, string fileName);
}
