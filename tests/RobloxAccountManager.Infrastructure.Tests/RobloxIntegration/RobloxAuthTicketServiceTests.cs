using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RobloxAccountManager.Core.Security;
using RobloxAccountManager.Infrastructure.RobloxIntegration;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests.RobloxIntegration;

public sealed class RobloxAuthTicketServiceTests
{
    [Fact]
    public async Task GetAuthTicketAsync_WhenCsrfChallengeAndValidTicket_ReturnsTicketSuccessfully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request) =>
        {
            // First attempt: simulate CSRF challenge
            if (!request.Headers.Contains("x-csrf-token"))
            {
                var challengeResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);
                challengeResponse.Headers.Add("x-csrf-token", "valid-csrf-token-12345");
                return challengeResponse;
            }

            // Second attempt: verify CSRF token and cookie present, return ticket
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            successResponse.Headers.Add("rbx-authentication-ticket", "ticket-sample-abcdef-98765");
            return successResponse;
        });

        using var httpClient = new HttpClient(handler);
        var service = new RobloxAuthTicketService(httpClient, NullLogger<RobloxAuthTicketService>.Instance);

        var cookieBytes = Encoding.UTF8.GetBytes("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_samplecookie123");
        using var buffer = new SensitiveBuffer(cookieBytes);

        // Act
        var ticket = await service.GetAuthTicketAsync(buffer);

        // Assert
        ticket.Should().Be("ticket-sample-abcdef-98765");
    }

    [Fact]
    public async Task GetAuthTicketAsync_WhenInvalidCookie_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request) =>
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        using var httpClient = new HttpClient(handler);
        var service = new RobloxAuthTicketService(httpClient, NullLogger<RobloxAuthTicketService>.Instance);

        var cookieBytes = Encoding.UTF8.GetBytes("invalid_cookie");
        using var buffer = new SensitiveBuffer(cookieBytes);

        // Act & Assert
        var act = () => service.GetAuthTicketAsync(buffer);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*verify your .ROBLOSECURITY cookie is valid*");
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
