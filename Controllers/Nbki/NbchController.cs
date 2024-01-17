using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PackageRequest.Controllers.Nbki
{
    [ApiController]
    [Route("[controller]")]
    public class NbchController : ControllerBase
    {
        private readonly AppOptions _options;
        private readonly ILogger<NbchController> _logger;
        private EventId _event => new EventId(new Random().Next(), nameof(NbchController));

        public NbchController(IOptions<AppOptions> options, ILogger<NbchController> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        [HttpGet] //скачивает файл бки
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        [Route("{filename}")]
        public async Task<ActionResult> NbchGet([FromRoute] string filename)
        {
            var @event = _event;

            _logger.LogInformation(@event, $"CRE ask file {filename}");

            filename = filename.Replace(".reject", "");

            Response.Headers.Add("Accept-Ranges", "bytes");

            string[] files = Directory.GetFiles(_options.NbchResponcePath);
            var responseFile = files.FirstOrDefault(x => !x.Contains("taken"));

            if (files.Length == 0 || string.IsNullOrEmpty(responseFile))
            {
                _logger.LogError(@event, $"Files in response folder {_options.NbchResponcePath} not found");
                throw new FileNotFoundException($"Files in response folder {_options.NbchResponcePath} not found");
            }

            var takenFile = _options.NbchTakenResponcePath + Path.GetFileName(responseFile + $"_{@event.Id}");
            System.IO.File.Move(responseFile, takenFile);

            _logger.LogInformation(@event, $"File {responseFile} is taken {takenFile}");

            await Task.Delay(_options.SleepNbch);

            var newResponseName = Path.Join(Path.GetDirectoryName(takenFile), filename);

            for (int retry = 0; retry <= _options.NbchRetryCount;)
            {
                try
                {
                    Stream fstream = new MemoryStream();

                    System.IO.File.Move(takenFile, newResponseName);

                    using (var stream = new FileStream(newResponseName, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = 0;
                        await stream.CopyToAsync(fstream);
                    }

                    _logger.LogInformation(@event, $"File {newResponseName} reading success");

                    System.IO.File.Move(newResponseName, _options.NbchUsedResponcePath + Path.GetFileName(responseFile));
                    _logger.LogInformation(@event, $"Response {responseFile} is moved to used {_options.NbchUsedResponcePath + Path.GetFileName(responseFile)}");

                    fstream.Position = 0;
                    return File(fstream, "application/pkcs7-mime");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(@event, ex, $"File {filename} fail - iteration {retry}");
                    System.IO.File.Move(newResponseName, responseFile);
                    _logger.LogWarning(@event, ex, $"Taken file {filename} is released -> {responseFile}");
                    retry++;
                }
            }

            throw new FileNotFoundException();
        }

        [HttpPost] //кре закидывает запрос КИ
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public ActionResult NbchPost()
        {
            var @event = _event;

            _logger.LogInformation(@event, "CRE pushed request");

            return Ok();

            // записываем пришедший файл на винт
            /*using (var fileStream = new FileStream(path + fileName, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }*/
        }
    }
}