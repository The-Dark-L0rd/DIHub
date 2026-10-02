namespace DIHub.Core.Models
{
    public sealed class ExtensionInstallResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public ExtensionInfo? Extension { get; init; }

        public static ExtensionInstallResult Ok(ExtensionInfo ext)
            => new() { Success = true, Extension = ext };

        public static ExtensionInstallResult Fail(string message)
            => new() { Success = false, ErrorMessage = message };
    }
}