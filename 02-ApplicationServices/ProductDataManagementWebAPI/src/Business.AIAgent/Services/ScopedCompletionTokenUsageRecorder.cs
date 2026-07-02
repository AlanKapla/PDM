namespace Business.AIAgent.Services;

public sealed class ScopedCompletionTokenUsageRecorder : ICompletionTokenUsageRecorder
{
    private int totalTokens;

    public int TotalTokens => Volatile.Read(ref totalTokens);

    public void Record(int totalTokens)
    {
        if (totalTokens <= 0)
        {
            return;
        }

        Interlocked.Add(ref this.totalTokens, totalTokens);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref totalTokens, 0);
    }
}
