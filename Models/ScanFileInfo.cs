namespace ai_scans_reader.Models
{
    public enum ScanStatus
    {
        NotSent,
        Sent,
        ResultReceived
    }

    public class ScanFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public ScanStatus Status { get; set; } = ScanStatus.NotSent;
        public ScanPersonResult? AiResult { get; set; }
    }
}