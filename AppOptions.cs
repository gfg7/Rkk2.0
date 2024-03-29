using Microsoft.Extensions.Logging;

namespace PackageRequest
{
    public class AppOptions
    {
        public bool OfflineMode {get; set;}
        public bool LogIncomming { get; set; }
        public bool FileLogging { get; set; }
        public bool LogBki { get; set; }
        public string LogsPath { get; set; }
        public int MaxRollingFiles { get; set; }
        public long FileSizeLimitBytes { get; set; }
        public int MaxFileBuffer { get; set; } = 2048;
        public LogLevel MinLevel { get; set; }
        public int SleepNbch { get; set; }
        public int SleepExperian { get; set; }
        public int SleepEquifax { get; set; }
        public int NbchRetryCount { get; set; } = 1;
        public string NbchResponcePath { get; set; }
        public string NbchTakenResponcePath { get; set; }
        public string NbchUsedResponcePath { get; set; }
        public int EiRetryCount { get; set; } = 1;
        public string EiResponcePath { get; set; }
        public string EiUsedResponcePath { get; set; }
        public string EiTakenResponcePath { get; set; }
        public int EquifaxRetryCount { get; set; } = 1;
        public string EquifaxResponcePath { get; set; }
        public string EquifaxUsedResponcePath { get; set; }
        public string EquifaxTakenResponcePath { get; set; }
        public bool FTPEquifaxEnabled { get; set; }
        public string EquifaxFtpDirectoryIn { get; set; }
        public string EquifaxFtpDirectoryOut { get; set; }
    }
}