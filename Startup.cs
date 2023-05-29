using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageRequest;
using PackageRequest.Controllers;
using NReco.Logging.File;
using Microsoft.OpenApi.Models;

namespace Rkk2._0
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IOptions<AppOptions> options)
        {
            Configuration = configuration;
            _options = options.Value;
        }

        private readonly AppOptions _options;
        public IConfiguration Configuration { get; }

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
            services.AddLogging(loggingBuilder =>
            {
                var logsFolder = _options.LogsPath;
                var minLevel = _options.MinLevel;
                var sizeLimit = _options.FileSizeLimitBytes;
                var maxFileCount = _options.MaxRollingFiles;

                loggingBuilder.BuildLogger(logsFolder, "Nbch", "Nbch", nameof(NbchController), maxFileCount, sizeLimit, minLevel);
                loggingBuilder.BuildLogger(logsFolder, "Experian", "Experian", nameof(ExperianController), maxFileCount, sizeLimit, minLevel);
                loggingBuilder.BuildLogger(logsFolder, "Equifax", "Equifax", nameof(EquifaxController), maxFileCount, sizeLimit, minLevel);
                loggingBuilder.BuildLogger(logsFolder, "ExperianScoring", "ExperianScoring", nameof(ExperianScoringController), maxFileCount, sizeLimit, minLevel);
                loggingBuilder.BuildLogger(logsFolder, "RequestResponse", "RequestResponse", nameof(LoggingMiddleware), maxFileCount, sizeLimit, minLevel);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var options = scope.ServiceProvider.GetRequiredService<IOptions<AppOptions>>();

                if (options.Value.LogIncomming)
                {
                    app.UseMiddleware<LoggingMiddleware>();
                }
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

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
