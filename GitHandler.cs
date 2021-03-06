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
using System.Diagnostics;
using System.IO;
using System.Text;

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
        private static bool Is64Bit => IntPtr.Size == 8 ||
                                       !string.IsNullOrEmpty(
                                           Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));

        /// <summary>
        ///     Gets Program Files directory
        /// </summary>
        /// <returns>Program Files or Program Files (x86) directory</returns>
        private static string ProgramFilesX86()
        {
            var result = Environment.GetEnvironmentVariable(Is64Bit ? "ProgramFiles(x86)" : "ProgramFiles");

            if (string.IsNullOrEmpty(result)) result = @"C:\Program Files\";

            return result;
        }

        /// <summary>
        ///     Finds the git binary.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> that contains the specified value has
        ///     been marked for deletion.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The user does not have the necessary registry rights.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        public static string FindGitBinary()
        {
            string git = null;

            // Try the PATH environment variable

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    var sdir = dir;
                    if (sdir.StartsWith("\"") && sdir.EndsWith("\""))
                        sdir = sdir.Substring(1, sdir.Length - 2);
                    git = Path.Combine(sdir, GitExeName);
                    if (File.Exists(git)) break;
                }

            if (!File.Exists(git)) git = null;


            // Search program files directory
            if (git == null)
                foreach (
                    var dir in
                    Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "git*"))
                {
                    git = Path.Combine(dir, Path.Combine("bin", GitExeName));
                    if (!File.Exists(git)) git = null;
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
            Console.WriteLine("Getting version info for {0}", workDir);
            var lines = ExecGit(workDir, "describe --long --tags --always");
            GitResult r;
            r.MajorVersion = "0";
            r.MinorVersion = "0";
            r.Revision = "0";
            r.Commit = "0";
            r.TotalCommits = "0";
            r.CommitsInCurrentBranch = "0";
            r.ShortHash = "";
            // ocekavany retezec ve formatu: 1.7.6-235-g0a52e4b
            var part1 = lines.Split('-');
            if (part1.Length >= 3)
            {
                // druhou cast rozdelit po teckach
                var part2 = part1[0].Split('.');
                if (part2.Length > 1)
                {
                    // delsi nez 1: mame major a minor verzi
                    if (part2.Length > 2) r.Revision = part2[2];
                    r.MinorVersion = part2[1];
                }
                // mame jen major verzi

                var s = part2[0].ToLowerInvariant();
                // kdyby nahodou nekdo chtel pojmenovavat git tagy v1.0.0 atd (tj zacinajci ne cislem ale v)
                if (s[0] == 'v') s = s.Remove(0, 1);
                r.MajorVersion = s;
            }

            // if commit parsing fails default to zero, we'll count commits later
            r.Commit = part1.Length < 2 ? "0" : part1[1].Trim();

            // just shorthash is remaining. it's either part 2 of part1 or everything
            r.ShortHash = part1.Length > 2 ? part1[2].Trim() : lines.Trim();

            // get total commits
            r.TotalCommits = ExecGit(workDir, "rev-list --count --all").Trim();
            //
            // if no tags are present we'll get 0.0.0-0-abcdefg
            // give total commit count at least
            if (r.MajorVersion.TryToInt32() == 0 && r.MinorVersion.TryToInt32() == 0 && r.Revision.TryToInt32() == 0 &&
                r.Commit.TryToInt32() == 0)
                r.Commit = r.TotalCommits;

            r.Branch = ExecGit(workDir, "rev-parse --abbrev-ref HEAD").Trim();
            // we don't want branches to be called HEAD...
            if (r.Branch == "HEAD")
                r.Branch = ExecGit(workDir, "describe --all").Trim();
            // get commits in current branch before cleaning branch name
            r.CommitsInCurrentBranch = ExecGit(workDir, $"rev-list --count {r.Branch}").Trim();
            r.Branch = CleanBranchName(r.Branch);
            r.LongHash = ExecGit(workDir, "rev-parse HEAD").Trim();
            Console.WriteLine("Version info: {0}", GitResultToString(r));
            if (string.IsNullOrEmpty(lines))
                Console.WriteLine("Possible error, git output follows:\n {0}", lines);
            return r;
        }

        /// <summary>
        ///     Cleans the name of the branch.
        /// </summary>
        /// <param name="branch">The branch.</param>
        /// <returns></returns>
        public static string CleanBranchName(string branch)
        {
            var s = branch;
            s = s.Replace("refs", string.Empty);
            s = s.Replace("remotes", string.Empty);
            s = s.Replace("remote", string.Empty);
            s = s.Replace("origin", string.Empty);
            s = s.Replace("heads", string.Empty);
            s = s.Replace("heads/", string.Empty);
            // get rid of all slashes
            while (s.Contains("//"))
                s = s.Replace("//", "/");
            s = s.Replace("/", "-");
            s = s.TrimStart('-');
            return s;
        }

        /// <summary>
        ///     Converts git results to string
        /// </summary>
        /// <param name="gr">GitResult.</param>
        /// <returns></returns>
        public static string GitResultToString(GitResult gr)
        {
            return $"{gr.MajorVersion}.{gr.MinorVersion}.{gr.Revision}-{gr.Commit}-{gr.ShortHash}";
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
            var r = new StringBuilder();
            while (p != null && !p.StandardOutput.EndOfStream)
                r.AppendLine(p.StandardOutput.ReadLine());
            if (p != null && !p.WaitForExit(1000))
                p.Kill();
            return r.ToString();
        }
    }
}