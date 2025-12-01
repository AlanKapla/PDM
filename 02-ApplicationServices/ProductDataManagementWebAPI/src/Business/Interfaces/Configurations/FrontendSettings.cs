namespace Business.Interfaces.Configurations
{
    public sealed class FrontendSettings
    {
        public const string SectionName = "Frontend";
        public string BaseUrl { get; set; } = string.Empty;
        public string ActivationPath { get; set; } = string.Empty; 
        public string ResetPasswordPath { get; set; } = string.Empty; 
        public string HomePath { get; set; } = string.Empty; 
    }
}