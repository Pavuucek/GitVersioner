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
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace GitVersioner
{
    internal static class Writers
    {
        /// <summary>
        ///     Writes the information.
        /// </summary>
        /// <param name="sFile">The input file.</param>
        /// <param name="backup">Backup input file first.</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        public static void WriteInfo(string sFile, bool backup = true, bool append = false)
        {
            if (!File.Exists(sFile))
            {
                Console.WriteLine("Unable to find file {0}", sFile);
                return;
            }
            var bkp = sFile + ".gwbackup";
            //if (File.Exists(bkp)) return false
            try
            {
                // zapis
                if (backup) File.Copy(sFile, bkp, true);
                var git = GitHandler.GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(sFile)));
                Utilities.SetEnvironmentVariables(git);

                var contents = File.ReadAllLines(bkp, Encoding.UTF8);
                if (Program.PrintMessages) Console.WriteLine("Reading {0}...", sFile);
                if (Program.PrintMessages) Console.WriteLine("Replacing...");
                var output = new List<string>();
                foreach (var line in contents)
                {
                    if (line != null && line.Contains("AssemblyInformationalVersion")) append = false;
                    output.Add(Utilities.DoReplace(line, git));
                }
                if (append)
                {
                    if (Program.PrintMessages) Console.WriteLine("Appending AssemblyInformationalVersion...");
                    output.Add(Utilities.DoReplace(
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
        ///     Restores the backup.
        /// </summary>
        /// <param name="sFile">The input file (without gwbackup extension).</param>
        public static void RestoreBackup(string sFile)
        {
            if (Program.PrintMessages) Console.WriteLine("Restoring {0}...", sFile);
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

        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     path specified a file that is read-only.-or- This operation is not
        ///     supported on the current platform.-or- path specified a directory.-or- The caller does not have the required
        ///     permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">The file specified in path was not found. </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        public static void AutoSearchAndReplace(string fileName)
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
            try
            {
                File.WriteAllText(fileName, contents, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to write to file: {0}", fileName);
                Console.WriteLine("Error: '{0}' in '{1}'", e.Message, e.Source);
            }
            Notifiers.NotifyTeamCity();
        }
    }
}