# CipherDNS

Personal Windows tray app for encrypted DNS using the DNSCrypt protocol.
It runs the official [dnscrypt-proxy](https://github.com/DNSCrypt/dnscrypt-proxy) engine
(ISC licence) and lets you pick a DNSCrypt service from a list.

## Services
| Service | Type |
|---|---|
| Quad9 (malware blocking / no filtering) | Security / Privacy |
| AdGuard (ads + trackers / Family / no filtering) | Ad blocking / Family / Privacy |
| CleanBrowsing (Security / Adult / Family) | Security / Family |
| Cisco OpenDNS | Security |
| Scaleway (France / Amsterdam / Safe) | Privacy / Security |
| DNSCrypt.ca, Plan9 DNS, Serbica, DCT | Privacy |
| dnscry.pt (North America / Europe / Asia-Pacific) | Privacy |
| Automatic | Fastest no-log, DNSSEC DNSCrypt server |

Edit `Providers.cs` to add more; names come from the official
[public resolvers list](https://dnscrypt.info/public-servers).

## What it does
- **Connect:** writes a config to `%AppData%\CipherDNS`, starts the engine on `127.0.0.1:53`,
  saves each adapter's DNS settings, then points Windows DNS at the engine.
- **Disconnect / Quit:** stops the engine and restores the saved DNS settings.
- Closing the window keeps it running in the tray. On start-up it restores DNS left over from a crash.
- Requires administrator rights (port 53 and adapter DNS changes).

## Setup
```
powershell -ExecutionPolicy Bypass -File get-engine.ps1
dotnet build -c Release
```
Run `bin\Release\net10.0-windows\CipherDNS.exe`.

If you use an outbound-blocking firewall, allow `engine\dnscrypt-proxy.exe` for **TCP and UDP**.
