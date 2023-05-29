using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PackageRequest.Controllers
{
    [Route("[controller]")]
    public class ExperianScoringController : Controller
    {
        private readonly AppOptions _options;
        private readonly ILogger<ExperianScoringController>? _logger;
        public ExperianScoringController(ILogger<ExperianScoringController> logger, IOptions<AppOptions> options)
        {
            _options = options.Value;

            if (_options.Loging)
            {
                _logger = logger;
            }
        }

        [HttpPost]
        [RequestSizeLimit(209_715_200)]
        public async Task<IActionResult> OkbList()
        {
            var @event = new EventId(new Random().Next(), nameof(ExperianScoringController));
            DateTime date = DateTime.Now;
            string id = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString();

            if (Request.Body == null)
            {
                _logger?.LogWarning(@event, $"CRE request is empty");
                return Problem(title: @event.Id.ToString(), detail: _options.LogsPath + " ExperianScoring.log", statusCode: 500);
            }

            var streamReader = new StreamReader(Request.Body);
            string xmlData = await streamReader.ReadToEndAsync();

            string trimText = xmlData.Substring(xmlData.LastIndexOf("\"ActionFlag\"") + 12);
            string actionFlagText = trimText.Substring(0, trimText.IndexOf("--"));
            int actionFlag = int.Parse(actionFlagText.Trim());

            string resp = "";

            _logger.LogInformation(@event, $"Request action flag {actionFlag}");

            if (actionFlag == 7)
            {
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
                string[] files = Directory.GetFiles(_options.ScoringEiResponcePath); //Берем список файлов для ответа отсюда

                var nameFiles = files.Select(x =>
                {
                    string[] words = x.Split(new char[] { '\\' });
                    return words.Last();
                });

                string str = "<s><a n = \"Name\">???</a></s>";
                string strXml = "";

                nameFiles.ToList().ForEach(x => strXml += str.Replace("???", x) + '\n');

                resp = "<s>\n" +
                                  "<s n=\"Data\">\n" +
                                      "<a n=\"ActionFlag\">9</a>\n" +
                                      "<c n=\"Files\">\n" +
                                      "<s>\n" +
                                      "<c n= \"wlListOfFiles\">\n" +
                                      strXml +
                                      "</c>\n" +
                                      "<a n=\"wlReturnCount\">" + nameFiles.Count().ToString() + "</a>\n" +
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
                Regex regex = new Regex("RESP(.*?)pem.pem");
                MatchCollection matches = regex.Matches(xmlData);
                string[] files = Directory.GetFiles(_options.ScoringEiResponcePath); //Берем список файлов для ответа отсюда

                foreach (Match match in matches)
                {
                    foreach (string s in files)
                    {
                        if (s.IndexOf(match.Value) > 0)
                        {
                            Thread.Sleep(_options.SleepExperian);

                            var fstream = System.IO.File.OpenRead(_options.ScoringEiResponcePath + match.Value);

                            Response.Headers.Add("Content-Disposition", String.Format("attachment; Filename={0}; Filename*=UTF-8''{0}", match.Value));
                            return File(fstream, "application/xml");
                        }
                    }
                }
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