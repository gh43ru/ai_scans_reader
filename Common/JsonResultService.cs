using ai_scans_reader.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ai_scans_reader.Common
{
    public class JsonResultService
    {
        private readonly string scansJsonFolder;
        private readonly string benchFolder;
        private readonly object syncRoot = new object();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly Encoding Utf8NoBom =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public JsonResultService(IOptions<ScansOptions> options)
        {
            scansJsonFolder = options.Value.JsonResultsFolder;
            benchFolder = options.Value.BenchFolder;

            Directory.CreateDirectory(scansJsonFolder);
            Directory.CreateDirectory(benchFolder);
        }

        private string GetJsonPath(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            return Path.Combine(scansJsonFolder, name + ".json");
        }

        public bool TryLoadExisting(string fileName, string sha256, out ScanPersonResult? result)
        {
            result = null;
            var path = GetJsonPath(fileName);
            if (!File.Exists(path))
                return false;

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var saved = JsonSerializer.Deserialize<SavedScanResult>(json, JsonOptions);

                if (saved == null || saved.Sha256 != sha256)
                    return false;

                result = saved.Result;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SaveResult(string fileName, string sha256, ScanPersonResult result, double elapsedSeconds)
        {
            lock (syncRoot)
            {
                var path = GetJsonPath(fileName);
                var payload = new SavedScanResult
                {
                    FileName = fileName,
                    Sha256 = sha256,
                    Result = result,
                    ElapsedSeconds = elapsedSeconds,
                    SavedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(payload, JsonOptions);
                File.WriteAllText(path, json, Utf8NoBom);

                UpdateBenchmark(elapsedSeconds);
            }
        }

        private void UpdateBenchmark(double elapsedSeconds)
        {
            var benchPath = Path.Combine(benchFolder, "benchmark.json");
            BenchmarkInfo bench;

            if (File.Exists(benchPath))
            {
                try
                {
                    var text = File.ReadAllText(benchPath, Encoding.UTF8);
                    bench = JsonSerializer.Deserialize<BenchmarkInfo>(text, JsonOptions) ?? new BenchmarkInfo();
                }
                catch
                {
                    bench = new BenchmarkInfo();
                }
            }
            else
            {
                bench = new BenchmarkInfo();
            }

            if (bench.MinSeconds == 0 || elapsedSeconds < bench.MinSeconds)
                bench.MinSeconds = elapsedSeconds;

            if (elapsedSeconds > bench.MaxSeconds)
                bench.MaxSeconds = elapsedSeconds;

            bench.TotalSeconds += elapsedSeconds;
            bench.Count++;
            bench.LastUpdated = DateTime.Now;

            var json = JsonSerializer.Serialize(bench, JsonOptions);
            File.WriteAllText(benchPath, json, Utf8NoBom);
        }

        public double GetAverageSeconds()
        {
            var benchPath = Path.Combine(benchFolder, "benchmark.json");
            if (!File.Exists(benchPath))
                return 0;

            try
            {
                var text = File.ReadAllText(benchPath, Encoding.UTF8);
                var bench = JsonSerializer.Deserialize<BenchmarkInfo>(text, JsonOptions);
                if (bench == null || bench.Count == 0)
                    return 0;

                return bench.TotalSeconds / bench.Count;
            }
            catch
            {
                return 0;
            }
        }
    }

    public class SavedScanResult
    {
        public string FileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public ScanPersonResult? Result { get; set; }
        public double ElapsedSeconds { get; set; }
        public DateTime SavedAt { get; set; }
    }

    public class BenchmarkInfo
    {
        public double MinSeconds { get; set; }
        public double MaxSeconds { get; set; }
        public double TotalSeconds { get; set; }
        public int Count { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}