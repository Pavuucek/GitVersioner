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

namespace GitVersioner
{
    /// <summary>
    ///     Main class of GitVersioner
    /// </summary>
    internal static class Program
    {
        public static bool PrintMessages = true;

        /// <summary>
        ///     Main function
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("GitVersioner");
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
            string param;
            switch (args[0].ToLower())
            {
                // write mode (with backup)
                case "w":
                    if (args.Length < 2)
                    {
                        Utilities.ShowHelp();
                        return;
                    }
                    Writers.WriteInfo(args[1]);
                    break;
                // restore mode (from backup)
                case "r":
                    if (args.Length < 2)
                    {
                        Utilities.ShowHelp();
                        return;
                    }
                    Writers.RestoreBackup(args[1]);
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
                    Writers.AutoSearchAndReplace(param);
                    break;
                // print mode (just print version info)
                case "p":
                    Utilities.PrintInfo();
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
                    Utilities.ShowHelp();
                    return;
            }
            if (PrintMessages) Console.WriteLine("Finished!");
        }
    }
}