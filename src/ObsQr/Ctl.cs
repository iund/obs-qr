using System.Diagnostics;
using System.Text.Json.Nodes;

namespace ObsQr;

// Stream actions and cached live status.
class Ctl(Settings s, ObsClient obs)
{
    record St(bool Live, bool Rec, string Up, double Kbps, double Fps, long Drop, long Total, double Cpu, string Scene);
    volatile St st = new(false, false, "00:00:00", 0, 0, 0, 0, 0, "");
    long lastBytes; long lastTick;

    public void Start(CancellationToken ct)
    {
        obs.OnConnected += AutoStart;
        _ = Loop(ct);
    }

    async void AutoStart()
    {
        try
        {
            await Task.Delay(1500);
            if (!s.AutoStart || s.Key == "") return;
            if (!(await obs.Req("GetStreamStatus")).GetProperty("outputActive").GetBoolean()) await Begin();
        }
        catch { }
    }

    async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { if (obs.Connected) await Poll(); else st = st with { Live = false, Rec = false, Kbps = 0, Fps = 0 }; } catch { }
            try { await Task.Delay(1000, ct); } catch { }
        }
    }

    async Task Poll()
    {
        var a = await obs.Req("GetStreamStatus");
        var b = await obs.Req("GetStats");
        var c = await obs.Req("GetSceneList");
        var live = a.GetProperty("outputActive").GetBoolean();
        var bytes = a.GetProperty("outputBytes").GetInt64();
        var now = Stopwatch.GetTimestamp();
        var kbps = live && lastTick > 0 && bytes >= lastBytes ? (bytes - lastBytes) * 8.0 / Stopwatch.GetElapsedTime(lastTick, now).TotalSeconds / 1000 : 0;
        lastBytes = bytes; lastTick = now;
        var up = a.GetProperty("outputTimecode").GetString() ?? "";
        var dot = up.IndexOf('.');
        st = new(live, a.GetProperty("outputReconnecting").GetBoolean(), dot > 0 ? up[..dot] : "00:00:00", kbps,
            b.GetProperty("activeFps").GetDouble(), a.GetProperty("outputSkippedFrames").GetInt64(), a.GetProperty("outputTotalFrames").GetInt64(),
            b.GetProperty("cpuUsage").GetDouble(), c.GetProperty("currentProgramSceneName").GetString());
    }

    public object Snapshot()
    {
        var x = st; var k = s.Key;
        return new
        {
            obs = obs.Connected, live = x.Live, reconnecting = x.Rec, uptime = x.Up, kbps = Math.Round(x.Kbps), fps = Math.Round(x.Fps),
            dropped = x.Drop, total = x.Total, cpu = Math.Round(x.Cpu, 1), scene = x.Scene,
            hasKey = k != "", keyHint = k.Length > 4 ? "••••" + k[^4..] : "••••", auto = s.AutoStart
        };
    }

    public async Task StartStream(string key, bool auto)
    {
        if (!string.IsNullOrWhiteSpace(key)) s.Key = key.Trim();
        s.AutoStart = auto; s.Save();
        if (s.Key == "") throw new Exception("Enter a stream key");
        await Begin();
    }

    public void SetAuto(bool auto) { s.AutoStart = auto; s.Save(); }

    async Task Begin()
    {
        // Keep OBS's current service and server; only replace the key.
        var cur = await obs.Req("GetStreamServiceSettings");
        var set = JsonNode.Parse(cur.GetProperty("streamServiceSettings").GetRawText()).AsObject();
        set["key"] = s.Key;
        await obs.Req("SetStreamServiceSettings", new { streamServiceType = cur.GetProperty("streamServiceType").GetString(), streamServiceSettings = set });
        await obs.Req("StartStream");
    }

    public Task StopStream() => obs.Req("StopStream");
}
