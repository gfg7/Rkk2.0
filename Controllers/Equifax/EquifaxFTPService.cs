using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PackageRequest.Controllers.Equifax
{
    public class EquifaxFTPService : IHostedService
    {
        private static AppOptions _options = null!;
        private static ILogger<EquifaxFTPService> _logger;
        private FileSystemWatcher _watcher = null!;

        public EquifaxFTPService(IOptions<AppOptions> options, ILogger<EquifaxFTPService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (Directory.GetFiles(_options.EquifaxResponcePath).Count() == 0)
            {
                _logger.LogCritical($"Response files not found in {_options.EquifaxResponcePath}");
            }

            _watcher = new FileSystemWatcher(_options.EquifaxFtpDirectoryIn)
            {
                NotifyFilter = NotifyFilters.FileName
            };

            _watcher.Created += InterceptEquifaxRequest;

            _watcher.EnableRaisingEvents = true;

            _logger.LogInformation("Equifax file system watcher is enabled");

            return Task.CompletedTask;
        }

        private static void InterceptEquifaxRequest(object sender, FileSystemEventArgs e)
        {
            var @event = new EventId(new Random().Next(), nameof(EquifaxFTPService));

            if (!e.Name.Contains("DYH"))
            {
                _logger.LogWarning(@event, $"Not equifax file {e.Name} found in ftp {_options.EquifaxFtpDirectoryIn}");
                return;
            }

            _logger.LogInformation($"Equifax request is intercepted {e.FullPath}");

            var responseFiles = Directory.GetFiles(_options.EquifaxResponcePath);

            if (responseFiles.Count() == 0)
            {
                _logger.LogCritical(@event, $"Response files not found in {_options.EquifaxResponcePath}");
                throw new FileNotFoundException($"Files not found in {_options.EquifaxResponcePath}");
            }

            var response = responseFiles.First();
            var requestFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(e.FullPath)));//убирает .zip.sgn.enc
            var newResponseName = "outbox_" + requestFileName + "_out.zip.sgn.enc";
            _logger.LogInformation(@event, $"New reponse file {newResponseName}");

            Thread.Sleep(_options.SleepEquifax);

            try
            {
                File.Delete(e.FullPath);
                _logger.LogInformation(@event, $"Delete request {e.FullPath}");

                var statReq = e.FullPath.Replace("_tmp", "");

                if (File.Exists(statReq))
                {
                    File.Delete(statReq);
                    _logger.LogInformation(@event, $"Delete request {statReq}");
                }

                File.Copy(response, _options.EquifaxFtpDirectoryOut + newResponseName);
                _logger.LogInformation(@event, $"New reponse file is moved to ftp {_options.EquifaxFtpDirectoryOut + newResponseName}");
                File.Move(response, _options.EquifaxUsedResponcePath + Path.GetFileName(response));
                _logger.LogInformation(@event, $"Response {response} is moved to used {_options.EquifaxUsedResponcePath + Path.GetFileName(response)}");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(@event, ex, "error on moving response to ftp");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _watcher.Dispose();

            _logger.LogInformation("Equifax file system watcher is disposed");

            return Task.CompletedTask;
        }
    }
}