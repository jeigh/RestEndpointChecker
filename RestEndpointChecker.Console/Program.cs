using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace RestEndpointChecker.Console
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            bool verbose = args.Contains("--verbose") || args.Contains("-v");
            await RunChecksAsync(verbose);
            return 0;
        }

        static async Task RunChecksAsync(bool verbose)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var config = new UrlConfig();
            configuration.Bind(config);

            var emailConfig = new EmailConfig();
            configuration.GetSection("Email").Bind(emailConfig);

            if (config?.Urls == null || config.Urls.Length == 0)
            {
                System.Console.WriteLine("Error: No URLs found in configuration file.");
                return;
            }

            var resultMessage = new StringBuilder();
            resultMessage.AppendLine($"Testing URLs at {DateTime.Now}...\n");

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
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
                    description = $"Request timed out (>{config.TimeoutSeconds}s)";
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

            if (failures.Count == 0)
            {
                resultMessage.AppendLine("✓ All URLs returned successful responses (200 OK)");
            }
            else
            {
                resultMessage.AppendLine("┌───────────────────────────────────────────────────────────────┬────────────┬──────────────────────────────────┐");
                resultMessage.AppendLine("│ URL                                                           │ Status     │ Description                      │");
                resultMessage.AppendLine("├───────────────────────────────────────────────────────────────┼────────────┼──────────────────────────────────┤");

                foreach (var (url, statusCode, description) in failures)
                {
                    var urlFormatted = url.Length > 61 ? url.Substring(0, 58) + "..." : url.PadRight(61);
                    var statusFormatted = statusCode.PadRight(10);
                    var descFormatted = description.Length > 32 ? description.Substring(0, 29) + "..." : description.PadRight(32);

                    resultMessage.AppendLine($"│ {urlFormatted} │ {statusFormatted} │ {descFormatted} │");
                }

                resultMessage.AppendLine("└───────────────────────────────────────────────────────────────┴────────────┴──────────────────────────────────┘");
            }

            if (verbose)
            {
                System.Console.Write(resultMessage.ToString());
            }
            else
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(emailConfig.SmtpServer))
                    {
                        var emailService = new EmailService(emailConfig);
                        await emailService.SendResultsAsync(resultMessage.ToString());
                        System.Console.WriteLine($"Results sent via email to {emailConfig.ToAddress}");
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Failed to send email: {ex.Message}");
                    System.Console.WriteLine("Results:");
                    System.Console.Write(resultMessage.ToString());
                }
            }
        }
    }

    class UrlConfig
    {
        public string[] Urls { get; set; } = Array.Empty<string>();
        public int TimeoutSeconds { get; set; } = 10;
    }
}
