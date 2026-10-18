using ai_scans_reader.Common;
using ai_scans_reader.Models;
using ai_scans_reader.Utils;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace ai_scans_reader.BackgroundServices
{
    public class OllamaService : BackgroundService
    {
        private readonly ScansContainer container;
        private readonly HttpClient httpClient;
        private readonly JsonResultService jsonResultService;
        private readonly string modelName;

        public OllamaService(
            ScansContainer container,
            IHttpClientFactory httpClientFactory,
            JsonResultService jsonResultService,
            IOptions<OllamaOptions> options)
        {
            this.container = container;
            this.jsonResultService = jsonResultService;

            var cfg = options.Value;
            this.modelName = cfg.Model;

            this.httpClient = httpClientFactory.CreateClient();
            this.httpClient.BaseAddress = new Uri(cfg.BaseUrl);
            this.httpClient.Timeout = TimeSpan.FromMinutes(cfg.TimeoutMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var scan = container.GetNextNotSent();

                if (scan == null)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                if (jsonResultService.TryLoadExisting(scan.FileName, scan.Sha256, out var savedResult))
                {
                    scan.AiResult = savedResult;
                    scan.Status = ScanStatus.ResultReceived;
                    Console.WriteLine($"[AI] Уже обработан ранее (загружен из JSON): {scan.FileName}");
                    continue;
                }

                scan.Status = ScanStatus.Sent;
                Console.WriteLine($"[AI] Отправлен: {scan.FileName}");

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var result = await SendToOllama(scan.FullPath, stoppingToken);
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed.TotalSeconds;

                    scan.AiResult = result;
                    scan.Status = ScanStatus.ResultReceived;

                    jsonResultService.SaveResult(scan.FileName, scan.Sha256, result, elapsed);

                    Console.WriteLine($"[AI] Ответ по файлу: {scan.FileName} (за {elapsed:F2} с)");

                    Console.WriteLine($"ФИО: {result.LastName} {result.FirstName} {result.MiddleName}");
                    Console.WriteLine($"Табельный номер: {result.PersonnelNumber}");
                    Console.WriteLine($"Организация: {result.Organization}");
                    Console.WriteLine($"Номер документа: {result.DocumentNumber}");
                    Console.WriteLine($"Дата документа: {result.DocumentDate}");
                    Console.WriteLine($"Подразделение: {result.Department}");
                    Console.WriteLine($"Должность: {result.Position}");
                    Console.WriteLine($"Дата приема: {result.HireDate}");
                    Console.WriteLine($"Оклад: {result.Salary}");
                    Console.WriteLine($"Характер работы: {result.EmploymentType}");
                    Console.WriteLine($"Испытательный: {result.ProbationPeriod}");

                    PrintRemainingTimeEstimate();

                    Console.WriteLine(new string('-', 60));
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    scan.Status = ScanStatus.NotSent;
                    Console.WriteLine($"[AI] Ошибка {scan.FileName} (через {stopwatch.Elapsed.TotalSeconds:F2} с): {ex.Message}");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }

        private void PrintRemainingTimeEstimate()
        {
            int remaining = container.Scans.Count(s => s.Status != ScanStatus.ResultReceived);

            if (remaining == 0)
            {
                Console.WriteLine("[Прогноз] Все сканы обработаны.");
                return;
            }

            var avg = jsonResultService.GetAverageSeconds();

            if (avg <= 0)
            {
                Console.WriteLine($"[Прогноз] Осталось файлов: {remaining}. Среднее время пока неизвестно.");
                return;
            }

            var totalSeconds = remaining * avg;
            var eta = TimeSpan.FromSeconds(totalSeconds);

            Console.WriteLine($"[Прогноз] Осталось файлов: {remaining}. " +
                              $"Среднее: {avg:F2} с. " +
                              $"Ждать примерно: {FormatTimeSpan(eta)} (до ~{DateTime.Now.AddSeconds(totalSeconds):HH:mm:ss})");
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours} ч {ts.Minutes} мин {ts.Seconds} с";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes} мин {ts.Seconds} с";
            return $"{ts.Seconds} с";
        }

        private async Task<ScanPersonResult> SendToOllama(string filePath, CancellationToken ct)
        {
            var imageBytes = await File.ReadAllBytesAsync(filePath, ct);
            var base64 = Convert.ToBase64String(imageBytes);

            var request = new
            {
                model = modelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content =
                            "Ты — эксперт по извлечению данных из кадровых документов (форма Т-1 'Приказ о приеме на работу'). " +
                            "Извлеки следующую информацию из изображения:\n" +
                            "- ФИО — в строке 'Фамилия, имя, отчество'. Верни одной строкой в том порядке, как написано в документе (Фамилия Имя Отчество), без запятых и лишних пробелов.\n" +
                            "- Табельный номер — в правом верхнем углу или рядом с ФИО.\n" +
                            "- Наименование организации — в шапке документа.\n" +
                            "- Номер и дата документа — в блоке 'Номер документа' и 'Дата составления'.\n" +
                            "- Структурное подразделение (Отдел продаж и т.д.).\n" +
                            "- Должность (менеджер и т.д.).\n" +
                            "- Дата приема на работу — в блоке 'Принять на работу' (с какого числа).\n" +
                            "- Оклад — в блоке 'с тарифной ставкой (окладом)'.\n" +
                            "- Характер работы — 'основное место работы' и т.д.\n" +
                            "- Испытательный срок — в блоке 'с испытанием на срок'.\n\n" +
                            "ПРАВИЛА ФОРМАТИРОВАНИЯ (обязательны):\n" +
                            "- Оклад верни только цифрами, без пробелов и разделителей тысяч.\n" +
                            "- Испытательный срок — без служебных пометок '(ев)', '(а)', '(ов)'.\n" +
                            "- Все даты — в формате dd.MM.yyyy (полный год).\n\n" +
                            "Ответь СТРОГО в формате JSON без пояснений и без markdown. " +
                            "Если поле не найдено — оставь пустую строку. Не добавляй другие поля.",
                        images = new[] { base64 }
                    }
                },
                format = new
                {
                    type = "object",
                    properties = new
                    {
                        fullName = new { type = "string" },
                        personnelNumber = new { type = "string" },
                        organization = new { type = "string" },
                        documentNumber = new { type = "string" },
                        documentDate = new { type = "string" },
                        department = new { type = "string" },
                        position = new { type = "string" },
                        hireDate = new { type = "string" },
                        salary = new { type = "string" },
                        employmentType = new { type = "string" },
                        probationPeriod = new { type = "string" }
                    },
                    required = new[]
                    {
                        "fullName", "personnelNumber",
                        "organization", "documentNumber", "documentDate",
                        "department", "position", "hireDate", "salary",
                        "employmentType", "probationPeriod"
                    }
                },
                stream = false,
                options = new
                {
                    temperature = 0
                }
            };

            var response = await httpClient.PostAsJsonAsync("/api/chat", request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var content = json.GetProperty("message").GetProperty("content").GetString() ?? "{}";

            var person = JsonSerializer.Deserialize<ScanPersonResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ScanPersonResult();

            var parts = (person.FullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            person.LastName = parts.ElementAtOrDefault(0) ?? string.Empty;
            person.FirstName = parts.ElementAtOrDefault(1) ?? string.Empty;
            person.MiddleName = parts.ElementAtOrDefault(2) ?? string.Empty;

            person.LastName = ScanFormatter.ReplaceQuotes(person.LastName);
            person.FirstName = ScanFormatter.ReplaceQuotes(person.FirstName);
            person.MiddleName = ScanFormatter.ReplaceQuotes(person.MiddleName);
            person.Organization = ScanFormatter.ReplaceQuotes(person.Organization);
            person.Department = ScanFormatter.ReplaceQuotes(person.Department);
            person.Position = ScanFormatter.ReplaceQuotes(person.Position);

            person.Salary = ScanFormatter.NormalizeSalary(person.Salary);
            person.ProbationPeriod = ScanFormatter.MonthFormatting(person.ProbationPeriod);

            person.DocumentDate = ScanFormatter.ToStandardDate(person.DocumentDate);
            person.HireDate = ScanFormatter.ToStandardDate(person.HireDate);

            return person;
        }
    }
}