namespace Business.Interfaces.Configurations
{
    public sealed class BlobStorageSettings
    {
        public const string SectionName = "BlobStorage";
        public string ContainerUrl { get; set; } = string.Empty;
        public string QueueUrl { get; set; } = string.Empty;        

        /// <summary>
        /// Pobiera nazwę kontenera zgodną z regułami Azure Blob Storage (małe litery)
        /// </summary>
        public static string GetContainerName(BlobContainerNames containerName)
        {
            return containerName.ToString().ToLowerInvariant();
        }
    }
}
