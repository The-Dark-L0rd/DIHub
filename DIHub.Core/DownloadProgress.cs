namespace DIHub.Core.Models
{
    /// <summary>Progress report emitted during extension downloads.</summary>
    public sealed class DownloadProgress
    {
        /// <summary>"Downloading" | "Verifying" | "Installing"</summary>
        public string CurrentStage { get; init; } = "Downloading";

        public long BytesReceived { get; init; }

        /// <summary>-1 when the server did not provide Content-Length.</summary>
        public long TotalBytes { get; init; } = -1;

        /// <summary>0.0 – 1.0. Zero when TotalBytes is unknown.</summary>
        public double PercentComplete { get; init; }
    }
}