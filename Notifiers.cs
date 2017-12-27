/*
 * Copyright (c) 2006-2014 Michal Kuncl <michal.kuncl@gmail.com> http://www.pavucina.info
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 *
 * Uses some parts from GitRevision program by Yves Goergen
 * http://dev.unclassified.de/en/apps/gitrevisiontool
 * https://github.com/dg9ngf/GitRevisionTool
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GitVersioner
{
    internal static class Notifiers
    {
        /// <summary>
        ///     Notifies the Teamcity. Since it's a simple console output run it i more places than needed :-)
        /// </summary>
        /// <param name="versionFormat">The version format.</param>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        public static void NotifyTeamCity(
            string versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$")
        {
            var vformat = versionFormat;
            if (string.IsNullOrEmpty(vformat))
                vformat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$";
            var gr = GitHandler.GetVersionInfo(Directory.GetCurrentDirectory());
            if (vformat.ToLowerInvariant().Trim() == "semver")
                vformat = "$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$".Replace("-master",
                    string.Empty);
            vformat = Utilities.DoReplace(vformat, gr);
            Console.WriteLine("##teamcity[buildNumber '{0}']", vformat);
        }

        /// <summary>
        ///     Notifies Appveyor build process.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        public static void NotifyAppveyor(
            string versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$")
        {
            var vformat = versionFormat;
            if (string.IsNullOrEmpty(vformat))
                vformat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$";
            var gr = GitHandler.GetVersionInfo(Directory.GetCurrentDirectory());
            if (vformat.ToLowerInvariant().Trim() == "semver")
                vformat = "$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$".Replace("-master",
                    string.Empty);
            vformat = Utilities.DoReplace(vformat, gr);
            var psi = new ProcessStartInfo("Appveyor.exe", "UpdateBuild -Version " + vformat)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            try
            {
                Console.WriteLine("Starting Appveyor.exe UpdateBuild -Version " + versionFormat);
                var p = Process.Start(psi);
                var r = new StringBuilder();
                while (p != null && !p.StandardOutput.EndOfStream)
                    r.AppendLine(p.StandardOutput.ReadLine());
                if (p != null && !p.WaitForExit(1000))
                    p.Kill();
                Console.WriteLine(r.ToString());
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