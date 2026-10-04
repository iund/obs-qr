# obs-qr

Print out a QR code then scan it later to control an active OBS stream. Connect to the same wifi as a PC running OBS and this tray app and control it via the web browser.

To install, download the latest installer from [Releases](https://github.com/iund/obs-qr/releases) and run it.

Setup:
- In OBS: Tools > WebSocket Server Settings > enable server, set a password.
- Tray icon > Set OBS WebSocket password.
- Tray icon > Print QR code, scan with a phone on the same Wi-Fi.

Notes:
- Port 5000 (TCP), allowed through the firewall for private/domain networks.
- QR URL contains a secret token; reprint if `%AppData%\ObsQr\settings.json` is deleted.
- OBS must already have a stream service/server configured; the app only sets the key.
