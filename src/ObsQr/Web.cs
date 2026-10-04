using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;

namespace ObsQr;

static class Web
{
    public static string HostUrl(Settings s) => $"http://{Environment.MachineName.ToLowerInvariant()}.local:{s.Port}/?t={s.Token}";

    static string IpUrl(Settings s)
    {
        var ip = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.GetIPProperties().GatewayAddresses.Count > 0)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
        return $"http://{ip}:{s.Port}/?t={s.Token}";
    }

    static string Ssid()
    {
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", "wlan show interfaces") { RedirectStandardOutput = true, CreateNoWindow = true });
            foreach (var l in p.StandardOutput.ReadToEnd().Split('\n'))
            {
                var t = l.Trim();
                if (t.StartsWith("SSID") && t.Contains(':')) return t[(t.IndexOf(':') + 1)..].Trim();
            }
        }
        catch { }
        return "";
    }

    static string PrintPage(Settings s)
    {
        var svg = new SvgQRCode(new QRCodeGenerator().CreateQrCode(HostUrl(s), QRCodeGenerator.ECCLevel.M)).GetGraphic(6);
        var ssid = Ssid();
        return Pages.Print.Replace("@@QR@@", svg).Replace("@@WIFI@@", ssid == "" ? "the same Wi-Fi as this PC" : $"Wi-Fi <b>{System.Net.WebUtility.HtmlEncode(ssid)}</b>")
            .Replace("@@FALLBACK@@", System.Net.WebUtility.HtmlEncode(IpUrl(s)));
    }

    public record StartReq(string Key, bool Auto);
    public record AutoReq(bool Auto);

    public static WebApplication Build(Settings s, Ctl ctl)
    {
        var b = WebApplication.CreateBuilder();
        b.WebHost.UseUrls($"http://0.0.0.0:{s.Port}");
        b.Logging.ClearProviders();
        var app = b.Build();
        bool Authed(HttpContext c) => c.Request.Cookies["t"] == s.Token;

        app.MapGet("/", (HttpContext c) =>
        {
            if (c.Request.Query["t"] == s.Token)
            {
                c.Response.Cookies.Append("t", s.Token, new CookieOptions { HttpOnly = true, MaxAge = TimeSpan.FromDays(3650), SameSite = SameSiteMode.Lax });
                return Results.Redirect("/");
            }
            return Results.Content(Authed(c) ? Pages.Phone : Pages.Denied, "text/html; charset=utf-8");
        });
        app.MapGet("/print", (HttpContext c) =>
            c.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip) ? Results.Content(PrintPage(s), "text/html; charset=utf-8") : Results.NotFound());
        app.MapGet("/api/status", (HttpContext c) => Authed(c) ? Results.Json(ctl.Snapshot()) : Results.Unauthorized());
        app.MapPost("/api/start", async (HttpContext c, StartReq r) =>
        {
            if (!Authed(c)) return Results.Unauthorized();
            try { await ctl.StartStream(r.Key, r.Auto); return Results.Ok(); }
            catch (Exception e) { return Results.BadRequest(new { error = e.Message }); }
        });
        app.MapPost("/api/stop", async (HttpContext c) =>
        {
            if (!Authed(c)) return Results.Unauthorized();
            try { await ctl.StopStream(); return Results.Ok(); }
            catch (Exception e) { return Results.BadRequest(new { error = e.Message }); }
        });
        app.MapPost("/api/auto", (HttpContext c, AutoReq r) =>
        {
            if (!Authed(c)) return Results.Unauthorized();
            ctl.SetAuto(r.Auto); return Results.Ok();
        });
        return app;
    }
}
