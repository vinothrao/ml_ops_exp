namespace HousePredictionAPI.Models;

public class AzureStorageConfig
{
    public string StorageAccount { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
