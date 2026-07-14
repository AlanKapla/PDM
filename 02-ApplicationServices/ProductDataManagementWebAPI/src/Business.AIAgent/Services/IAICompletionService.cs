namespace Business.AIAgent.Services;

public interface IAICompletionService
{
    /// <param name="temperature">0.0–2.0. Dla JSON generation używaj 0.1–0.3. Domyślnie null = domyślna wartość modelu.</param>
    /// <param name="jsonMode">Gdy true, wymusza odpowiedź w formacie JSON object (json_object response format).</param>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        int maxOutputTokens = 4096,
        float? temperature = null,
        bool jsonMode = false);

    Task<string> CompleteWithImageAsync(string systemPrompt, byte[] imageBytes, string mediaType, CancellationToken cancellationToken);
}
