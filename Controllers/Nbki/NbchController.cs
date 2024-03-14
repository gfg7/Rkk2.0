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

        [HttpDelete("reset")]
        public ActionResult Reset()
        {
            if (_options.OfflineMode)
            {
                return BadRequest($"offline is on");
            }

            string[] taken = Directory.GetFiles(_options.NbchTakenResponcePath);
            string[] used = Directory.GetFiles(_options.NbchUsedResponcePath);

            foreach (var item in taken)
            {
                System.IO.File.Delete(item);
            }

            foreach (var item in used)
            {
                var resp = _options.NbchResponcePath + Path.GetFileName(item);
                System.IO.File.Move(item, resp);
            }

            return Ok();
        }

        [HttpGet] //скачивает файл бки
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        [Route("{filename}")]
        public async Task<ActionResult> NbchGet([FromRoute] string filename)
        {
            var @event = _event;

            _logger.LogInformation(@event, $"CRE ask file {filename}");

            await Task.Delay(_options.SleepNbch);

            filename = filename.Replace(".reject", "");
            Response.Headers.Add("Accept-Ranges", "bytes");

            var takenFile = _options.NbchTakenResponcePath + Path.GetFileName(filename);

            if (!System.IO.File.Exists(Path.Combine(takenFile)))
            {
                var responseFile = Directory.GetFiles(_options.NbchResponcePath).FirstOrDefault();

                if (string.IsNullOrEmpty(responseFile))
                {
                    _logger.LogError(@event, $"Files in response folder {_options.NbchResponcePath} not found");
                    throw new FileNotFoundException($"Files in response folder {_options.NbchResponcePath} not found");
                }

                System.IO.File.Copy(responseFile, takenFile);
                _logger.LogInformation(@event, $"File {responseFile} is taken {takenFile}");

                var usedFile = Path.Combine(_options.NbchUsedResponcePath, Path.GetFileName(responseFile));
                System.IO.File.Move(responseFile, usedFile);
                _logger.LogInformation(@event, $"Response {responseFile} is moved to used {usedFile}");
            }

            for (int retry = 0; retry <= _options.NbchRetryCount;)
            {
                try
                {
                    Stream fstream = new MemoryStream();

                    using (var stream = new FileStream(takenFile, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = 0;
                        await stream.CopyToAsync(fstream);
                    }

                    _logger.LogInformation(@event, $"File {takenFile} for {filename} reading success");
                    System.IO.File.Delete(takenFile);

                    fstream.Position = 0;
                    return File(fstream, "application/pkcs7-mime", filename);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(@event, ex, $"File {filename} fail - iteration {retry}");
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

            _logger.LogInformation(@event, $"CRE pushed request {Request.Form.Files.FirstOrDefault()?.FileName} {Request.Form.Files.FirstOrDefault()?.Length}");

            return Ok("неизвестный ответ от БКИ, который нам не дали и его не парсят, но он нужен :)");

            // записываем пришедший файл на винт
            /*using (var fileStream = new FileStream(path + fileName, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }*/
        }
    }
}