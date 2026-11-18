using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Security;

namespace RobloxAccountManager.Infrastructure.RobloxIntegration;

public sealed class RobloxAuthTicketService : IRobloxAuthTicketService
{
    private static readonly Uri AuthTicketEndpoint = new("https://auth.roblox.com/v1/authentication-ticket");
    private readonly HttpClient _httpClient;
    private readonly ILogger<RobloxAuthTicketService> _logger;

    public RobloxAuthTicketService(HttpClient httpClient, ILogger<RobloxAuthTicketService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetAuthTicketAsync(SensitiveBuffer cookieBuffer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookieBuffer);

        var rawBytes = cookieBuffer.DangerousGetBuffer();
        var rawCookie = Encoding.UTF8.GetString(rawBytes).Trim().Trim('"');

        // Ensure formatting: if the user passed raw value, ensure .ROBLOSECURITY= prefix
        string cookieHeaderValue = rawCookie.StartsWith(".ROBLOSECURITY=", StringComparison.OrdinalIgnoreCase)
            ? rawCookie
            : $".ROBLOSECURITY={rawCookie}";

        using var request = CreateTicketRequest(cookieHeaderValue);

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Handle Roblox CSRF Challenge (HTTP 403 Forbidden with x-csrf-token)
        if (response.StatusCode == HttpStatusCode.Forbidden &&
            response.Headers.TryGetValues("x-csrf-token", out var csrfTokens))
        {
            var csrfToken = csrfTokens.FirstOrDefault();
            response.Dispose(); // dispose the failed challenge response

            if (!string.IsNullOrEmpty(csrfToken))
            {
                using var retryRequest = CreateTicketRequest(cookieHeaderValue);
                retryRequest.Headers.Add("x-csrf-token", csrfToken);

                response = await _httpClient.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("Roblox returned a CSRF challenge without a valid token.");
            }
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = string.Empty;
                try
                {
                    errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                }
                catch { /* ignore */ }

                _logger.LogWarning("Roblox authentication ticket request failed with HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Roblox returned status {response.StatusCode} when requesting authentication ticket. Please verify your .ROBLOSECURITY cookie is valid and active.");
            }

            if (response.Headers.TryGetValues("rbx-authentication-ticket", out var ticketHeaders))
            {
                var ticket = ticketHeaders.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ticket))
                {
                    return ticket.Trim();
                }
            }

            if (response.Content.Headers.TryGetValues("rbx-authentication-ticket", out var contentTicketHeaders))
            {
                var ticket = contentTicketHeaders.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ticket))
                {
                    return ticket.Trim();
                }
            }
        }

        throw new InvalidOperationException("Roblox response did not include an 'rbx-authentication-ticket' header.");
    }

    private static HttpRequestMessage CreateTicketRequest(string cookieHeaderValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, AuthTicketEndpoint);
        request.Headers.Add("Cookie", cookieHeaderValue);
        request.Headers.Add("Referer", "https://www.roblox.com/");
        request.Headers.Add("Origin", "https://www.roblox.com");
        // Roblox authentication-ticket API requires Content-Type: application/json with JSON body
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return request;
    }
}
