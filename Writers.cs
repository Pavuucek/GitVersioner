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
using System.Text.RegularExpressions;
using System.Xml;

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

                var contents = File.ReadAllLines(bkp, Program.UseEncoding);
                Console.WriteLine("Reading {0}...", sFile);
                Console.WriteLine("Replacing...");
                var output = new List<string>();
                foreach (var line in contents)
                {
                    if (line != null && line.Contains("AssemblyInformationalVersion")) append = false;
                    output.Add(Utilities.DoReplace(line, git));
                }

                if (append)
                {
                    Console.WriteLine("Appending AssemblyInformationalVersion...");
                    output.Add(
                        Utilities.DoReplace(
                            "[assembly: AssemblyInformationalVersion(\"$Branch$:$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$\")]",
                            git));
                }

                File.WriteAllLines(sFile, output.ToArray(), Program.UseEncoding);
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
            Console.WriteLine("Restoring {0}...", sFile);
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

        /// <summary>Automatically searches and replaces git info</summary>
        /// <param name="fileName">file name</param>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     path specified a file that is read-only.-or- This operation is not
        ///     supported on the current platform.-or- path specified a directory.-or- The caller does not have the required
        ///     permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">The file specified in path was not found. </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        public static void AutoSearchAndReplaceAssemblyInfo(string fileName)
        {
            var fName = fileName;
            if (string.IsNullOrEmpty(fName))
                fName = Directory.GetCurrentDirectory() + "\\Properties\\AssemblyInfo.cs";
            if (!File.Exists(fName))
            {
                Console.WriteLine("Unable to find file {0}", fName);
                return;
            }

            var gr = GitHandler.GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(fName)));
            var contents = File.ReadAllText(fName, Program.UseEncoding);
            var assemblyVersion =
                $"{gr.MajorVersion.TryToInt32()}.{gr.MinorVersion.TryToInt32()}.{gr.Revision.TryToInt32()}.{gr.Commit.TryToInt32()}";
            var assemblyInfoVersion =
                $"{gr.Branch}:{gr.MajorVersion}.{gr.MinorVersion}.{gr.Revision}-{gr.Commit}-{gr.ShortHash}";
            var assemblyFileVersion =
                $"{gr.MajorVersion.TryToInt32()}.{gr.MinorVersion.TryToInt32()}.{gr.Revision.TryToInt32()}.{gr.Commit.TryToInt32()}";
            contents = Regex.Replace(contents, @"AssemblyVersion\(""[^""]*""\)",
                $"AssemblyVersion(\"{assemblyVersion}\")");
            contents = Regex.Replace(contents, @"AssemblyInformationalVersion\(""[^""]*""\)",
                $"AssemblyInformationalVersion(\"{assemblyInfoVersion}\")");
            contents = Regex.Replace(contents, @"AssemblyFileVersion\(""[^""]*""\)",
                $"AssemblyFileVersion(\"{assemblyFileVersion}\")");
            try
            {
                File.WriteAllText(fName, contents, Program.UseEncoding);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to write to file: {0}", fName);
                Console.WriteLine("Error: '{0}' in '{1}'", e.Message, e.Source);
            }

            Notifiers.NotifyTeamCity();
        }

        public static void AutoSearchAndReplaceProjects()
        {
            var projects = Utilities.SearchForFiles("*.csproj");
            projects.AddRange(Utilities.SearchForFiles("*.vbproj"));
            if (projects.Count == 0) return;
            foreach (var fName in projects)
            {
                Console.WriteLine("Processing " + fName);
                var gr = GitHandler.GetVersionInfo(Path.GetDirectoryName(Path.GetFullPath(fName)));
                var doc = new XmlDocument();
                doc.Load(fName);
                // validate document
                var valid = doc.SelectSingleNode("/Project/PropertyGroup/TargetFramework");
                if (valid != null && !valid.InnerText.Contains("netcore")) return;
                var nodes = new List<string>
                {
                    "Version",
                    "AssemblyVersion",
                    "FileVersion"
                };

                var docnodes = doc.SelectNodes("/Project/PropertyGroup");
                foreach (XmlNode docnode in docnodes)
                foreach (var node in nodes)
                {
                    var node1 = docnode.SelectSingleNode(node);
                    if (node1 == null)
                    {
                        var n1 = docnode.OwnerDocument.CreateNode(XmlNodeType.Element, node, null);
                        n1.InnerText = $"{gr.MajorVersion}.{gr.MinorVersion}.{gr.Revision}.{gr.Commit}";
                        docnode.AppendChild(n1);
                    }
                    else
                    {
                        node1.InnerText = $"{gr.MajorVersion}.{gr.MinorVersion}.{gr.Revision}.{gr.Commit}";
                    }
                }

                doc.Save(fName);
            }
        }
    }
}