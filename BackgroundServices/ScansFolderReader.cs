using ai_scans_reader.Common;
using ai_scans_reader.Models;
using Microsoft.Extensions.Options;

namespace ai_scans_reader.BackgroundServices
{
    public class ScansFolderReader : BackgroundService
    {
        private readonly string path;
        private readonly ScansInfo scansInfo;

        public ScansFolderReader(IOptions<ScansOptions> options, ScansInfo scansInfo)
        {
            this.path = options.Value.InputFolder;
            this.scansInfo = scansInfo;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            scansInfo.LoadAll();
            Console.WriteLine($"Начальная загрузка: {scansInfo.Container.Scans.Count} файлов");

            var watcher = new FileSystemWatcher(path, "*.png");

            watcher.Created += (s, e) =>
            {
                scansInfo.AddFile(e.FullPath);
                Console.WriteLine($"[+] Добавлен: {e.Name}");
            };

            watcher.Changed += (s, e) =>
            {
                scansInfo.AddFile(e.FullPath);
                Console.WriteLine($"[~] Изменён: {e.Name}");
            };

            watcher.Deleted += (s, e) =>
            {
                scansInfo.RemoveFile(e.FullPath);
                Console.WriteLine($"[-] Удалён: {e.Name}");
            };

            watcher.Renamed += (s, e) =>
            {
                scansInfo.RemoveFile(e.OldFullPath);
                scansInfo.AddFile(e.FullPath);
                Console.WriteLine($"[>] Переименован: {e.OldName} -> {e.Name}");
            };

            watcher.EnableRaisingEvents = true;
            stoppingToken.Register(() => watcher.Dispose());

            return Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}