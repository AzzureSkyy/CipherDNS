namespace CipherDNS;

/// <summary>A DNSCrypt resolver from the official public-resolvers list.</summary>
public sealed record Provider(string Name, string Category, string Description, string[] ResolverNames);

public static class Providers
{
    public static readonly Provider[] All =
    [
        new("Quad9 (malware blocking)", "Security", "Swiss non-profit; blocks known malicious domains.",
            ["quad9-dnscrypt-ip4-filter-pri"]),
        new("Quad9 (no filtering)", "Privacy", "Swiss non-profit; no blocking.",
            ["quad9-dnscrypt-ip4-nofilter-pri"]),
        new("AdGuard (ads + trackers)", "Ad blocking", "Blocks ads, trackers and phishing.",
            ["adguard-dns"]),
        new("AdGuard Family", "Family", "AdGuard plus adult content blocking and safe search.",
            ["adguard-dns-family"]),
        new("AdGuard (no filtering)", "Privacy", "AdGuard servers without blocking.",
            ["adguard-dns-unfiltered"]),
        new("CleanBrowsing Security", "Security", "Blocks phishing and malware domains.",
            ["cleanbrowsing-security"]),
        new("CleanBrowsing Adult", "Family", "Blocks adult content and malicious domains.",
            ["cleanbrowsing-adult"]),
        new("CleanBrowsing Family", "Family", "Strictest: adult, mixed content, proxies/VPNs.",
            ["cleanbrowsing-family"]),
        new("Cisco OpenDNS", "Security", "Cisco's resolver with phishing protection.",
            ["cisco"]),
        new("Scaleway (France)", "Privacy", "No-log resolver in Paris.",
            ["scaleway-fr"]),
        new("Scaleway (Amsterdam)", "Privacy", "No-log resolver in Amsterdam.",
            ["scaleway-ams"]),
        new("Scaleway Safe (France)", "Security", "Scaleway with malware filtering.",
            ["sfw.scaleway-fr"]),
        new("DNSCrypt.ca (Canada)", "Privacy", "Non-logging Canadian resolver.",
            ["dnscrypt.ca-ipv4"]),
        new("Plan9 DNS (US)", "Privacy", "No-log resolvers in New Jersey and Florida.",
            ["plan9dns-nj", "plan9dns-fl"]),
        new("Serbica", "Privacy", "No-log resolver in the Netherlands.",
            ["serbica"]),
        new("DCT (Germany/France)", "Privacy", "Non-logging resolvers in Germany and France.",
            ["dct-de", "dct-fr"]),
        new("dnscry.pt North America", "Privacy", "Volunteer no-log network: NY, LA, Seattle, Toronto.",
            ["dnscry.pt-newyork-ipv4", "dnscry.pt-losangeles-ipv4", "dnscry.pt-seattle-ipv4", "dnscry.pt-toronto-ipv4"]),
        new("dnscry.pt Europe", "Privacy", "Volunteer no-log network: London, Paris, Frankfurt.",
            ["dnscry.pt-london-ipv4", "dnscry.pt-paris-ipv4", "dnscry.pt-frankfurt-ipv4"]),
        new("dnscry.pt Asia-Pacific", "Privacy", "Volunteer no-log network: Tokyo, Singapore.",
            ["dnscry.pt-tokyo-ipv4", "dnscry.pt-singapore-ipv4"]),
        new("Automatic (fastest DNSCrypt)", "Auto", "Let the engine pick the fastest no-log, DNSSEC server.",
            []),
    ];
}
