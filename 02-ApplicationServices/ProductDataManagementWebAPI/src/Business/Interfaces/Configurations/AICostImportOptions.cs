namespace Business.Interfaces.Configurations
{
    public sealed class AICostImportOptions
    {
        public const string SectionName = "AICostImport";

        public int MaxRetryAttempts { get; set; } = 3;
        public int InitialRetryDelaySeconds { get; set; } = 30;
        public double RetryBackoffMultiplier { get; set; } = 2;
        public int RetentionDays { get; set; } = 30;
        public long MaxBatchTotalBytes { get; set; } = 52_428_800;
        public string QueueName { get; set; } = "ai-cost-import-process";
        public int WorkerPollIntervalSeconds { get; set; } = 5;
    }
}
