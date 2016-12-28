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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;

namespace GitVersioner
{
    internal static class Utilities
    {
        /// <summary>
        ///     Prints version info to console.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission to perform this operation.</exception>
        public static void PrintInfo()
        {
            Program.PrintMessages = false;
            var gr = GitHandler.GetVersionInfo(Directory.GetCurrentDirectory());
            SetEnvironmentVariables(gr);
            Console.WriteLine(GitHandler.GitResultToString(gr));
        }

        /// <summary>
        ///     Sets the environment variables.
        /// </summary>
        /// <param name="gitResult">The git result.</param>
        /// <exception cref="SecurityException">The caller does not have the required permission to perform this operation.</exception>
        public static void SetEnvironmentVariables(GitResult gitResult)
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
        ///     Does the replace.
        /// </summary>
        /// <param name="inString">The input string.</param>
        /// <param name="gr">The GitResult.</param>
        /// <returns></returns>
        public static string DoReplace(string inString, GitResult gr)
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
        ///     Prints a message when Git is not found
        /// </summary>
        public static void NoGit()
        {
            Console.WriteLine("Unable to find Git binary!");
        }

        /// <summary>
        ///     Shows the help.
        /// </summary>
        public static void ShowHelp()
        {
            var exename = Path.GetFileName(Assembly.GetExecutingAssembly().ManifestModule.ToString());
            Console.WriteLine();
            Console.WriteLine("Usage: {0} [parameter] --file=[file]", exename);
            Console.WriteLine("Supported parameters:");
            Console.WriteLine("write (short: w): * write version information to file and do a backup");
            Console.WriteLine("restore (short: r): * restore file from backup");
            // TODO: write something intelligent here :-)
            Console.WriteLine("auto (short: a): * Auto-Rewrite");
            Console.WriteLine("print (short: p): just prints version info");
            Console.WriteLine("build-appveyor (short: ba): ** Send version info to Appveyor.exe");
            Console.WriteLine();
            Console.WriteLine("* = file (or f) parameter is expected");
            Console.WriteLine();
            Console.WriteLine("for example {0} w Properties\\AssemblyInfo.cs", exename);
            Console.WriteLine("or {0} r Properties\\AssemblyInfo.cs", exename);
            Console.WriteLine();
            Console.WriteLine("** = version (or v) parameter is optional");
            Console.WriteLine("for example {0} --build-appveyor --version=$MajorVersion$.$MinorVersion$", exename);
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