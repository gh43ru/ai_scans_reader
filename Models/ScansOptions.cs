namespace ai_scans_reader.Models
{
    public class ScansOptions
    {
        public string InputFolder { get; set; } = string.Empty;
        public string JsonResultsFolder { get; set; } = string.Empty;
        public string BenchFolder { get; set; } = string.Empty;
    }

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "qwen3-vl:8b-instruct";
        public int TimeoutMinutes { get; set; } = 5;
    }
}
