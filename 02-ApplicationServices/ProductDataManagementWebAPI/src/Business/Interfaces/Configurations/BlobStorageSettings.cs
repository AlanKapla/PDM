namespace Business.Interfaces.Configurations
{
    public sealed class BlobStorageSettings
    {
        public const string SectionName = "BlobStorage";
        public string ContainerUrl { get; set; } = string.Empty;
        public string QueueUrl { get; set; } = string.Empty;
    }
}
