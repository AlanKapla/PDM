namespace Business.Interfaces.Exceptions
{
    /// <summary>
    /// Thrown when PDF cannot be converted to images for AI processing.
    /// <see cref="Message"/> is English (logs); <see cref="UserMessage"/> is Polish (UI/worker).
    /// </summary>
    public sealed class PdfConversionException : Exception
    {
        public PdfConversionReason Reason { get; }

        public string UserMessage { get; }

        public PdfConversionException(
            PdfConversionReason reason,
            string message,
            string userMessage)
            : base(message)
        {
            Reason = reason;
            UserMessage = userMessage;
        }

        public PdfConversionException(
            PdfConversionReason reason,
            string message,
            string userMessage,
            Exception innerException)
            : base(message, innerException)
        {
            Reason = reason;
            UserMessage = userMessage;
        }

        public static PdfConversionException PasswordProtected(Exception? inner = null)
        {
            const string english = "PDF is password-protected and cannot be processed.";
            const string polish = "Plik PDF jest zabezpieczony hasłem i nie może zostać przetworzony";
            if (inner is null)
            {
                return new PdfConversionException(PdfConversionReason.PasswordProtected, english, polish);
            }

            return new PdfConversionException(PdfConversionReason.PasswordProtected, english, polish, inner);
        }

        public static PdfConversionException Corrupt(Exception? inner = null)
        {
            const string english = "Failed to read PDF file – the file may be corrupt.";
            const string polish = "Nie udało się odczytać pliku PDF – plik może być uszkodzony";
            if (inner is null)
            {
                return new PdfConversionException(PdfConversionReason.Corrupt, english, polish);
            }

            return new PdfConversionException(PdfConversionReason.Corrupt, english, polish, inner);
        }

        public static PdfConversionException TooManyPages(int pageCount, int maxPages)
        {
            string english = $"PDF has too many pages ({pageCount}; maximum {maxPages}).";
            const string polish = "Plik PDF ma zbyt wiele stron (maks. 20).";
            return new PdfConversionException(PdfConversionReason.TooManyPages, english, polish);
        }
    }
}
