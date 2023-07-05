using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

#nullable enable
namespace PackageRequest.Controllers
{
    [Route("[controller]")]
    public class ExperianController : Controller
    {
        private readonly AppOptions _options;
        private readonly ILogger<ExperianController>? _logger;
        private readonly ExperianRequestFileStore _requestStore;
        public ExperianController(ILogger<ExperianController> logger, IOptions<AppOptions> options, ExperianRequestFileStore requestStore)
        {
            _options = options.Value;
            _requestStore = requestStore;

            if (_options.Loging)
            {
                _logger = logger;
            }
        }

        [HttpGet("/reset")]
        public ActionResult Reset()
        {
            _requestStore.FlushStore();
            _logger.LogInformation(new EventId(new Random().Next(), nameof(ExperianController)), "experian request store is reseted");
            return Ok();
        }

        [HttpPost]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public async Task<ActionResult> OkbList()
        {
            var @event = new EventId(new Random().Next(), nameof(ExperianController));
            DateTime date = DateTime.Now;

            if (Request.Body == null)
            {
                _logger?.LogWarning(@event, $"CRE request is empty");
                return Problem(title: @event.Id.ToString(), detail: _options.LogsPath + " Experian.log", statusCode: 500);
            }

            var streamReader = new StreamReader(Request.Body);
            string xmlData = await streamReader.ReadToEndAsync();

            // _logger?.LogInformation(@event, "CRE request: " + xmlData);

            string trimText = xmlData.Substring(xmlData.LastIndexOf("Content-Disposition: form-data; name=\"ActionFlag\"") + 51);
            string actionFlagText = trimText.Substring(0, trimText.IndexOf("--"));
            int actionFlag = int.Parse(actionFlagText.Trim());
            string resp = "";

            _logger?.LogInformation(@event, $"Request action flag {actionFlag}");

            if (actionFlag == 7)
            {
                var k = xmlData.IndexOf("filename=") + 10;
                var j = xmlData.IndexOf(".pem") + 8;


                string requestFilename = xmlData.Substring(k, j - k);

                _requestStore.AddNewRequest(requestFilename);

                _logger?.LogInformation(@event, $"Requested file {requestFilename} is added to queue");

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
            else if (actionFlag == 9)
            {
                var firstRequested = _requestStore.PeekRequest().Replace("CHD", "RESP");

                if (string.IsNullOrEmpty(firstRequested))
                {
                    resp = UnavailableResponse(date);
                    _logger?.LogDebug(@event, "request store is empty");
                }
                else
                {
                    _logger?.LogInformation(@event, $"Requested file {firstRequested} is used");

                    string strXml = $"<s><a n = \"Name\">{firstRequested}</a></s>";

                    // nameFiles.ToList().ForEach(x => strXml += str.Replace("???", x) + '\n');

                    resp = "<s>\n" +
                                      "<s n=\"Data\">\n" +
                                          "<a n=\"ActionFlag\">9</a>\n" +
                                          "<c n=\"Files\">\n" +
                                          "<s>\n" +
                                          "<c n= \"wlListOfFiles\">\n" +
                                          strXml +
                                          "</c>\n" +
                                          "<a n=\"wlReturnCount\">" + 1 + "</a>\n" +
                                          "</s>\n" +
                                          "</c>\n" +
                                          "<a n=\"StreamID\">30564169</a>\n" +
                                          "<a n=\"ValidationErrors\"/>\n" +
                                          "<a n=\"errorCode\">0</a>\n" +
                                          "<a n=\"responseDate\">" + date.ToString("yyyyMMddhhmmss") + "</a>\n" +
                                      "</s>\n" +
                                  "</s>\n";
                }
            }
            else if (actionFlag == 1)
            {

                Thread.Sleep(_options.SleepExperian);

                var firstRequested = _requestStore.ProcessRequest().Replace("CHD", "RESP");

                if (string.IsNullOrEmpty(firstRequested))
                {
                    _logger?.LogError(@event, "ei request store is empty");
                    throw new ArgumentNullException("ei request store is empty");
                }

                _logger?.LogInformation(@event, $"Requested file {firstRequested} is removed from queue");

                string[] files = Directory.GetFiles(_options.RKK_EiResponcePath); //Берем список файлов для ответа отсюда

                var responseFile = files.FirstOrDefault(x => Path.GetFileName(x) == firstRequested);

                _logger?.LogDebug(@event, $"searching for {firstRequested} in response folder");

                if (responseFile is null)
                {
                    responseFile = _options.RKK_EiResponcePath + firstRequested;
                    System.IO.File.Move(files[0], responseFile);
                    _logger?.LogDebug(@event, "no prepared file, renaming stub file");
                }

                // Stream fstream = new MemoryStream();

                // using (var stream = new FileStream(_options.RKK_EiResponcePath + firstRequested, FileMode.Open, FileAccess.Read))
                // {
                //     stream.Position = 0;
                //     await stream.CopyToAsync(fstream);
                // }

                resp = Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync(responseFile, CancellationToken.None));

                _logger?.LogInformation(@event, $"File {firstRequested} reading success");

                // fstream.Position = 0;


                System.IO.File.Delete(responseFile);

                return Ok(resp);


                // Response.Headers.Add("Content-Disposition", String.Format("attachment; Filename={0}; Filename*=UTF-8''{0}", firstRequested));
                // return File(fstream, "application/xml");
            }
            else
            {
                resp = UnavailableResponse(date);
            }

            var bytes = UTF8Encoding.UTF8.GetBytes(resp);
            return File(new MemoryStream(bytes), "application/xml");
        }

        private string UnavailableResponse(DateTime date) => "<s>\n" +
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
}