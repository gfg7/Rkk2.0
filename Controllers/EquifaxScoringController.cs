using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageRequest;

namespace Rkk2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EquifaxScoringController : ControllerBase
    {
        private readonly AppOptions _options;
        private readonly ILogger<EquifaxScoringController>? _logger;
        private readonly Root _listReponse;
        private EventId _event => new EventId(new Random().Next(), nameof(EquifaxScoringController));

        public EquifaxScoringController(IOptions<AppOptions> options, ILogger<EquifaxScoringController> logger)
        {
            _options = options.Value;

            if (_options.Loging)
            {
                _logger = logger;
            }

            _listReponse = new Root();
        }

        [HttpPost("/api/auth/get")]//1
        public ActionResult Auth()
        {
            _logger?.LogInformation(_event, "CRE asked for auth token");
            return Ok(Guid.NewGuid());
        }

        [HttpGet]
        [Route("/api/resource/{folder}")]//3
        public ActionResult GetResource([FromRoute] string folder)
        {
            _logger?.LogInformation(_event, $"CRE asked for list of files in folder {folder}");

            var response = _listReponse;
            response.name = folder;

            string[] files = Directory.GetFiles(_options.ScoringEquifaxRTakenResponcePath);

            foreach (var item in files)
            {
                var filename = Path.GetFileName(item);

                response.items.Add(new Item()
                {
                    path = $"/{filename}",
                    virtualPath = $"/{filename}",
                    name = filename,
                    extension = Path.GetExtension(filename)
                });
            }

            return Ok(response);
        }


        [HttpPost]
        [Route("/api/resource/{folder}/{filename}")]//2?
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        public ActionResult PostResource([FromRoute] string folder, [FromRoute] string filename)
        {
            var @event = _event;

            _logger?.LogInformation(_event, $"CRE uploaded file {filename}");

            string[] files = Directory.GetFiles(_options.ScoringEquifaxResponcePath);
            var responseFile = files.FirstOrDefault(x => !x.Contains("taken"));

            if (files.Length == 0 || string.IsNullOrEmpty(responseFile))
            {
                _logger?.LogError(@event, $"Files in response folder {_options.ScoringEquifaxResponcePath} not found");
                throw new FileNotFoundException();
            }

            var requestFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filename)));//убирает .zip.sgn.enc
            var takenFile = _options.ScoringEquifaxRTakenResponcePath + Path.GetFileName($"outbox_{requestFileName}_out.zip.sgn.enc");
            System.IO.File.Copy(responseFile, takenFile);

            _logger?.LogInformation(@event, $"File {responseFile} is taken {takenFile} - response for request {filename} is created");

            var usedFile = _options.ScoringEquifaxRUsedResponcePath + Path.GetFileName(responseFile);
            System.IO.File.Move(responseFile, usedFile);

            _logger?.LogInformation(@event, $"Original {responseFile} is moved to used {usedFile}");

            return Ok();
        }

        [HttpGet]//4
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue), RequestSizeLimit(long.MaxValue)]
        [Route("/api/download/{folder}/{filename}")]
        public async Task<ActionResult> EquifaxScoringGet([FromRoute] string folder, [FromRoute] string filename)
        {
            var @event = _event;

            _logger?.LogInformation(@event, $"CRE ask file {filename}");

            filename = filename.Replace(".reject", "");

            Response.Headers.Add("Accept-Ranges", "bytes");

            string[] files = Directory.GetFiles(_options.ScoringEquifaxRTakenResponcePath);
            var responseFile = files.FirstOrDefault(x=> x == filename);

            if (files.Length == 0 || string.IsNullOrEmpty(responseFile))
            {
                _logger?.LogError(@event, $"Files in response folder {_options.ScoringEquifaxRTakenResponcePath} not found");
                throw new FileNotFoundException();
            }

            await Task.Delay(_options.SleepNbch);

            for (int retry = 0; retry <= (_options.EquifaxScoringRetryCount ?? 1);)
            {
                try
                {
                    Stream fstream = new MemoryStream();

                    using (var stream = new FileStream(responseFile, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = 0;
                        await stream.CopyToAsync(fstream);
                    }

                    _logger?.LogInformation(@event, $"File {responseFile} reading success");

                    System.IO.File.Move(responseFile, _options.ScoringEquifaxResponcePath + Path.GetFileName(responseFile));
                    _logger?.LogInformation(@event, $"Response {responseFile} is moved to used {_options.ScoringEquifaxRUsedResponcePath + Path.GetFileName(responseFile)}");

                    fstream.Position = 0;
                    return File(fstream, "application/pkcs7-mime");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(@event, ex, $"File {filename} fail - iteration {retry}");
                    retry++;
                }
            }

            throw new FileNotFoundException();
        }
    }

    #region equifax file server
    public class Item
    {
        public string path { get; set; }
        public string virtualPath { get; set; }
        public string name { get; set; }
        public int size { get; set; } = 214748364;
        public string extension { get; set; }
        public DateTime modified { get; set; } = DateTime.Now;
        public int mode { get; set; }
        public bool isDir { get; set; } = false;
        public string type { get; set; } = "blob";
    }

    public class Sorting
    {
        public string by { get; set; } = "name";
        public bool asc { get; set; } = false;
    }

    public class Root
    {
        public List<Item> items { get; set; } = new List<Item>();
        public int numDirs { get; set; } = 1;
        public int numFiles { get; set; } = 1;
        public Sorting sorting { get; set; } = new Sorting();
        public string path { get; set; } = "/";
        public string virtualPath { get; set; } = "/";
        public string name { get; set; } = "scoring-test";
        public int size { get; set; } = 214748364;
        public string extension { get; set; }
        public DateTime modified { get; set; } = DateTime.Now;
        public int mode { get; set; }
        public bool isDir { get; set; } = true;
        public string type { get; set; }
    }
    #endregion
}