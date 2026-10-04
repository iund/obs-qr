# obs-qr

Windows tray app. A printed QR code opens a phone page, served by the PC, to start/stop an OBS stream.

Install: download `ObsQr-Setup-<version>.exe` from Releases and run it.

Setup:
- In OBS: Tools > WebSocket Server Settings > enable server, set a password.
- Tray icon > Set OBS WebSocket password.
- Tray icon > Print QR code, scan with a phone on the same Wi-Fi.

Notes:
- Port 5000 (TCP), allowed through the firewall for private/domain networks.
- QR URL contains a secret token; reprint if `%AppData%\ObsQr\settings.json` is deleted.
- OBS must already have a stream service/server configured; the app only sets the key.

Build: `dotnet publish src/ObsQr -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`; installer: Inno Setup on `installer/ObsQr.iss`. Pushing a `v*` tag builds and publishes a release.
