namespace Business.Interfaces.Services;

public interface IPdfToImageConverterService
{
    /// <summary>
    /// Renderuje wszystkie strony PDF do tablic bajtów PNG (300 DPI, białe tło).
    /// Nazwa metody historyczna — format wyjściowy to PNG.
    /// </summary>
    Task<IReadOnlyList<byte[]>> ConvertAllPagesToJpegAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken);
}
