namespace Business.Interfaces.Services
{
    public interface IPdfToImageConverter
    {
        /// <summary>
        /// Converts every PDF page to JPEG bytes (in-memory), preserving page order (index 0 = page 1).
        /// </summary>
        Task<IReadOnlyList<byte[]>> ConvertAllPagesToJpegAsync(
            byte[] pdfBytes,
            CancellationToken cancellationToken);
    }
}
