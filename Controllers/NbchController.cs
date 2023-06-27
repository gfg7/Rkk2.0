using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;

#nullable enable
namespace PackageRequest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NbchController : ControllerBase
    {
        private readonly AppOptions _options;
        private readonly ILogger<NbchController>? _logger;
        private EventId _event => new EventId(new Random().Next(), nameof(NbchController));

        public NbchController(IOptions<AppOptions> options, ILogger<NbchController> logger)
        {
            _options = options.Value;

            if (_options.Loging)
            {
                _logger = logger;
            }
        }

        [HttpGet]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        [Route("{fileName}")]
        public async Task<ActionResult> NbchGet([FromRoute] string fileName)
        {
            string[] files = Directory.GetFiles(_options.RKK_NbchResponcePath);
            var @event = _event;

            if (files.Length == 0)
            {
                _logger?.LogError(@event, $"Files in response folder {_options.RKK_NbchResponcePath} not found");
                throw new FileNotFoundException();
            }

            await Task.Delay(_options.SleepNbch);

            Response.Headers.Add("Accept-Ranges", "bytes");

            try
            {
                Stream fstream = new MemoryStream();

                var newResponseName = Path.Join(Path.GetDirectoryName(files[0]), fileName);
                System.IO.File.Move(files[0], newResponseName);

                using (var stream = new FileStream(newResponseName, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(fstream);
                }

                System.IO.File.Delete(newResponseName);

                _logger?.LogInformation(@event, $"File {newResponseName} reading success");

                fstream.Position = 0;
                return File(fstream, "application/pkcs7-mime");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(@event, ex, $"File {fileName} reading fail");
                throw new FileNotFoundException();
            }
        }

        [HttpPost]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public async Task<ActionResult> NbchPost()
        {
            var @event = _event;

            _logger?.LogInformation(@event, "CRE pushed request");

            return Ok();

            // записываем пришедший файл на винт
            /*using (var fileStream = new FileStream(path + fileName, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }*/
        }
    }
}