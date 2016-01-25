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

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace GitVersioner
{
    internal static class GitHandler
    {
        /// <summary>
        ///     The git executable name
        /// </summary>
        private static readonly string GitExeName = Environment.OSVersion.Platform == PlatformID.Unix
            ? "git"
            : "git.exe";

        /// <summary>
        ///     Gets a value indicating whether OS is 64bit.
        /// </summary>
        /// <value>
        ///     <c>true</c> if [is64 bit]; otherwise, <c>false</c>.
        /// </value>
        private static bool Is64Bit
        {
            get
            {
                return IntPtr.Size == 8 ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
            }
        }

        /// <summary>
        ///     Gets Program Files directory
        /// </summary>
        /// <returns>Program Files or Program Files (x86) directory</returns>
        private static string ProgramFilesX86()
        {
            string result;
            if (Is64Bit)
            {
                result = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            else
            {
                result = Environment.GetEnvironmentVariable("ProgramFiles");
            }
            if (string.IsNullOrEmpty(result)) result = @"C:\Program Files\";
            return result;
        }

        /// <summary>
        ///     Finds the git binary.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SecurityException">The caller does not have the required permission to perform this operation. </exception>
        /// <exception cref="IOException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> that contains the specified value has
        ///     been marked for deletion.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The user does not have the necessary registry rights.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        public static string FindGitBinary()
        {
            string git = null;
            RegistryKey key;

            // Try the PATH environment variable

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    var sdir = dir;
                    if (sdir.StartsWith("\"") && sdir.EndsWith("\""))
                    {
                        // Strip quotes (no Path.PathSeparator supported in quoted directories though)
                        sdir = sdir.Substring(1, sdir.Length - 2);
                    }
                    git = Path.Combine(sdir, GitExeName);
                    if (File.Exists(git)) break;
                }
            if (!File.Exists(git)) git = null;

            // Read registry uninstaller key
            if (git == null)
            {
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1");
                if (key != null)
                {
                    var loc = key.GetValue("InstallLocation");
                    if (loc is string)
                    {
                        git = Path.Combine((string)loc, Path.Combine("bin", GitExeName));
                        if (!File.Exists(git)) git = null;
                    }
                }
            }

            // Try 64-bit registry key
            if (git == null && Is64Bit)
            {
                key =
                    Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1");
                if (key != null)
                {
                    var loc = key.GetValue("InstallLocation");
                    if (loc is string)
                    {
                        git = Path.Combine((string)loc, Path.Combine("bin", GitExeName));
                        if (!File.Exists(git)) git = null;
                    }
                }
            }

            // Search program files directory
            if (git == null)
            {
                foreach (
                    var dir in
                        Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            "git*"))
                {
                    git = Path.Combine(dir, Path.Combine("bin", GitExeName));
                    if (!File.Exists(git)) git = null;
                }
            }

            // Try 32-bit program files directory
            if (git != null || !Is64Bit) return git;
            foreach (var dir in Directory.GetDirectories(ProgramFilesX86(), "git*"))
            {
                git = Path.Combine(dir, Path.Combine("bin", GitExeName));
                if (!File.Exists(git)) git = null;
            }

            return git;
        }

        /// <summary>
        ///     Gets the version information.
        /// </summary>
        /// <param name="workDir">The work dir.</param>
        /// <returns></returns>
        public static GitResult GetVersionInfo(string workDir)
        {
            if (Program.PrintMessages) Console.WriteLine("Getting version info for {0}", workDir);
            var lines = ExecGit(workDir, "describe --long --tags --always");
            GitResult r;
            r.MajorVersion = 0;
            r.MinorVersion = 0;
            r.Revision = 0;
            r.Commit = 0;
            r.ShortHash = "";
            // ocekavany retezec ve formatu: 1.7.6-235-g0a52e4b
            //lines = "g0a52e4b";
            var part1 = lines.Split('-');
            if (part1.Length >= 3)
            {
                // druhou cast rozdelit po teckach
                var part2 = part1[0].Split('.');
                if (part2.Length > 1)
                {
                    // delsi nez 1: mame major a minor verzi
                    if (part2.Length > 2)
                    {
                        // mame i revizi
                        try
                        {
                            r.Revision = Convert.ToInt32(part2[2]);
                        }
                        catch
                        {
                            r.Revision = 0;
                        }
                    }
                    try
                    {
                        r.MinorVersion = Convert.ToInt32(part2[1]);
                    }
                    catch
                    {
                        r.MinorVersion = 0;
                    }
                }
                // mame jen major verzi
                try
                {
                    var s = part2[0].ToLower();
                    // kdyby nahodou nekdo chtel pojmenovavat git tagy v1.0.0 atd (tj zacinajci ne cislem ale v)
                    if (s[0] == 'v')
                    {
                        s = s.Remove(0, 1);
                    }
                    r.MajorVersion = Convert.ToInt32(s);
                }
                catch
                {
                    r.MajorVersion = 0;
                }
            }
            try
            {
                r.Commit = Convert.ToInt32(part1[1]);
            }
            catch
            {
                r.Commit = 0;
            }
            try
            {
                r.ShortHash = part1[2];
            }
            catch
            {
                r.ShortHash = lines;
            }
            r.ShortHash = r.ShortHash.Trim();
            //
            r.Branch = ExecGit(workDir, "rev-parse --abbrev-ref HEAD").Trim();
            // we don't want branches to be called HEAD...
            if (r.Branch == "HEAD")
                r.Branch = ExecGit(workDir, "describe --all").Trim().Replace("heads/", string.Empty);
            r.LongHash = ExecGit(workDir, "rev-parse HEAD").Trim();
            if (Program.PrintMessages) Console.WriteLine("Version info: {0}", GitResultToString(r));
            if (string.IsNullOrEmpty(lines))
            {
                Console.WriteLine("Possible error, git output follows:\n {0}", lines);
            }
            return r;
        }

        /// <summary>
        ///     Converts git results to string
        /// </summary>
        /// <param name="gr">GitResult.</param>
        /// <returns></returns>
        public static string GitResultToString(GitResult gr)
        {
            var s = gr.MajorVersion + ".";
            s += gr.MinorVersion + ".";
            s += gr.Revision + "-";
            s += gr.Commit + "-";
            s += gr.ShortHash;
            return s;
        }

        /// <summary>
        ///     Executes the git program.
        /// </summary>
        /// <param name="workDir">The work dir.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        private static string ExecGit(string workDir, string parameters)
        {
            var psi = new ProcessStartInfo(FindGitBinary(), parameters)
            {
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
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
            return r;
        }
    }
}