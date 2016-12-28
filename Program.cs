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

using ArachNGIN.CommandLine;
using System;

namespace GitVersioner
{
    /// <summary>
    ///     Main class of GitVersioner
    /// </summary>
    internal static class Program
    {
        public static bool PrintMessages = true;
        public static Parameters CmdLine;

        /// <summary>
        ///     Main function
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
#if DEBUG
            PrintMessages = true;
#else
            PrintMessages=false;
#endif
            Console.WriteLine("GitVersioner");
            CmdLine = new Parameters(args);
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
            // write command: check for 'w' or 'write' and 'f' or 'file' parameter
            if (!string.IsNullOrEmpty(CmdLine["w"]) || !string.IsNullOrEmpty(CmdLine["write"]))
            {
                var f = CmdLine["f"];
                if (string.IsNullOrEmpty(f)) f = CmdLine["file"];
                if (string.IsNullOrEmpty(f))
                {
                    // 'f' or 'file' are not assigned: help and end
                    Utilities.ShowHelp();
                    return;
                }
                Writers.WriteInfo(f);
            }
            // restore command
            else if (!string.IsNullOrEmpty(CmdLine["r"]) || !string.IsNullOrEmpty(CmdLine["restore"]))
            {
                var f = CmdLine["f"];
                if (string.IsNullOrEmpty(f)) f = CmdLine["file"];
                if (string.IsNullOrEmpty(f))
                {
                    // 'f' or 'file' are not assigned: help and end
                    Utilities.ShowHelp();
                    return;
                }
                Writers.RestoreBackup(f);
            }
            // auto search mode
            else if (!string.IsNullOrEmpty(CmdLine["a"]) || !string.IsNullOrEmpty(CmdLine["auto"]))
            {
                var f = CmdLine["f"];
                if (string.IsNullOrEmpty(f)) f = CmdLine["file"];
                if (string.IsNullOrEmpty(f))
                {
                    // 'f' or 'file' are not assigned: help and end
                    Utilities.ShowHelp();
                    return;
                }
                Writers.AutoSearchAndReplace(f);
            }
            // notify appveyor
            else if (!string.IsNullOrEmpty(CmdLine["ba"]) || !string.IsNullOrEmpty(CmdLine["build-appveyor"]))
            {
                var f = CmdLine["v"];
                if (string.IsNullOrEmpty(f)) f = CmdLine["version"];
                if (string.IsNullOrEmpty(f))
                {
                    // 'v' or 'version-format' are not assigned: help and end
                    Utilities.ShowHelp();
                    return;
                }
                Notifiers.NotifyAppveyor(f);
            }
            // print mode (just print version info)
            else if (!string.IsNullOrEmpty(CmdLine["p"]) || !string.IsNullOrEmpty(CmdLine["print-info"]))
            {
                Utilities.PrintInfo();
            }
            else
            {
                Utilities.ShowHelp();
            }
            if (PrintMessages) Console.WriteLine("Finished!");
        }
    }
}