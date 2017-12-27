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
using System.Text;
using ArachNGIN.CommandLine;

namespace GitVersioner
{
    /// <summary>
    ///     Main class of GitVersioner
    /// </summary>
    internal static class Program
    {
        public static Encoding UseEncoding = Encoding.UTF8;

        /// <summary>
        ///     Main function
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("GitVersioner");
            var cmdLine = new Parameters(args);
            if (string.IsNullOrEmpty(GitHandler.FindGitBinary()))
            {
                Utilities.NoGit();
                return;
            }

            if (args.Length < 1)
            {
                Utilities.ShowHelp();
                return;
            }

            UseEncoding = Encoding.UTF8;
            if (!string.IsNullOrEmpty(cmdLine["no-utf"]) || !string.IsNullOrEmpty(cmdLine["no-utf8"]))
                UseEncoding = Encoding.ASCII;
            // write command: check for 'w' or 'write' and 'f' or 'file' parameter
            if (!string.IsNullOrEmpty(cmdLine["w"]) || !string.IsNullOrEmpty(cmdLine["write"]))
            {
                var f = cmdLine["f"];
                if (string.IsNullOrEmpty(f)) f = cmdLine["file"];
                if (string.IsNullOrEmpty(f))
                {
                    // 'f' or 'file' are not assigned: help and end
                    Utilities.ShowHelp();
                    return;
                }

                Writers.WriteInfo(f);
            }
            // restore command
            else if (!string.IsNullOrEmpty(cmdLine["r"]) || !string.IsNullOrEmpty(cmdLine["restore"]))
            {
                var f = cmdLine["f"];
                if (string.IsNullOrEmpty(f)) f = cmdLine["file"];
                if (string.IsNullOrEmpty(f))
                {
                    // 'f' or 'file' are not assigned: help and end
                    Utilities.ShowHelp();
                    return;
                }

                Writers.RestoreBackup(f);
            }
            // auto search mode
            else if (!string.IsNullOrEmpty(cmdLine["a"]) || !string.IsNullOrEmpty(cmdLine["auto"]))
            {
                var f = cmdLine["f"];
                if (string.IsNullOrEmpty(f)) f = cmdLine["file"];
                Writers.AutoSearchAndReplaceAssemblyInfo(f);
            }
            // auto replace mode for dotnet projects
            else if (!string.IsNullOrEmpty(cmdLine["o"]) || !string.IsNullOrEmpty(cmdLine["project"]))
            {
                Writers.AutoSearchAndReplaceProjects();
            }
            // notify appveyor
            else if (!string.IsNullOrEmpty(cmdLine["ba"]) || !string.IsNullOrEmpty(cmdLine["build-appveyor"]))
            {
                var f = cmdLine["v"];
                if (string.IsNullOrEmpty(f)) f = cmdLine["version"];
                if (string.IsNullOrEmpty(f)) f = string.Empty;
                Notifiers.NotifyAppveyor(f);
            }
            // print mode (just print version info)
            else if (!string.IsNullOrEmpty(cmdLine["p"]) || !string.IsNullOrEmpty(cmdLine["print-info"]))
            {
                Utilities.PrintInfo();
            }
            else
            {
                Utilities.ShowHelp();
            }

            Console.WriteLine("Finished!");
        }
    }
}