using System.Diagnostics;

namespace CipherDNS;

// A Run-key entry can't launch a requireAdministrator app without a UAC prompt,
// so startup uses a logon scheduled task with highest privileges instead.
static class Startup
{
    const string TaskName = "CipherDNS";
    static readonly string LastFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CipherDNS", "last-provider.txt");

    public static bool IsEnabled() => Schtasks($"/Query /TN \"{TaskName}\"") == 0;

    public static bool Set(bool enable)
    {
        if (!enable) return Schtasks($"/Delete /TN \"{TaskName}\" /F") == 0;
        var exe = Environment.ProcessPath!;
        return Schtasks($"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\" --tray\" /SC ONLOGON /RL HIGHEST /F") == 0;
    }

    public static string? LastProvider =>
        File.Exists(LastFile) ? File.ReadAllText(LastFile).Trim() : null;

    public static void SaveLastProvider(string? name)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LastFile)!);
        if (name is null) File.Delete(LastFile);
        else File.WriteAllText(LastFile, name);
    }

    static int Schtasks(string args)
    {
        using var p = Process.Start(new ProcessStartInfo("schtasks.exe", args)
        {
            CreateNoWindow = true, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true,
        })!;
        p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode;
    }
}
