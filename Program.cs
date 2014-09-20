using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace GitVersioner
{
    static class Program
    {
        private struct GitResult
        {
            public int MajorVersion;
            public int MinorVersion;
            public int Revision;
            public int Commit;
            public string ShortHash;
            public string LongHash;
            public string Branch;
        }

        static readonly string GitExeName = Environment.OSVersion.Platform == PlatformID.Unix ? "git" : "git.exe";

        private static string ProgramFilesX86()
        {
            if (Is64Bit)
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private static bool Is64Bit
        {
            get
            {
                return IntPtr.Size == 8 ||
                    !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
            }
        }

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
                    object loc = key.GetValue("InstallLocation");
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
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1");
                if (key != null)
                {
                    object loc = key.GetValue("InstallLocation");
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
                foreach (string dir in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "git*"))
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


        private static bool WriteInfo(string sFile, bool backup = true)
        {
            if (!File.Exists(sFile)) return false;
            var bkp = sFile + ".gwbackup";
            //if (File.Exists(bkp)) return false;
            File.Copy(sFile, bkp, true);
            var git = GetVersionInfo(Path.GetDirectoryName(sFile));
            // zapis
            using (var infile = new StreamReader(bkp, Encoding.Default, true))
            {
                var append = true;
                using (var outfile = new StreamWriter(sFile, false, infile.CurrentEncoding))
                {
                    while (!infile.EndOfStream)
                    {
                        var l = infile.ReadLine();
                        if (l != null && l.Contains("AssemblyInformationalVersion")) append = false;
                        outfile.WriteLine(DoReplace(l, git));
                    }
                    if (append) outfile.WriteLine("[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]");
                }
            }
            return true;
        }

        private static GitResult GetVersionInfo(string workDir)
        {
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
                    r.MajorVersion = Convert.ToInt32(part2[0]);
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
            r.LongHash = ExecGit(workDir, "rev-parse HEAD").Trim();
            return r;
        }

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

        private static void RestoreBackup(string inFile)
        {
            
        }

        static void Main(string[] args)
        {
            if (string.IsNullOrEmpty(FindGitBinary())) return;
            if (args.Length < 2) return;
            switch (args[0].ToLower())
            {
                case "w":
                    WriteInfo(args[1]);
                    break;
                case "r":
                    RestoreBackup(args[1]);
                    break;
                default:
                    return;
            }
        }
    }
}
