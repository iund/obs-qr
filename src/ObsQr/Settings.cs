using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObsQr;

class Settings
{
    static readonly string File = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ObsQr", "settings.json");
    public int Port { get; set; } = 5000;
    public int ObsPort { get; set; } = 4455;
    public string Token { get; set; } = "";
    public string KeyEnc { get; set; } = "";
    public string ObsPassEnc { get; set; } = "";
    public bool AutoStart { get; set; }
    [JsonIgnore] public bool IsNew { get; private set; }
    [JsonIgnore] public string Key { get => Dec(KeyEnc); set => KeyEnc = Enc(value); }
    [JsonIgnore] public string ObsPass { get => Dec(ObsPassEnc); set => ObsPassEnc = Enc(value); }

    static string Enc(string v) => string.IsNullOrEmpty(v) ? "" : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(v), null, DataProtectionScope.CurrentUser));
    static string Dec(string v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(v), null, DataProtectionScope.CurrentUser)); }
        catch { return ""; }
    }

    public static Settings Load()
    {
        Settings s = null;
        try { s = JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File)); } catch { }
        var isNew = s == null;
        s ??= new Settings();
        s.IsNew = isNew;
        if (s.Token == "") { s.Token = RandomNumberGenerator.GetHexString(16).ToLowerInvariant(); s.Save(); }
        return s;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File));
        System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this));
    }
}
