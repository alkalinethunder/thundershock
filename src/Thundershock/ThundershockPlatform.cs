using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Microsoft.Xna.Framework;

namespace Thundershock
{
    public static class ThundershockPlatform
    {
        [DllImport("kernel32.dll")]
        internal static extern bool AttachConsole(int dwProcessId);

        public static string OSName
            => Environment.OSVersion.VersionString;

        public static string DeveloperName
            => EntryPoint.EntryAssembly.GetCustomAttributes(false).OfType<AssemblyCompanyAttribute>().First()
                .Company;

        public static string TitleName
            => EntryPoint.EntryAssembly.GetCustomAttributes(false).OfType<AssemblyProductAttribute>().First()
                .Product;

        public static string LocalDataPath
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DeveloperName,
                TitleName);

        public static readonly string Community = "https://community.mvanoverbeek.me/";

        public static Color HtmlColor(string html)
        {
            var gdiColor = System.Drawing.ColorTranslator.FromHtml(html);
            return new Color(gdiColor.R, gdiColor.G, gdiColor.B, gdiColor.A);
        }

        public static Platform GetCurrentPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return Platform.MacOS;
                case PlatformID.WinCE:
                case PlatformID.Win32NT:
                case PlatformID.Win32Windows:
                case PlatformID.Win32S:
                case PlatformID.Xbox:
                    return Platform.Windows;
                case PlatformID.Unix:
                    return Platform.Linux;
            }

            return Platform.Unknown;
        }

        public static bool VerifyInternetConnection()
        {
            try
            {
                using var client = new WebClient();
                using (client.OpenRead("http://google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPlatform(Platform platform)
        {
            return GetCurrentPlatform() == platform;
        }

        public static IEnumerable<Type> GetAllTypes<T>()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    if (!typeof(T).IsAssignableFrom(type))
                        continue;

                    if (type.GetConstructor(Type.EmptyTypes) != null)
                        yield return type;
                }
            }
        }

        public static Process ExecProcess(string path, string[] args)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = path;
            foreach (var arg in args)
                startInfo.ArgumentList.Add(arg);
            startInfo.RedirectStandardOutput = true;

            return Process.Start(startInfo);
        }

        public static string GetProcessorName()
        {
            // thank you, John Tur.
            if (X86Base.IsSupported)
            {
                Span<int> text = stackalloc int[3 * 4];
                for (var i = 0; i < 3; i++)
                {
                    (text[i * 4 + 0],
                        text[i * 4 + 1],
                        text[i * 4 + 2],
                        text[i * 4 + 3]) = X86Base.CpuId((int) (i + 0x80000002), 0);
                }

                return Encoding.ASCII.GetString(MemoryMarshal.Cast<int, byte>(text));
            }
            else
            {
                return "Unknown";
            }
        }

        public static void OpenFile(string path)
        {
            var info = new ProcessStartInfo();
            info.UseShellExecute = true;
            info.FileName = path;
            Process.Start(info);
        }

        /// <summary>
        /// Retrieves the amount of total system memory in megabytes.
        /// </summary>
        /// <returns>Amount of system memory in megabytes.</returns>
        public static long GetTotalSystemMemory()
        {
            if (IsPlatform(Platform.Windows))
            {
                using var proc = ExecProcess("wmic", new[] {"OS", "get", "TotalVisibleMemorySize", "/Value"});
                var output = proc.StandardOutput.ReadToEnd();

                var trim = output.Trim();
                var split = trim.Split('=').Last();
                var kbytes = long.Parse(split);

                return kbytes / 1024;
            }
            else
            {
                using var proc = ExecProcess("/bin/bash", new[] {"-c", "free -m"});
                var output = proc.StandardOutput.ReadToEnd();

                var lines = output.Split('\n');
                var memLine = lines.First(x => x.StartsWith("Mem:"));
                var cols = memLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var total = cols[1];
                var mbytes = long.Parse(total);

                return mbytes;
            }
        }
    }

    public enum Platform
    {
        Windows,
        MacOS,
        Linux,
        Unknown,
    }
}
