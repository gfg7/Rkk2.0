using System.Net.Mime;
using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NReco.Logging.File;

namespace PackageRequest
{
    public static class LoggerConfiguration
    {
        private static readonly Func<LogMessage, string> _format = x => $"{DateTime.UtcNow} {x.EventId.Name} {x.LogLevel} {x.EventId.Id} : {x.Message}\n{x.Exception?.Message}";
        private static readonly Func<string, string> _name = x => string.Format(x, DateTime.UtcNow);
        public static ILoggingBuilder BuildLogger(this ILoggingBuilder builder, string logsFolder, string folder,
        string logName, string eventName, int maxFileCount, long sizeLimit, LogLevel minLevel)
        {
            var logDirectory = Path.Combine(logsFolder, folder);
            var logFile = Path.Combine(logDirectory, logName + "_{0:dd}-{0:MM}-{0:yyyy}.log");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            return builder.AddFile(logFile, fileLoggerOpts =>
            {
                fileLoggerOpts.Append = true;
                fileLoggerOpts.FormatLogEntry = _format;
                fileLoggerOpts.FormatLogFileName = _name;
                fileLoggerOpts.MaxRollingFiles = maxFileCount;
                fileLoggerOpts.FileSizeLimitBytes = sizeLimit;
                fileLoggerOpts.MinLevel = minLevel;

                fileLoggerOpts.HandleFileError = (err) => {
                    err.UseNewLogFileName(Path.GetFileNameWithoutExtension(err.LogFileName) + "_alt" + Path.GetExtension(err.LogFileName));
                };

                fileLoggerOpts.FilterLogEntry = (msg) =>
                {
                    return msg.EventId.Name == eventName;
                };
            });
        }
    }
}