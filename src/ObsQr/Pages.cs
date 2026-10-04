namespace ObsQr;

static class Pages
{
    public const string Login = """
<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>OBS Stream Control</title>
<body style="font:16px system-ui;background:#14161a;color:#e8eaed;padding:16px;max-width:480px;margin:auto;display:grid;gap:12px">
<h2 style="margin:4px 0">OBS Stream Control</h2><label for="pin">PIN</label>
<input id="pin" type="password" inputmode="numeric" autocomplete="off" style="padding:12px;font-size:16px;border-radius:8px;border:1px solid #333a45;background:#1e2128;color:inherit">
<button id="go" style="padding:14px;font-size:16px;font-weight:600;border:0;border-radius:8px;background:#4c8dff;color:#fff">Unlock</button><div id="m" style="color:#e74c3c"></div>
<script>
go.onclick=async()=>{const r=await fetch('/api/login',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({pin:pin.value})});
 if(r.ok)location.reload();else m.textContent='Wrong PIN'};
pin.onkeydown=e=>{if(e.key==='Enter')go.click()};
</script>
""";

    public const string Print = """
<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Print QR code</title>
<style>
body{font:16px system-ui,sans-serif;margin:0;display:grid;justify-items:center;gap:16px;padding:24px}
.sheet{display:grid;justify-items:center;gap:12px;text-align:center;max-width:420px}
.sheet svg{width:300px;height:300px}
h1{margin:0;font-size:26px}p{margin:0}code{word-break:break-all;font-size:12px}
button{font-size:16px;padding:12px 20px}
@media print{button{display:none}}
</style>
<div class="sheet"><h1>Scan to control the stream</h1>@@QR@@<p>Connect your phone to @@WIFI@@, then scan.</p>
<p>If it does not open, type:<br><code>@@FALLBACK@@</code></p></div>
<button onclick="print()">Print</button>
""";

    public const string Phone = """
<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>OBS Stream Control</title>
<style>
:root{--bg:#14161a;--card:#1e2128;--well:#14161a;--line:#333a45;--fg:#e8eaed;--mut:#8b919c;--ok:#2ecc71;--bad:#e74c3c;--warn:#f1c40f;--acc:#4c8dff;color-scheme:dark}
@media (prefers-color-scheme:light){:root{--bg:#f3f4f7;--card:#fff;--well:#eef0f4;--line:#d3d8e0;--fg:#1a1d23;--mut:#5d6572;--ok:#1e8e4e;--bad:#c9382b;--acc:#2a6be0;color-scheme:light}}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--fg);font:16px system-ui,sans-serif;padding:16px;display:flex;justify-content:center}
main{width:100%;max-width:480px;display:grid;gap:12px;min-width:0}
h1{font-size:18px;margin:4px 0;display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}
.card{background:var(--card);border-radius:12px;padding:14px;display:grid;gap:10px}
label{font-size:13px;color:var(--mut)}
.row{display:flex;gap:8px;align-items:center}
input[type=password],input[type=text]{flex:1;min-width:0;padding:12px;border-radius:8px;border:1px solid var(--line);background:var(--well);color:var(--fg);font-size:16px}
button{border:0;border-radius:8px;padding:14px;font-size:16px;font-weight:600;color:#fff;cursor:pointer}
button:disabled{opacity:.4}
.eye{background:var(--line);color:var(--fg);padding:12px}
#start{background:var(--ok);flex:1}#stop{background:var(--bad);flex:1}
.sw{display:flex;justify-content:space-between;align-items:center;gap:12px}
.sw small{display:block;color:var(--mut);font-size:12px}
input[type=checkbox]{width:44px;height:24px;accent-color:var(--acc);flex:none}
.pill{padding:4px 10px;border-radius:99px;font-size:13px;font-weight:700;background:var(--line);color:var(--fg)}
.pill.live{background:var(--bad);color:#fff}.pill.wait{background:var(--warn);color:#000}
.grid{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.stat{background:var(--well);border-radius:8px;padding:10px;min-width:0}.stat b{display:block;font-size:20px;font-variant-numeric:tabular-nums}.stat span{font-size:12px;color:var(--mut)}
#msg{font-size:13px;color:var(--mut);min-height:18px}
</style>
<main>
<h1>OBS Stream Control <span class="pill" id="obs">OBS …</span></h1>
<section class="card">
  <label for="key">Stream key</label>
  <div class="row"><input id="key" type="password" autocomplete="off"><button class="eye" id="eye" aria-label="Show key">👁</button></div>
  <div class="sw"><div>Auto-start with last key<small>Starts streaming when OBS launches on the PC</small></div><input type="checkbox" id="auto"></div>
  <div class="row"><button id="start">▶ Start stream</button><button id="stop" disabled>■ Stop stream</button></div>
  <div id="msg"></div>
</section>
<section class="card">
  <div class="sw"><b>Status</b><span class="pill" id="state">Offline</span></div>
  <div class="grid">
    <div class="stat"><b id="up">00:00:00</b><span>Uptime</span></div>
    <div class="stat"><b id="br">0 kbps</b><span>Bitrate</span></div>
    <div class="stat"><b id="fps">0</b><span>FPS</span></div>
    <div class="stat"><b id="drop">0 (0%)</b><span>Dropped frames</span></div>
    <div class="stat"><b id="cpu">0%</b><span>CPU</span></div>
    <div class="stat"><b id="scene">-</b><span>Scene</span></div>
  </div>
</section>
</main>
<script>
const $=id=>document.getElementById(id);let seeded=false;
const post=(u,b)=>fetch(u,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(b||{})}).then(async r=>{if(!r.ok){let e={};try{e=await r.json()}catch{}throw new Error(e.error||'Request failed')}});
$('eye').onclick=()=>$('key').type=$('key').type==='password'?'text':'password';
$('auto').onchange=()=>post('/api/auto',{auto:$('auto').checked}).catch(()=>{});
$('start').onclick=async()=>{$('start').disabled=true;$('msg').textContent='Sending to OBS…';
 try{await post('/api/start',{key:$('key').value,auto:$('auto').checked});$('key').value='';$('msg').textContent=''}
 catch(e){$('msg').textContent=e.message}};
$('stop').onclick=async()=>{$('stop').disabled=true;try{await post('/api/stop');$('msg').textContent=''}catch(e){$('msg').textContent=e.message}};
function set(s,c){$('state').textContent=s;$('state').className='pill '+c}
async function poll(){
 try{const r=await fetch('/api/status');if(r.status==401){location.reload();return}if(!r.ok)throw 0;const s=await r.json();
  if(!seeded){seeded=true;$('auto').checked=s.auto}
  $('key').placeholder=s.hasKey?'Saved key '+s.keyHint:'Paste stream key';
  $('obs').textContent=s.obs?'OBS connected':'OBS not running';
  set(!s.obs?'OBS not running':s.reconnecting?'Reconnecting…':s.live?'● LIVE':'Offline',s.live?'live':s.reconnecting?'wait':'');
  $('start').disabled=!s.obs||s.live;$('stop').disabled=!s.obs||!s.live;
  $('up').textContent=s.uptime;$('br').textContent=s.kbps+' kbps';$('fps').textContent=s.fps;
  $('drop').textContent=s.dropped+' ('+(s.total?(100*s.dropped/s.total).toFixed(1):'0.0')+'%)';
  $('cpu').textContent=s.cpu+'%';$('scene').textContent=s.scene||'-';
 }catch{$('obs').textContent='PC unreachable';set('Unreachable','wait')}
 setTimeout(poll,1000)}
poll();
</script>
""";
}
