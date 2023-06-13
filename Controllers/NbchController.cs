using System;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Linq;

#nullable enable
namespace PackageRequest.Controllers
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

        [HttpGet]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        [Route("{id}")]
        public IActionResult NbchGet([FromRoute]string id)
        {
            string[] files = Directory.GetFiles(_options.RKK_NbchResponcePath);
            var @event = _event;

            FileStream fstream = null;

            if (files.Length == 0)
            {
                _logger.LogError(@event, $"Files in response folder {_options.RKK_NbchResponcePath} not found");
                return Problem(title: @event.Id.ToString(), detail: _options.LogsPath + " Nbch.log", statusCode: 500);
            }

            try
            {
                foreach (string fileName in files)
                {
                    if (fileName.Contains(id.Remove(id.IndexOf("."))))
                    {
                        _logger.LogInformation(@event, $"File {fileName} search success");

                        Response.Headers.Add("Accept-Ranges", "bytes");
                        Response.Headers.ContentLength = 89506816;

                        try
                        {
                            fstream = System.IO.File.OpenRead(fileName);
                            _logger.LogInformation(@event, $"File {fileName} reading success");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(@event, ex, $"File {fileName} reading fail");
                            return Problem(title: @event.Id.ToString(), detail: _options.LogsPath + " Nbch.log", statusCode: 500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(@event, ex, $"File {id} not found");
                return Problem(title: @event.Id.ToString(), detail: _options.LogsPath + " Nbch.log", statusCode: 404);
            }

            Task.Delay(_options.SleepNbch);

            return File(fstream, "application/pkcs7-mime");
        }

        [HttpPost]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public async Task<ActionResult> NbchPost(object body)
        {
            var @event = _event;

            var streamReader = new StreamReader(Request.Body);
            string xmlData = await streamReader.ReadToEndAsync();

            if (string.IsNullOrEmpty(xmlData)) 
            {
                xmlData = body.ToString();
            }

            if (string.IsNullOrEmpty(xmlData))
            {
                _logger.LogWarning(@event, $"CRE request is empty");
                throw new Exception()
                {
                    HelpLink = @event.Id.ToString()
                };
            }

            _logger.LogInformation(@event, $"CRE pushed request {xmlData}");

            string fileName = string.Empty;
            try
            {

                Regex regex = new Regex(@"(?<=6810BB|6810BB00).*?(?=\.XML\.gz)");
                MatchCollection matches = regex.Matches(xmlData);
                fileName = matches.FirstOrDefault().Value;

                //fileName = xmlData.Substring(xmlData.IndexOf("6801BB"), xmlData.IndexOf(".XML.gz") - xmlData.IndexOf("6801BB"));
            }
            catch (Exception ex)
            {
                _logger.LogError(@event, ex, $"Response name build from {fileName} failed");
                throw ex;
            }

            string responseFileName = string.Empty;
            string[] files;
            try
            {
                files = Directory.GetFiles(_options.RKK_NbchResponcePath);
                responseFileName = fileName + "_Reply.XML.gz.p7s.p7m";
                _logger.LogInformation(@event, $"Response file {responseFileName} found");
            }
            catch (Exception ex)
            {
                _logger.LogError(@event, ex, $"File {responseFileName} in response folder {_options.RKK_NbchResponcePath} not found");
                throw ex;
            }

            try
            {
                _logger.LogInformation(@event, responseFileName);
                System.IO.File.Move(files[0], files[0].Remove(files[0].IndexOf('6')) + responseFileName);
                _logger.LogInformation(@event, $"Moving file {responseFileName} to ftp folder {_options.FtpDirectoryOut} success");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(@event, ex, $"Moving file {responseFileName} to ftp folder {_options.FtpDirectoryOut} fail");
                throw ex;
            }

            // записываем пришедший файл на винт
            /*using (var fileStream = new FileStream(path + fileName, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }*/
        }
    }
}