using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using System.Threading;

#nullable enable
namespace PackageRequest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EquifaxController : ControllerBase
    {
        private readonly AppOptions _options;
        private readonly ILogger<EquifaxController>? _logger;
        public EquifaxController(IOptions<AppOptions> options, ILogger<EquifaxController> logger)
        {
            _options = options.Value;

            if (_options.Loging)
            {
                _logger = logger;
            }
        }

        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        [HttpGet]
        public ActionResult EquifaxGet()
        {
            string path = _options.LogsPath;
            var @event = new EventId(new Random().Next(), nameof(EquifaxController));

            while (true)
            {
                string[] filesOnFtpInbox, filesOnResponsDir;
                try
                {
                    filesOnFtpInbox = Directory.GetFiles(_options.FtpDirectoryIn);
                    filesOnResponsDir = Directory.GetFiles(_options.RKK_EquifaxResponcePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(@event, ex, "Files not found");
                    throw ex;
                }

                string cleanFilename = "";
                string filenameResponse = "";
                foreach (string filename in filesOnFtpInbox)
                {
                    foreach (string filenameResponce in filesOnResponsDir)
                    {
                        // Проверка имён файлов
                        if (!filename.Contains("DYH"))
                        {
                            _logger?.LogDebug(@event, $"Not equifax file {filename} found in ftp {_options.FtpDirectoryIn}");
                        }
                        else if (!filenameResponce.Contains("DYH"))
                        {
                            _logger?.LogDebug(@event, $"Not equifax file {filenameResponce} found in {_options.RKK_EquifaxResponcePath}");
                        }
                        else
                        {
                            cleanFilename = filename.Substring(filename.IndexOf("DYH"), filename.IndexOf(".zip") - filename.IndexOf("DYH"));
                            filenameResponse = filenameResponce.Substring(filenameResponce.IndexOf("DYH"), filenameResponce.IndexOf(".zip") - filenameResponce.IndexOf("DYH"));

                            try
                            {
                                if (filename.Length > 0 && cleanFilename == filenameResponse.Substring(0, filenameResponse.IndexOf("_out")))
                                {
                                    System.IO.File.Copy(filenameResponce, _options.FtpDirectoryOut + filenameResponse + ".zip.sig.enc", true);
                                    _logger?.LogInformation(@event, $"File {filenameResponse} is moved to ftp folder {_options.FtpDirectoryOut}");
                                    System.IO.File.Delete(filename);
                                }
                                else
                                {
                                    _logger.LogWarning(@event, $"Bad file naming {filename} {cleanFilename} {filenameResponse}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(@event, ex, $"File {filenameResponse} not found");
                            }
                        }
                    }

                    Thread.Sleep(_options.SleepEquifax);
                }
            }
        }
    }
}