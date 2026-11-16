using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace RobloxAccountManager.Infrastructure.Logging;

public sealed class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    private const string RobloxCookieName = "_ROBLO" + "SECURITY";
    private static readonly Regex TokenPattern = new(
        $@"(?<prefix>[""']?)(?<key>{Regex.Escape(RobloxCookieName)}|token|password)(?<separator>[""']?\s*[:=]\s*[""']?)(?:(?<quote>[""'])(?<quoted>.*?)\k<quote>|(?<plain>[^\s,""';]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToArray())
        {
            if (property.Value is not ScalarValue { Value: string value })
            {
                continue;
            }

            var redacted = RedactText(value);
            if (!string.Equals(value, redacted, StringComparison.Ordinal))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, new ScalarValue(redacted)));
            }
        }
    }

    public static string RedactText(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return TokenPattern.Replace(input, match =>
        {
            var value = match.Groups["quoted"].Success
                ? $"{match.Groups["quote"].Value}[REDACTED]{match.Groups["quote"].Value}"
                : "[REDACTED]";

            return $"{match.Groups["prefix"].Value}{match.Groups["key"].Value}{match.Groups["separator"].Value}{value}";
        });
    }
}
