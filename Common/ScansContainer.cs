using ai_scans_reader.Models;

namespace ai_scans_reader.Common
{
    public class ScansContainer
    {
        public List<ScanFileInfo> Scans { get; set; } = new();

        public bool TryAdd(ScanFileInfo scan)
        {
            if (Scans.Any(s => s.Sha256 == scan.Sha256))
                return false;

            Scans.Add(scan);
            return true;
        }

        public void Remove(string fileName)
        {
            Scans.RemoveAll(s => s.FileName == fileName);
        }

        public ScanFileInfo? GetNextNotSent()
        {
            return Scans.FirstOrDefault(s => s.Status == ScanStatus.NotSent);
        }
    }
}