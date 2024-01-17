using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PackageRequest;
using PackageRequest.Controllers.Ei;
using PackageRequest.Controllers.Equifax;
using PackageRequest.Controllers.Nbki;
using Rkk2._0.Controllers.Equifax;

namespace Rkk2._0
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        private AppOptions _options;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Rkk2.0"
                });
            });

            services.Configure<AppOptions>(Configuration);
            services.AddControllers();
            services.AddHealthChecks();

            _options = Configuration.Get<AppOptions>();

            if (_options.FTPEquifaxEnabled)
            {
                services.AddHostedService<EquifaxFTPService>();
            }

            services.AddLogging(loggingBuilder =>
            {

                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();

                if (_options.FileLogging)
                {
                    var logsFolder = _options.LogsPath;
                    var minLevel = _options.MinLevel;
                    var sizeLimit = _options.FileSizeLimitBytes;
                    var maxFileCount = _options.MaxRollingFiles;

                    if (_options.LogBki)
                    {
                        loggingBuilder.BuildLogger(logsFolder, "Nbch", "Nbch", nameof(NbchController), maxFileCount, sizeLimit, minLevel);
                        loggingBuilder.BuildLogger(logsFolder, "Experian", "Experian", nameof(ExperianController), maxFileCount, sizeLimit, minLevel);
                        loggingBuilder.BuildLogger(logsFolder, "EquifaxFTP", "EquifaxFTP", nameof(EquifaxFTPService), maxFileCount, sizeLimit, minLevel);
                        loggingBuilder.BuildLogger(logsFolder, "EquifaxFileServer", "EquifaxFileServer", nameof(EquifaxFileServerController), maxFileCount, sizeLimit, minLevel);
                    }

                    if (_options.LogIncomming)
                    {
                        loggingBuilder.BuildLogger(logsFolder, "RequestResponse", "RequestResponse", nameof(LoggingMiddleware), maxFileCount, sizeLimit, minLevel);
                    }
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}/{httpReq.Headers["X-Forwarded-Prefix"]}" } };
                });
            });
            app.UseSwaggerUI(c =>
            {
                string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
                c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "rkk2.0");
            });

            app.UseMiddleware<LoggingMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
            {
                AllowCachingResponses = false
            });
        }
    }
}
