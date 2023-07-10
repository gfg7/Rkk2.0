using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable
namespace PackageRequest.Controllers
{
    public class EquifaxController : IHostedService
    {
        private static AppOptions _options;
        private static ILogger<EquifaxController>? _logger;
        private FileSystemWatcher _watcher;

        public EquifaxController(IOptions<AppOptions> options, ILogger<EquifaxController> logger)
        {
            _options = options.Value;

            if (_options.Loging)
            {
                _logger = logger;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (Directory.GetFiles(_options.RKK_EquifaxResponcePath).Count() == 0)
            {
                _logger?.LogCritical($"Response files not found in {_options.RKK_EquifaxResponcePath}");

                //return Task.FromException(new Exception($"Files not found in {_options.RKK_EquifaxResponcePath}"));
            }

            _watcher = new FileSystemWatcher(_options.FtpDirectoryIn)
            {
                NotifyFilter = NotifyFilters.FileName
            };

            _watcher.Created += InterceptEquifaxRequest;

            _watcher.EnableRaisingEvents = true;

            _logger?.LogInformation("Equifax file system watcher is enabled");

            return Task.CompletedTask;
        }

        private static void InterceptEquifaxRequest(object sender, FileSystemEventArgs e)
        {
            var @event = new EventId(new Random().Next(), nameof(EquifaxController));

            if (!e.Name.Contains("DYH"))
            {
                _logger?.LogWarning(@event, $"Not equifax file {e.Name} found in ftp {_options.FtpDirectoryIn}");
                return;
            }

            _logger?.LogInformation($"Equifax request is intercepted {e.FullPath}");

            var responseFiles = Directory.GetFiles(_options.RKK_EquifaxResponcePath);

            if (responseFiles.Count() == 0)
            {
                _logger?.LogCritical(@event, $"Response files not found in {_options.RKK_EquifaxResponcePath}");
                throw new Exception($"Files not found in {_options.RKK_EquifaxResponcePath}");
            }

            var response = responseFiles.First();
            var requestFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(e.FullPath)));//убирает .zip.sgn.enc
            var newResponseName = "outbox_" + requestFileName + "_out.zip.sgn.enc";
            _logger?.LogInformation(@event, $"New reponse file {newResponseName}");

            Thread.Sleep(_options.SleepEquifax);

            try
            {
                File.Delete(e.FullPath);
                _logger?.LogInformation(@event, $"Delete request {e.FullPath}");

                var statReq = e.FullPath.Replace("_tmp", "");

                if (File.Exists(statReq))
                {
                    File.Delete(statReq);
                    _logger?.LogInformation(@event, $"Delete request {statReq}");
                }

                File.Copy(response, _options.FtpDirectoryOut + newResponseName);
                _logger?.LogInformation(@event, $"New reponse file is moved to ftp {_options.FtpDirectoryOut + newResponseName}");
                File.Move(response, _options.RKK_EquifaxUsedResponcePath + Path.GetFileName(response));
                _logger?.LogInformation(@event, $"Response {response} is moved to used {_options.RKK_EquifaxUsedResponcePath + Path.GetFileName(response)}");
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(@event, ex, "error on moving response to ftp");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _watcher.Dispose();

            _logger?.LogInformation("Equifax file system watcher is disposed");

            return Task.CompletedTask;
        }
    }
}