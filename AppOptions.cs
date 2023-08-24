using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace PackageRequest
{
    public class AppOptions
    {
        public bool LogIncomming { get; set; }
        public bool Loging { get; set; }
        public string LogsPath { get; set; }
        public int MaxRollingFiles { get; set; }
        public long FileSizeLimitBytes { get; set; }
        public LogLevel MinLevel { get; set; }
        public int SleepNbch { get; set; }
        public int SleepExperian { get; set; }
        public int SleepEquifax { get; set; }
        public string FtpDirectoryIn { get; set; }
        public string FtpDirectoryOut { get; set; }
        public string RKK_NbchResponcePath { get; set; }
        public int? NbchRetryCount { get; set; }
        public int? EquifaxScoringRetryCount { get; set; }
        public string RKK_NbchTakenResponcePath { get; set; }
        public string RKK_NbchUsedResponcePath { get; set; }
        public string RKK_EiResponcePath { get; set; }
        public string RKK_EiUsedResponcePath { get; set; }
        public string RKK_EquifaxResponcePath { get; set; }
        public string RKK_EquifaxUsedResponcePath { get; set; }
        public string ScoringEiResponcePath { get; set; }
        public string ScoringEquifaxResponcePath { get; set; }
        public string ScoringEquifaxRTakenResponcePath { get; set; }
        public string ScoringEquifaxRUsedResponcePath { get; set; }
        public bool EquifaxEnabled { get; set; }
    }
}