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
        public static bool PrintMessages = true;

        /// <summary>
        ///     Prints version info to console.
        /// </summary>
        private static void PrintInfo()
        {
            PrintMessages = false;
            var gr = GitHandler.GetVersionInfo(Directory.GetCurrentDirectory());
            SetEnvironmentVariables(gr);
            Console.WriteLine(GitHandler.GitResultToString(gr));
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
                var git = GitHandler.GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(sFile)));
                SetEnvironmentVariables(git);

                var contents = File.ReadAllLines(bkp, Encoding.UTF8);
                if (PrintMessages) Console.WriteLine("Reading {0}...", sFile);
                if (PrintMessages) Console.WriteLine("Replacing...");
                var output = new List<string>();
                foreach (var line in contents)
                {
                    if (line != null && line.Contains("AssemblyInformationalVersion")) append = false;
                    output.Add(DoReplace(line, git));
                }
                if (append)
                {
                    if (PrintMessages) Console.WriteLine("Appending AssemblyInformationalVersion...");
                    output.Add(
                        DoReplace(
                            "[assembly: AssemblyInformationalVersion(\"$Branch$:$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$\")]",
                            git));
                }
                File.WriteAllLines(sFile, output.ToArray(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: '{0}' in '{1}'", e.Message, e.Source);
            }
            Notifiers.NotifyTeamCity();
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
            if (PrintMessages) Console.WriteLine("Restoring {0}...", sFile);
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
            if (string.IsNullOrEmpty(GitHandler.FindGitBinary()))
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
                    Notifiers.NotifyAppveyor(param);
                    break;

                default:
                    ShowHelp();
                    return;
            }
            if (PrintMessages) Console.WriteLine("Finished!");
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
            var gr = GitHandler.GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(fileName)));
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
            Notifiers.NotifyTeamCity();
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
    }
}