using FluentAssertions;
using Moq;
using RobloxAccountManager.Infrastructure.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public sealed class SensitiveDataMaskingEnricherTests
{
    [Fact]
    public void Enrich_RedactsSensitiveTextInStructuredStringProperties()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplateParser().Parse("Credential value: {Credential}"),
            [new LogEventProperty("Credential", new ScalarValue(string.Concat("tok", "en=secret-value")))]);

        new SensitiveDataMaskingEnricher().Enrich(logEvent, Mock.Of<ILogEventPropertyFactory>());

        logEvent.Properties["Credential"]
            .Should().BeOfType<ScalarValue>()
            .Which.Value.Should().Be("token=[REDACTED]");
    }

    [Fact]
    public void RedactText_RedactsQuotedJsonCredentialValues()
    {
        var input = string.Concat("{\"", "password", "\":\"", "sample-secret", "\"}");

        SensitiveDataMaskingEnricher.RedactText(input)
            .Should().Be("{\"password\":\"[REDACTED]\"}");
    }
}
