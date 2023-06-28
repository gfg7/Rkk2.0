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
            return Ok();
        }

        [HttpPost]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public async Task<ActionResult> OkbList()
        {
            var @event = new EventId(new Random().Next(), nameof(ExperianController));
            DateTime date = DateTime.Now;
            // string id = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString();

            if (Request.Body == null)
            {
                _logger?.LogWarning(@event, $"CRE request is empty");
                return Problem(title: @event.Id.ToString(), detail: _options.LogsPath + " Experian.log", statusCode: 500);
            }

            var streamReader = new StreamReader(Request.Body);
            string xmlData = await streamReader.ReadToEndAsync();

            _logger?.LogInformation(@event, "CRE request: " + xmlData);

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
                // string[] files = Directory.GetFiles(_options.RKK_EiResponcePath); //Берем список файлов для ответа отсюда

                // var nameFiles = files.Select(x =>
                // {
                //     string[] words = x.Split(new char[] { '\\' });
                //     return words.Last();
                // });

                var firstRequested = _requestStore.PeekRequest().Replace("CHD", "RESP");

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
            else if (actionFlag == 1)
            {

                Thread.Sleep(_options.SleepExperian);

                var firstRequested = _requestStore.ProcessRequest().Replace("CHD", "RESP");

                _logger?.LogInformation(@event, $"Requested file {firstRequested} is removed from queue");

                string[] files = Directory.GetFiles(_options.RKK_EiResponcePath); //Берем список файлов для ответа отсюда
                System.IO.File.Move(files[0], _options.RKK_EiResponcePath + firstRequested);

                Stream fstream = new MemoryStream();

                using (var stream = new FileStream(_options.RKK_EiResponcePath + firstRequested, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(fstream);
                }

                System.IO.File.Delete(_options.RKK_EiResponcePath + firstRequested);

                _logger?.LogInformation(@event, $"File {firstRequested} reading success");

                fstream.Position = 0;

                Response.Headers.Add("Content-Disposition", String.Format("attachment; Filename={0}; Filename*=UTF-8''{0}", firstRequested));
                return File(fstream, "application/xml");
            }
            else
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

            var bytes = UTF8Encoding.UTF8.GetBytes(resp);
            return File(new MemoryStream(bytes), "application/xml");
        }
    }
}