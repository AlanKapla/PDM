namespace Business.AIAgent.Services;

public interface ICompletionTokenUsageRecorder
{
    int TotalTokens { get; }

    void Record(int totalTokens);

    void Reset();
}
