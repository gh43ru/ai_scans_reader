using System.Globalization;

namespace ai_scans_reader.Utils
{
    public static class ScanFormatter
    {
        public static string ReplaceQuotes(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return string.Empty;

            var result = new System.Text.StringBuilder(originalText.Length);
            bool open = true;

            foreach (var ch in originalText)
            {
                if (ch == '"')
                {
                    result.Append(open ? '«' : '»');
                    open = !open;
                }
                else
                {
                    result.Append(ch);
                }
            }

            return result.ToString();
        }

        public static string MonthFormatting(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return string.Empty;

            var text = originalText
                .Replace("(ев)", "")
                .Replace("(а)", "")
                .Replace("(ов)", "")
                .Trim()
                .ToLowerInvariant();

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return string.Empty;

            bool hasMonthWord = words.Any(w => w.StartsWith("месяц") || w == "мес." || w == "мес");

            if (hasMonthWord)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    if (IsNumberWord(word))
                    {
                        var replaced = char.ToUpper(word[0], CultureInfo.CurrentCulture) + word.Substring(1);
                        Console.WriteLine($"[ScanFormatter] '{originalText}' '{word}' заменено на '{replaced}'");
                        words[i] = replaced;
                    }
                }
                return string.Join(' ', words);
            }

            var first = words[0];
            var capitalized = char.ToUpper(first[0], CultureInfo.CurrentCulture) + first.Substring(1);

            int num = WordToNumber(first);
            if (num <= 0)
                return capitalized;

            string monthWord = GetMonthWord(num);
            var result = $"{capitalized} {monthWord}";
            Console.WriteLine($"[ScanFormatter] '{originalText}' добавлено '{result}'");
            return result;
        }

        public static string NormalizeSalary(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return string.Empty;

            return originalText
                .Replace(" ", "")
                .Replace("\u00A0", "")
                .Replace(",", ".")
                .Trim();
        }

        public static string ToStandardDate(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return string.Empty;

            var input = originalText.Trim();

            foreach (var prefix in new[] { "с ", "по ", "от " })
            {
                if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(prefix.Length).Trim();
                    break;
                }
            }

            input = input.Replace("г.", "").Replace("года", "").Trim();

            string[] formats =
            {
                "dd.MM.yyyy", "d.M.yyyy",
                "dd.MM.yy",   "d.M.yy",
                "dd/MM/yyyy", "d/M/yyyy",
                "dd-MM-yyyy", "d-M-yyyy",
                "yyyy-MM-dd", "yyyy.MM.dd"
            };

            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
            {
                return dt.ToString("dd.MM.yyyy");
            }

            return input;
        }

        private static bool IsNumberWord(string word)
        {
            return WordToNumber(word) > 0 ||
                   word == "одиннадцать" || word == "двенадцать" ||
                   word == "полтора" || word == "полутора";
        }

        private static int WordToNumber(string word)
        {
            return word switch
            {
                "один" or "одного" or "одна" or "одну" => 1,
                "два" or "двух" or "две" => 2,
                "три" or "трех" or "трёх" => 3,
                "четыре" or "четырех" or "четырёх" => 4,
                "пять" or "пяти" => 5,
                "шесть" or "шести" => 6,
                "семь" or "семи" => 7,
                "восемь" or "восьми" => 8,
                "девять" or "девяти" => 9,
                "десять" or "десяти" => 10,
                "одиннадцать" => 11,
                "двенадцать" => 12,
                "полтора" or "полутора" => 1,
                _ => 0
            };
        }

        private static string GetMonthWord(int num)
        {
            var lastTwo = num % 100;
            var lastOne = num % 10;

            if (lastTwo >= 11 && lastTwo <= 14)
                return "месяцев";
            if (lastOne == 1)
                return "месяц";
            if (lastOne >= 2 && lastOne <= 4)
                return "месяца";
            return "месяцев";
        }
    }
}