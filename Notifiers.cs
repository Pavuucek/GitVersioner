using System;
using System.Diagnostics;
using System.IO;

namespace GitVersioner
{
    internal static class Notifiers
    {
        /// <summary>
        ///     Notifies the Teamcity. Since it's a simple console output run it i more places than needed :-)
        /// </summary>
        /// <param name="versionFormat">The version format.</param>
        private static void NotifyTeamCity(
            string versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$")
        {
            if (string.IsNullOrEmpty(versionFormat))
                versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$";
            var gr = Program.GetVersionInfo(Directory.GetCurrentDirectory());
            if (versionFormat.ToLower().Trim() == "semver")
                versionFormat = "$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$".Replace("-master",
                    string.Empty);
            versionFormat = Program.DoReplace(versionFormat, gr);
            Console.WriteLine("##teamcity[buildNumber '{0}']", versionFormat);
        }

        /// <summary>
        ///     Notifies Appveyor build process.
        /// </summary>
        private static void NotifyAppveyor(
            string versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$")
        {
            if (string.IsNullOrEmpty(versionFormat))
                versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$";
            var gr = Program.GetVersionInfo(Directory.GetCurrentDirectory());
            if (versionFormat.ToLower().Trim() == "semver")
                versionFormat = "$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$".Replace("-master",
                    string.Empty);
            versionFormat = Program.DoReplace(versionFormat, gr);
            var psi = new ProcessStartInfo("Appveyor.exe", "UpdateBuild -Version " + versionFormat)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            try
            {
                Console.WriteLine("Starting Appveyor.exe UpdateBuild -Version " + versionFormat);
                var p = Process.Start(psi);
                var r = string.Empty;
                while (p != null && !p.StandardOutput.EndOfStream)
                {
                    r += p.StandardOutput.ReadLine() + "\n";
                }
                if (p != null && !p.WaitForExit(1000))
                {
                    p.Kill();
                }
                Console.WriteLine(r);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Cannot find Appveyor binary! Error message follows:");
                Console.WriteLine(e.ToString());
            }
            // also notify teamcity...
            Console.WriteLine("##teamcity[buildNumber '{0}']", versionFormat);
        }
    }
}