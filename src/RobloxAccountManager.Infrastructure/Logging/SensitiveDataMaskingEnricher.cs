using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace RobloxAccountManager.Infrastructure.Logging;

public sealed class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    private static readonly Regex TokenPattern = new(@"(_ROBLOSECURITY|token|password)[\s=:]+([^\s"";]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Enricher placeholder for redaction hooks
    }

    public static string RedactText(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return TokenPattern.Replace(input, "$1=[REDACTED]");
    }
}
