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

namespace GitVersioner
{
    /// <summary>
    ///     GitResult structure
    /// </summary>
    internal struct GitResult
    {
        public string Branch;
        public string Commit;
        public string TotalCommits;
        public string CommitsInCurrentBranch;
        public string LongHash;
        public string MajorVersion;
        public string MinorVersion;
        public string Revision;
        public string ShortHash;
    }
}