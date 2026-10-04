using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObsQr;

// Minimal obs-websocket v5 client with auto-reconnect.
class ObsClient(Settings s)
{
    static readonly JsonSerializerOptions Opt = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> pending = new();
    readonly SemaphoreSlim sendLock = new(1, 1);
    ClientWebSocket ws;
    public volatile bool Connected;
    public event Action OnConnected;

    public async Task Run(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Session(ct); } catch { }
            Connected = false;
            foreach (var p in pending.Values) p.TrySetCanceled();
            pending.Clear();
            try { await Task.Delay(3000, ct); } catch { }
        }
    }

    async Task Session(CancellationToken ct)
    {
        using var w = new ClientWebSocket();
        await w.ConnectAsync(new Uri($"ws://127.0.0.1:{s.ObsPort}"), ct);
        ws = w;
        var d = (await Recv(w, ct)).GetProperty("d");
        string auth = null;
        if (d.TryGetProperty("authentication", out var a))
        {
            var secret = Sha(s.ObsPass + a.GetProperty("salt").GetString());
            auth = Sha(secret + a.GetProperty("challenge").GetString());
        }
        await Send(w, new { op = 1, d = new { rpcVersion = 1, authentication = auth, eventSubscriptions = 0 } }, ct);
        await Recv(w, ct); // Identified; a wrong password closes the socket here
        Connected = true;
        _ = Task.Run(() => OnConnected?.Invoke());
        while (true)
        {
            var m = await Recv(w, ct);
            if (m.GetProperty("op").GetInt32() != 7) continue;
            var rd = m.GetProperty("d");
            if (pending.TryRemove(rd.GetProperty("requestId").GetString(), out var t)) t.TrySetResult(rd);
        }
    }

    static string Sha(string v) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(v)));

    static async Task<JsonElement> Recv(ClientWebSocket w, CancellationToken ct)
    {
        var buf = new byte[16384];
        using var ms = new MemoryStream();
        WebSocketReceiveResult r;
        do
        {
            r = await w.ReceiveAsync(buf, ct);
            if (r.MessageType == WebSocketMessageType.Close) throw new Exception("closed");
            ms.Write(buf, 0, r.Count);
        } while (!r.EndOfMessage);
        return JsonDocument.Parse(ms.ToArray()).RootElement.Clone();
    }

    static Task Send(ClientWebSocket w, object o, CancellationToken ct) =>
        w.SendAsync(JsonSerializer.SerializeToUtf8Bytes(o, Opt), WebSocketMessageType.Text, true, ct);

    public async Task<JsonElement> Req(string type, object data = null)
    {
        var w = ws;
        if (!Connected || w == null) throw new Exception("OBS is not running or WebSocket is off");
        var id = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = tcs;
        await sendLock.WaitAsync();
        try { await Send(w, new { op = 6, d = new { requestType = type, requestId = id, requestData = data } }, CancellationToken.None); }
        finally { sendLock.Release(); }
        var r = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var st = r.GetProperty("requestStatus");
        if (!st.GetProperty("result").GetBoolean())
            throw new Exception(st.TryGetProperty("comment", out var c) ? c.GetString() : "OBS request failed");
        return r.TryGetProperty("responseData", out var rd) ? rd : default;
    }
}
