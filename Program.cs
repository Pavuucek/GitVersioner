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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace GitVersioner
{
    /// <summary>
    ///     Main class of GitVersioner
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///     The git executable name
        /// </summary>
        private static readonly string GitExeName = Environment.OSVersion.Platform == PlatformID.Unix
            ? "git"
            : "git.exe";

        private static bool _printMessages = true;

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
        private static string FindGitBinary()
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
        ///     Prints version info to console.
        /// </summary>
        private static void PrintInfo()
        {
            _printMessages = false;
            var gr = GetVersionInfo(Directory.GetCurrentDirectory());
            SetEnvironmentVariables(gr);
            Console.WriteLine(GitResultToString(gr));
        }

        /// <summary>
        ///     Sets the environment variables.
        /// </summary>
        /// <param name="gitResult">The git result.</param>
        private static void SetEnvironmentVariables(GitResult gitResult)
        {
            var fullVersionWithBranch =
                DoReplace("$Branch$:$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$",
                    gitResult);
            var fullVersion = DoReplace("$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$", gitResult);
            var fullSemVer =
                DoReplace("$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$", gitResult)
                    .Replace("-master", string.Empty);
            const EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;
            Environment.SetEnvironmentVariable("GV-FullVersionWithBranch", fullVersionWithBranch, target);
            Environment.SetEnvironmentVariable("GV-FullVersion", fullVersion, target);
            Environment.SetEnvironmentVariable("GV-SemVer", fullSemVer, target);
            Environment.SetEnvironmentVariable("GV-Branch", gitResult.Branch, target);
            Environment.SetEnvironmentVariable("GV-MajorVersion", gitResult.MajorVersion.ToString(), target);
            Environment.SetEnvironmentVariable("GV-MinorVersion", gitResult.MinorVersion.ToString(), target);
            Environment.SetEnvironmentVariable("GV-Revision", gitResult.Revision.ToString(), target);
            Environment.SetEnvironmentVariable("GV-Commit", gitResult.Commit.ToString(), target);
            Environment.SetEnvironmentVariable("GV-ShortHash", gitResult.ShortHash, target);
            Environment.SetEnvironmentVariable("GV-LongHash", gitResult.LongHash, target);
        }

        /// <summary>
        ///     Writes the information.
        /// </summary>
        /// <param name="sFile">The input file.</param>
        /// <param name="backup">Backup input file first.</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        private static void WriteInfo(string sFile, bool backup = true, bool append = false)
        {
            if (!File.Exists(sFile))
            {
                Console.WriteLine("Unable to find file {0}", sFile);
                return;
            }
            var bkp = sFile + ".gwbackup";
            //if (File.Exists(bkp)) return false;
            try
            {
                // zapis
                if (backup) File.Copy(sFile, bkp, true);
                var git = GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(sFile)));
                SetEnvironmentVariables(git);

                var contents = File.ReadAllLines(bkp,Encoding.UTF8);
                if (_printMessages) Console.WriteLine("Reading {0}...", sFile);
                if (_printMessages) Console.WriteLine("Replacing...");
                var output = new List<string>();
                foreach (var line in contents)
                {
                    if (line != null && line.Contains("AssemblyInformationalVersion")) append = false;
                    output.Add(DoReplace(line, git));
                }
                if (append)
                {
                    if (_printMessages) Console.WriteLine("Appending AssemblyInformationalVersion...");
                    output.Add(
                        DoReplace(
                            "[assembly: AssemblyInformationalVersion(\"$Branch$:$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$\")]",
                            git));
                }
                File.WriteAllLines(sFile, output.ToArray(),Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: '{0}' in '{1}'", e.Message, e.Source);
            }
            NotifyTeamCity();
        }

        /// <summary>
        ///     Gets the version information.
        /// </summary>
        /// <param name="workDir">The work dir.</param>
        /// <returns></returns>
        private static GitResult GetVersionInfo(string workDir)
        {
            if (_printMessages) Console.WriteLine("Getting version info for {0}", workDir);
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
            if (_printMessages) Console.WriteLine("Version info: {0}", GitResultToString(r));
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
        private static string GitResultToString(GitResult gr)
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

        /// <summary>
        ///     Does the replace.
        /// </summary>
        /// <param name="inString">The input string.</param>
        /// <param name="gr">The GitResult.</param>
        /// <returns></returns>
        private static string DoReplace(string inString, GitResult gr)
        {
            var r = inString.Replace("$MajorVersion$", gr.MajorVersion.ToString(CultureInfo.InvariantCulture));
            r = r.Replace("$MinorVersion$", gr.MinorVersion.ToString(CultureInfo.InvariantCulture));
            r = r.Replace("$Revision$", gr.Revision.ToString(CultureInfo.InvariantCulture));
            r = r.Replace("$Commit$", gr.Commit.ToString(CultureInfo.InvariantCulture));
            r = r.Replace("$ShortHash$", gr.ShortHash);
            r = r.Replace("$LongHash$", gr.LongHash);
            r = r.Replace("$Branch$", gr.Branch);
            return r;
        }

        /// <summary>
        ///     Restores the backup.
        /// </summary>
        /// <param name="sFile">The input file (without gwbackup extension).</param>
        private static void RestoreBackup(string sFile)
        {
            if (_printMessages) Console.WriteLine("Restoring {0}...", sFile);
            var bkp = sFile + ".gwbackup";
            if (!File.Exists(bkp)) return;
            try
            {
                File.Copy(bkp, sFile, true);
                File.Delete(bkp);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to restore backup {0}", bkp);
                Console.WriteLine("Error: '{0}' in '{1}'", e.Message, e.Source);
            }
        }

        /// <summary>
        ///     Main function
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("GitVersioner");
            if (string.IsNullOrEmpty(FindGitBinary()))
            {
                NoGit();
                return;
            }
            if (args.Length < 1)
            {
                ShowHelp();
                return;
            }
            string param;
            switch (args[0].ToLower())
            {
                // write mode (with backup)
                case "w":
                    if (args.Length < 2)
                    {
                        ShowHelp();
                        return;
                    }
                    WriteInfo(args[1]);
                    break;
                // restore mode (from backup)
                case "r":
                    if (args.Length < 2)
                    {
                        ShowHelp();
                        return;
                    }
                    RestoreBackup(args[1]);
                    break;
                // auto-rewrite mode
                case "a":
                    param = string.Empty;
                    if (args.Length <= 2)
                    {
                        for (var i = 1; i < args.Length; i++)
                        {
                            param += args[i] + " ";
                        }
                    }
                    param = param.Trim();
                    AutoSearchAndReplace(param);
                    break;
                // print mode (just print version info)
                case "p":
                    PrintInfo();
                    break;
                // notify: appveyor
                case "ba":
                    param = string.Empty;
                    if (args.Length < 2)
                    {
                        for (var i = 1; i < args.Length - 1; i++)
                        {
                            param += args[i] + " ";
                        }
                    }
                    param = param.Trim();
                    NotifyAppveyor(param);
                    break;

                default:
                    ShowHelp();
                    return;
            }
            if (_printMessages) Console.WriteLine("Finished!");
        }

        private static void AutoSearchAndReplace(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = Directory.GetCurrentDirectory() + "\\Properties\\AssemblyInfo.cs";
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Unable to find file {0}", fileName);
                return;
            }
            var gr = GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(fileName)));
            var contents = File.ReadAllText(fileName, Encoding.UTF8);
            var assemblyVersion = string.Format("{0}.{1}.{2}.{3}", gr.MajorVersion, gr.MinorVersion, gr.Revision,
                gr.Commit);
            var assemblyInfoVersion = string.Format("{0}:{1}.{2}.{3}-{4}-{5}", gr.Branch, gr.MajorVersion,
                gr.MinorVersion, gr.Revision, gr.Commit, gr.ShortHash);
            var assemblyFileVersion = string.Format("{0}.{1}.{2}.{3}", gr.MajorVersion, gr.MinorVersion, gr.Revision,
                gr.Commit);
            contents = Regex.Replace(contents, @"AssemblyVersion\(""[^""]*""\)",
                string.Format("AssemblyVersion(\"{0}\")", assemblyVersion));
            contents = Regex.Replace(contents, @"AssemblyInformationalVersion\(""[^""]*""\)",
                string.Format("AssemblyInformationalVersion(\"{0}\")", assemblyInfoVersion));
            contents = Regex.Replace(contents, @"AssemblyFileVersion\(""[^""]*""\)",
                string.Format("AssemblyFileVersion(\"{0}\")", assemblyFileVersion));
            File.WriteAllText(fileName, contents, Encoding.UTF8);
            NotifyTeamCity();
        }

        /// <summary>
        ///     Notifies the Teamcity. Since it's a simple console output run it i more places than needed :-)
        /// </summary>
        /// <param name="versionFormat">The version format.</param>
        private static void NotifyTeamCity(
            string versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$")
        {
            if (string.IsNullOrEmpty(versionFormat))
                versionFormat = "$Branch$-$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$";
            var gr = GetVersionInfo(Directory.GetCurrentDirectory());
            if (versionFormat.ToLower().Trim() == "semver")
                versionFormat = "$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$".Replace("-master",
                    string.Empty);
            versionFormat = DoReplace(versionFormat, gr);
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
            var gr = GetVersionInfo(Directory.GetCurrentDirectory());
            if (versionFormat.ToLower().Trim() == "semver")
                versionFormat = "$MajorVersion$.$MinorVersion$.$Revision$-$Branch$+$Commit$".Replace("-master",
                    string.Empty);
            versionFormat = DoReplace(versionFormat, gr);
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

        /// <summary>
        ///     Prints a message when Git is not found
        /// </summary>
        private static void NoGit()
        {
            Console.WriteLine("Unable to find Git binary!");
        }

        /// <summary>
        ///     Shows the help.
        /// </summary>
        private static void ShowHelp()
        {
            var exename = Path.GetFileName(Assembly.GetExecutingAssembly().ManifestModule.ToString());
            Console.WriteLine();
            Console.WriteLine("Usage: {0} [parameter] [file]", exename);
            Console.WriteLine("Supported parameters:");
            Console.WriteLine("W: * write version information to file and do a backup");
            Console.WriteLine("R: * restore file from backup");
            // TODO: write something intelligent here :-)
            Console.WriteLine("A: * Auto-Rewrite");
            Console.WriteLine("P: just prints version info");
            Console.WriteLine("BA: Send version info to Appveyor.exe");
            Console.WriteLine();
            Console.WriteLine("* = second parameter is expected");
            Console.WriteLine();
            Console.WriteLine("for example {0} w Properties\\AssemblyInfo.cs", exename);
            Console.WriteLine("or {0} r Properties\\AssemblyInfo.cs", exename);
            Console.WriteLine();
            Console.WriteLine("Supported replacement strings:");
            Console.WriteLine("$MajorVersion$");
            Console.WriteLine("$MinorVersion$");
            Console.WriteLine("$Revision$");
            Console.WriteLine("$Commit$");
            Console.WriteLine("$ShortHash$");
            Console.WriteLine("$LongHash$");
            Console.WriteLine("$Branch$");
            Console.WriteLine();
            // TODO: also write something about auto-rewrite mode here
            Console.WriteLine("");
        }

        /// <summary>
        ///     GitResult structure
        /// </summary>
        private struct GitResult
        {
            public string Branch;
            public int Commit;
            public string LongHash;
            public int MajorVersion;
            public int MinorVersion;
            public int Revision;
            public string ShortHash;
        }
    }
}