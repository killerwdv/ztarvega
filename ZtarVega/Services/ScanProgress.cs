namespace ZtarVega.Services
{
    /// <summary>Describes how far a ROM scan has advanced.</summary>
    internal sealed class ScanProgress
    {
        public ScanProgress(int processedFiles, int totalFiles, string currentFileName)
        {
            ProcessedFiles = processedFiles;
            TotalFiles = totalFiles;
            CurrentFileName = currentFileName;
        }

        public int ProcessedFiles { get; }

        public int TotalFiles { get; }

        public string CurrentFileName { get; }
    }
}