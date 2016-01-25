namespace GitVersioner
{
    /// <summary>
    ///     GitResult structure
    /// </summary>
    internal struct GitResult
    {
        public string Branch;
        public int Commit;
        public string LongHash;
        public int MajorVersion;
        public int MinorVersion;
        public int Revision;
        public string ShortHash;
    }
}