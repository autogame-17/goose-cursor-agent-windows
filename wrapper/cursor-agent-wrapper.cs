using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

public static class Program
{
    // 可通过环境变量覆盖（用于在不同电脑复用）
    // - CURSOR_AGENT_WSL_DISTRO: 例如 Ubuntu-24.04
    // - CURSOR_AGENT_WSL_USER: 例如 root
    // - CURSOR_AGENT_WSL_BIN: WSL 内 cursor-agent 路径，例如 /root/.local/bin/cursor-agent
    private static readonly string Distro = Environment.GetEnvironmentVariable("CURSOR_AGENT_WSL_DISTRO") ?? "Ubuntu-24.04";
    private static readonly string User = Environment.GetEnvironmentVariable("CURSOR_AGENT_WSL_USER") ?? "root";
    private static readonly string Bin = Environment.GetEnvironmentVariable("CURSOR_AGENT_WSL_BIN") ?? "/root/.local/bin/cursor-agent";

    private static string Quote(string arg)
    {
        if (arg == null) return "\"\"";
        if (arg.Length == 0) return "\"\"";

        bool needsQuotes = arg.Any(ch => char.IsWhiteSpace(ch) || ch == '"' || ch == '\\');
        if (!needsQuotes) return arg;

        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var ch in arg)
        {
            if (ch == '"') sb.Append("\\\"");
            else sb.Append(ch);
        }
        sb.Append('"');
        return sb.ToString();
    }

    public static int Main(string[] args)
    {
        var fixedArgs = new[]
        {
            "-d", Distro,
            "-u", User,
            "--",
            Bin
        };

        var allArgs = fixedArgs.Concat(args).Select(Quote);
        var psi = new ProcessStartInfo
        {
            FileName = @"C:\Windows\System32\wsl.exe",
            Arguments = string.Join(" ", allArgs),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var p = new Process { StartInfo = psi })
        {
            p.Start();

            // 直接按字节流转发，避免因为编码/缓冲导致“看起来没输出”
            var outThread = new Thread(() =>
            {
                try { p.StandardOutput.BaseStream.CopyTo(Console.OpenStandardOutput()); }
                catch { }
            })
            { IsBackground = true };

            var errThread = new Thread(() =>
            {
                try { p.StandardError.BaseStream.CopyTo(Console.OpenStandardError()); }
                catch { }
            })
            { IsBackground = true };

            outThread.Start();
            errThread.Start();

            p.WaitForExit();
            try { outThread.Join(2000); } catch { }
            try { errThread.Join(2000); } catch { }
            return p.ExitCode;
        }
    }
}

