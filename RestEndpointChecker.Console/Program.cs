using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace RestEndpointChecker.Console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var config = new UrlConfig();
            configuration.Bind(config);

            if (config?.Urls == null || config.Urls.Length == 0)
            {
                System.Console.WriteLine("Error: No URLs found in configuration file.");
                return;
            }

            System.Console.WriteLine($"Testing URLs at {DateTime.Now}...\n");

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var failures = new List<(string Url, string StatusCode, string Description)>();

            foreach (var url in config.Urls)
            {
                var statusCode = "N/A";
                var description = "";
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var response = await httpClient.GetAsync(url);
                    stopwatch.Stop();

                    statusCode = ((int)response.StatusCode).ToString();

                    if (response.IsSuccessStatusCode) continue;
                    else description = $"{response.ReasonPhrase}";
                }
                catch (TaskCanceledException)
                {
                    stopwatch.Stop();
                    statusCode = "TIMEOUT";
                    description = "Request timed out (>10s)";
                }
                catch (HttpRequestException ex)
                {
                    stopwatch.Stop();
                    statusCode = "ERROR";
                    description = ex.Message.Length > 30 ? ex.Message.Substring(0, 27) + "..." : ex.Message;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    statusCode = "ERROR";
                    description = ex.Message.Length > 30 ? ex.Message.Substring(0, 27) + "..." : ex.Message;
                }

                failures.Add((url, statusCode, description));
            }

            if (failures.Count == 0) System.Console.WriteLine("✓ All URLs returned successful responses (200 OK)");
            else
            {
                System.Console.WriteLine("┌───────────────────────────────────────────────────────────────┬────────────┬──────────────────────────────────┐");
                System.Console.WriteLine("│ URL                                                           │ Status     │ Description                      │");
                System.Console.WriteLine("├───────────────────────────────────────────────────────────────┼────────────┼──────────────────────────────────┤");

                foreach (var (url, statusCode, description) in failures)
                {
                    var urlFormatted = url.Length > 61 ? url.Substring(0, 58) + "..." : url.PadRight(61);
                    var statusFormatted = statusCode.PadRight(10);
                    var descFormatted = description.Length > 32 ? description.Substring(0, 29) + "..." : description.PadRight(32);

                    System.Console.WriteLine($"│ {urlFormatted} │ {statusFormatted} │ {descFormatted} │");
                }

                System.Console.WriteLine("└───────────────────────────────────────────────────────────────┴────────────┴──────────────────────────────────┘");
            }
        }
    }

    class UrlConfig
    {
        public string[] Urls { get; set; } = Array.Empty<string>();
    }
}
