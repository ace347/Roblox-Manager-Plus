using System.Net.Http;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace RobloxAccountManager.App.Views;

/// <summary>
/// Hosts an embedded WebView2 pointed at roblox.com/login.
/// After a successful login Roblox redirects to the home page; at that point
/// we pull the .ROBLOSECURITY cookie from our own WebView session (not from
/// any third-party browser store) and expose it via <see cref="CapturedCookie"/>,
/// <see cref="CapturedUsername"/>, and <see cref="CapturedDisplayName"/>.
/// </summary>
public partial class RobloxLoginWindow : Window
{
    private const string CookieName = ".ROBLOSECURITY";

    /// <summary>The raw .ROBLOSECURITY value captured after login (never stored plain in DB).</summary>
    public string? CapturedCookie { get; private set; }

    /// <summary>The Roblox username detected from the authenticated session.</summary>
    public string? CapturedUsername { get; private set; }

    /// <summary>The Roblox display name detected from the authenticated session.</summary>
    public string? CapturedDisplayName { get; private set; }

    public RobloxLoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebView.EnsureCoreWebView2Async();

        // Listen for every navigation so we can detect a successful login redirect
        WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        var url = WebView.Source?.ToString() ?? string.Empty;

        // Roblox redirects to /home after a successful login
        if (!url.Contains("/home", StringComparison.OrdinalIgnoreCase))
            return;

        StatusText.Text = "Login detected — capturing session…";

        // Extract .ROBLOSECURITY from our own WebView cookie store
        var cookieManager = WebView.CoreWebView2.CookieManager;
        var cookies = await cookieManager.GetCookiesAsync("https://www.roblox.com");
        var securityCookie = cookies.FirstOrDefault(c =>
            c.Name.Equals(CookieName, StringComparison.OrdinalIgnoreCase));

        if (securityCookie is null || string.IsNullOrWhiteSpace(securityCookie.Value))
        {
            StatusText.Text = "⚠ Could not read session cookie. Try logging in again.";
            return;
        }

        CapturedCookie = securityCookie.Value;

        // Fetch user profile info directly via Roblox Users API using the captured cookie
        var (username, displayName) = await FetchUserInfoAsync(CapturedCookie);
        CapturedUsername = username;
        CapturedDisplayName = displayName ?? username;

        if (string.IsNullOrWhiteSpace(CapturedUsername) || CapturedUsername == "{}")
        {
            StatusText.Text = "⚠ Could not verify username with Roblox API. Please retry.";
            return;
        }

        StatusText.Text = $"✅ Logged in as {CapturedDisplayName} (@{CapturedUsername}). Saving…";

        // Brief delay so the user can see the success confirmation
        await Task.Delay(900);
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Calls the official Roblox authenticated-users endpoint to retrieve the verified
    /// username and display name for this session cookie.
    /// </summary>
    private static async Task<(string? Username, string? DisplayName)> FetchUserInfoAsync(string cookie)
    {
        try
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://users.roblox.com/v1/users/authenticated");
            
            var cookieHeader = cookie.StartsWith(".ROBLOSECURITY=", StringComparison.OrdinalIgnoreCase)
                ? cookie
                : $".ROBLOSECURITY={cookie}";

            request.Headers.Add("Cookie", cookieHeader);
            request.Headers.Add("User-Agent", "RobloxAccountManager/1.0");

            var response = await client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string? name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                string? displayName = root.TryGetProperty("displayName", out var dispProp) ? dispProp.GetString() : null;

                return (name, displayName);
            }
        }
        catch
        {
            // Network or parse issue — caller will validate
        }

        return (null, null);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
