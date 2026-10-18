using ai_scans_reader.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ai_scans_reader.Common
{
    public class ScansInfo
    {
        private readonly string pathFolderScans;
        private readonly ScansContainer container;

        public ScansContainer Container => container;

        public ScansInfo(IOptions<ScansOptions> options, ScansContainer container)
        {
            this.pathFolderScans = options.Value.InputFolder;
            this.container = container;
        }

        public void LoadAll()
        {
            if (!Directory.Exists(pathFolderScans))
                return;

            foreach (var filePath in Directory.GetFiles(pathFolderScans, "*.png"))
                AddFile(filePath);
        }

        public void AddFile(string filePath)
        {
            if (!File.Exists(filePath) || !filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var sha = ComputeSha256(filePath);
                container.TryAdd(new ScanFileInfo
                {
                    FileName = Path.GetFileName(filePath),
                    FullPath = filePath,
                    Sha256 = sha,
                    Status = ScanStatus.NotSent
                });
            }
            catch (IOException) { }
        }

        public void RemoveFile(string filePath)
        {
            container.Remove(Path.GetFileName(filePath));
        }

        private string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(stream));
        }
    }
}