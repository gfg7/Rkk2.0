using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PackageRequest.Controllers.Ei
{
    [Route("[controller]/old")]
    public class ExperianController : Controller
    {
        private readonly AppOptions _options;
        private readonly ILogger<ExperianController> _logger;
        public ExperianController(ILogger<ExperianController> logger, IOptions<AppOptions> options)
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

            string[] taken = Directory.GetFiles(_options.EiTakenResponcePath);
            string[] used = Directory.GetFiles(_options.EiUsedResponcePath);

            foreach (var item in taken)
            {
                System.IO.File.Delete(item);
            }

            foreach (var item in used)
            {
                var resp = _options.EiResponcePath + Path.GetFileName(item);
                System.IO.File.Move(item, resp);
            }

            return Ok();
        }

        [HttpPost]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public async Task<ActionResult> OkbList([FromForm(Name = "ActionFlag")] int actionFlag, [FromForm(Name = "FileBody")] IFormFile upload)
        {
            var @event = new EventId(new Random().Next(), nameof(ExperianController));
            DateTime date = DateTime.Now;

            Thread.Sleep(_options.SleepExperian);

            Request.Form.TryGetValue("FileName", out var filename);
            string resp = "";
            Stream fstream = Stream.Null;

            _logger.LogInformation(@event, $"Request ActionFlag {actionFlag} FileBody {upload?.Length} bytes FileName {filename}");

            if (actionFlag == 7) //загрузка файла в бки
            {
                var uploadName = upload.FileName;
                var takenFile = Path.Combine(_options.EiTakenResponcePath, uploadName!.Replace(uploadName[..uploadName.IndexOf("_")], "RESP"));

                if (!System.IO.File.Exists(Path.Combine(takenFile)) && !_options.OfflineMode)
                {
                    var responseFile = Directory.GetFiles(_options.EiResponcePath).FirstOrDefault();
                    System.IO.File.Copy(responseFile, takenFile);

                    _logger.LogInformation(@event, $"File {responseFile} is taken {takenFile} - response for request {upload} is created");

                    var usedFile = Path.Combine(_options.EiUsedResponcePath, Path.GetFileName(responseFile));
                    System.IO.File.Move(responseFile, usedFile);

                    _logger.LogInformation(@event, $"Original {responseFile} is moved to used {usedFile}");
                }

                resp = "<s>\n" +
                    "<s n=\"Data\">\n" +
                    "<a n=\"ActionFlag\">7</a>\n" +
                    "<c n=\"History\">\n" +
                    "<s>\n" +
                    "<s n=\"warnings\">\n" +
                    "<a n=\"present\">0</a>\n" +
                    "</s>\n" +
                    "<c n=\"wlRecords\">\n" +
                    "</c>\n" +
                    "<a n=\"wlReturnCount\">0</a>\n" +
                    "</s>\n" +
                    "</c>\n" +
                    "<a n=\"StreamID\">30564169</a>\n" +
                    "<a n=\"ValidationErrors\" />\n" +
                    "<a n=\"errorCode\">0</a>\n" +
                    "<a n=\"responseDate\">" + date.ToString("yyyyMMddhhmmss") + "</a>\n" +
                    "<a n=\"supportMail\" />\n" +
                    "</s>\n" +
                    "</s>\n";
            }


            if (actionFlag == 9)//запрос списка доступных на скачивание
            {
                string[] firstRequested = Directory.GetFiles(_options.EiTakenResponcePath); //Берем список файлов для ответа отсюда

                _logger.LogInformation(@event, $"CRE asks for requested list");
                string strXml = string.Join('\n', firstRequested.Where(x => x.Contains("RESP")).Select(x => $"<s><a n = \"Name\">{Path.GetFileName(x)}</a></s>"));

                resp = "<s>\n" +
                                  "<s n=\"Data\">\n" +
                                      "<a n=\"ActionFlag\">9</a>\n" +
                                      "<c n=\"Files\">\n" +
                                      "<s>\n" +
                                      "<c n= \"wlListOfFiles\">\n" +
                                      strXml +
                                      "</c>\n" +
                                      "<a n=\"wlReturnCount\">" + firstRequested.Count() + "</a>\n" +
                                      "</s>\n" +
                                      "</c>\n" +
                                      "<a n=\"StreamID\">30564169</a>\n" +
                                      "<a n=\"ValidationErrors\"/>\n" +
                                      "<a n=\"errorCode\">0</a>\n" +
                                      "<a n=\"responseDate\">" + date.ToString("yyyyMMddhhmmss") + "</a>\n" +
                                  "</s>\n" +
                              "</s>\n";
            }


            if (actionFlag == 1)//скачивание файла
            {
                for (int retry = 0; retry <= _options.EiRetryCount;)
                {
                    try
                    {
                        var takenFile = Path.Combine(_options.EiTakenResponcePath, filename!);
                        _logger.LogDebug(@event, $"searching for {takenFile} in response folder");

                        if (!System.IO.File.Exists(takenFile))
                        {
                            _logger.LogError(@event, $"{takenFile} not found");
                            throw new FileNotFoundException($"Requested file {takenFile} not found");
                        }

                        using (var stream = new FileStream(takenFile, FileMode.Open, FileAccess.Read))
                        {
                            stream.Position = 0;
                            await stream.CopyToAsync(fstream);
                        }

                        _logger.LogInformation(@event, $"File {takenFile} reading success");
                        System.IO.File.Delete(takenFile);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(@event, ex, $"File {filename} fail - iteration {retry}");
                        retry++;
                    }
                }
            }

            if (string.IsNullOrEmpty(resp))//необработанные экшены/ошибка
            {
                resp = "<s>\n" +
                    "<c n=\"ValidationErrors\">\n" +
                    "<s>\n" +
                    "<a n=\"number\">503 Service Unavailable</a>\n" +
                    "</s>\n" +
                    "</c>\n" +
                    "<a n=\"errorCode\">1</a>\n" +
                    "<a n=\"responseDate\">" + date.ToString("yyyyMMddhhmmss") + "</a>\n" +
                    "<a n=\"streamID\">30564169</a>\n" +
                    "</s>\n";
            }

            fstream = new MemoryStream(Encoding.UTF8.GetBytes(resp))
            {
                Position = 0
            };
            return File(fstream, "application/xml");
        }
    }
}