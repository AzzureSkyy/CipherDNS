using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace CipherDNS;

/// <summary>Runs the bundled dnscrypt-proxy engine and points Windows DNS at it.</summary>
public sealed class Engine : IDisposable
{
    readonly string engineDir = Path.Combine(AppContext.BaseDirectory, "engine");
    readonly string dataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CipherDNS");
    Process? process;

    public event Action<string>? Log;
    public bool Running => process is { HasExited: false };
    public Provider? Current { get; private set; }

    string ConfigPath => Path.Combine(dataDir, "dnscrypt-proxy.toml");
    string BackupPath => Path.Combine(dataDir, "dns-backup.txt");

    public void Start(Provider provider, bool blockIpv6)
    {
        Stop(restoreDns: false);
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(ConfigPath, BuildConfig(provider, blockIpv6));
        var psi = new ProcessStartInfo(Path.Combine(engineDir, "dnscrypt-proxy.exe"),
            $"-config \"{ConfigPath}\"")
        {
            WorkingDirectory = dataDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) Log?.Invoke(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Log?.Invoke(e.Data); };
        process.Exited += (_, _) => Log?.Invoke("Engine stopped.");
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        Current = provider;
        PointWindowsDnsHere();
    }

    public void Stop(bool restoreDns = true)
    {
        if (restoreDns) RestoreWindowsDns();
        if (process is { HasExited: false })
        {
            try { process.Kill(entireProcessTree: true); process.WaitForExit(3000); } catch { }
        }
        process?.Dispose();
        process = null;
        Current = null;
    }

    static string BuildConfig(Provider provider, bool blockIpv6)
    {
        var sb = new StringBuilder();
        if (provider.ResolverNames.Length > 0)
            sb.AppendLine("server_names = [" +
                string.Join(", ", provider.ResolverNames.Select(n => $"'{n}'")) + "]");
        sb.AppendLine("""
            listen_addresses = ['127.0.0.1:53', '[::1]:53']
            max_clients = 250
            ipv4_servers = true
            ipv6_servers = false
            dnscrypt_servers = true
            doh_servers = false
            odoh_servers = false
            require_dnssec = false
            require_nolog = false
            require_nofilter = false
            force_tcp = false
            timeout = 5000
            keepalive = 30
            lb_strategy = 'p2'
            cert_refresh_delay = 240
            bootstrap_resolvers = ['9.9.9.11:53', '8.8.8.8:53']
            ignore_system_dns = true
            netprobe_timeout = 60
            netprobe_address = '9.9.9.9:53'
            log_level = 2
            cache = true
            cache_size = 4096
            cache_min_ttl = 2400
            cache_max_ttl = 86400
            cache_neg_min_ttl = 60
            cache_neg_max_ttl = 600
            """);
        sb.AppendLine($"block_ipv6 = {(blockIpv6 ? "true" : "false")}");
        sb.AppendLine("""

            [sources]
              [sources.public-resolvers]
                urls = ['https://raw.githubusercontent.com/DNSCrypt/dnscrypt-resolvers/master/v3/public-resolvers.md', 'https://download.dnscrypt.info/resolvers-list/v3/public-resolvers.md']
                cache_file = 'public-resolvers.md'
                minisign_key = 'RWQf6LRCGA9i53mlYecO4IzT51TGPpvWucNSCh1CBM0QTaLn73Y7GFO3'
                refresh_delay = 73
            """);
        if (provider.ResolverNames.Length == 0)
        {
            // Automatic mode: only no-log, DNSSEC-capable DNSCrypt servers.
            return sb.ToString()
                .Replace("require_dnssec = false", "require_dnssec = true")
                .Replace("require_nolog = false", "require_nolog = true");
        }
        return sb.ToString();
    }

    static IEnumerable<NetworkInterface> ActiveAdapters() =>
        NetworkInterface.GetAllNetworkInterfaces().Where(n =>
            n.OperationalStatus == OperationalStatus.Up &&
            n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel &&
            n.GetIPProperties().GatewayAddresses.Count > 0);

    void PointWindowsDnsHere()
    {
        if (!File.Exists(BackupPath))
        {
            // Remember each adapter's DNS: "dhcp" or a static list, so Stop can restore it.
            var lines = ActiveAdapters().Select(a =>
            {
                var dns = a.GetIPProperties().DnsAddresses
                    .Where(d => d.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                var dhcp = IsDhcpDns(a.Name);
                return $"{a.Name}\t{(dhcp ? "dhcp" : string.Join(",", dns))}";
            });
            File.WriteAllLines(BackupPath, lines);
        }
        foreach (var adapter in ActiveAdapters())
        {
            RunPs($"Set-DnsClientServerAddress -InterfaceAlias '{adapter.Name}' -ServerAddresses ('127.0.0.1','::1')");
            Log?.Invoke($"DNS for '{adapter.Name}' set to CipherDNS.");
        }
        RunPs("Clear-DnsClientCache");
    }

    public void RestoreWindowsDns()
    {
        if (!File.Exists(BackupPath)) return;
        foreach (var line in File.ReadAllLines(BackupPath))
        {
            var parts = line.Split('\t');
            if (parts.Length != 2) continue;
            var cmd = parts[1] is "dhcp" or ""
                ? $"Set-DnsClientServerAddress -InterfaceAlias '{parts[0]}' -ResetServerAddresses"
                : $"Set-DnsClientServerAddress -InterfaceAlias '{parts[0]}' -ServerAddresses ({string.Join(",", parts[1].Split(',').Select(s => $"'{s}'"))})";
            RunPs(cmd);
            Log?.Invoke($"DNS for '{parts[0]}' restored.");
        }
        File.Delete(BackupPath);
        RunPs("Clear-DnsClientCache");
    }

    static bool IsDhcpDns(string alias)
    {
        // Static DNS is stored in the NameServer registry value; DHCP DNS leaves it empty.
        var guid = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Name == alias)?.Id;
        if (guid == null) return true;
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{guid}");
        return string.IsNullOrWhiteSpace(key?.GetValue("NameServer") as string);
    }

    static string RunPs(string command)
    {
        var psi = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -NonInteractive -Command \"{command.Replace("\"", "\\\"")}\"")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(15000);
        return output;
    }

    /// <summary>Resolves a name through the local engine to confirm it is answering.</summary>
    public static async Task<string> TestAsync()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var addresses = await Dns.GetHostAddressesAsync("example.com");
            return $"example.com -> {addresses.FirstOrDefault()} in {sw.ElapsedMilliseconds} ms";
        }
        catch (Exception e) { return "Lookup failed: " + e.Message; }
    }

    public void Dispose() => Stop();
}
