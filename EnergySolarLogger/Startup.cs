using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using InfluxDB.Collector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnergySolarLogger
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            Metrics.Collector = new CollectorConfiguration()
                .Tag.With("host", Environment.GetEnvironmentVariable("COMPUTERNAME"))
                .Batch.AtInterval(TimeSpan.FromSeconds(5))
                .WriteTo.InfluxDB(Environment.GetEnvironmentVariable("INFLUX_ADDRESS"), Environment.GetEnvironmentVariable("INFLUX_DB"))
                .CreateCollector();

            var timer = new System.Threading.Timer(o => CollectSolarStats(), null, 0, 30000);
        }

        private void CollectSolarStats()
        {
            try
            {
                var output = new SolarMaxCollector(Environment.GetEnvironmentVariable("SOLARMAX_IP"), int.Parse(Environment.GetEnvironmentVariable("SOLARMAX_PORT"))).SendMessage(new string[] { "PAC", "KDY", "KT0" });
                Console.WriteLine($"Collected solar stats {string.Join(", ", output)}");

                Metrics.Measure("currentSolarProduction", output["PAC"] / 2);
                Metrics.Measure("todaySolarProduction", output["KDY"]);
                Metrics.Measure("totalSolarProduction", output["KT0"]);
            }
            catch
            {
                Console.WriteLine("Could not collect solar stats");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                Console.WriteLine(string.Join(", ", context.Request.Query.Select(x => $"{x.Key} : {x.Value.FirstOrDefault()}")));

                if (context.Request.Query.ContainsKey("u") && int.TryParse(context.Request.Query["u"].First(), out int totalEnergyUsed))
                {
                    Metrics.Measure("totalEnergyUsed", totalEnergyUsed);
                }
                if (context.Request.Query.ContainsKey("p") && int.TryParse(context.Request.Query["p"].First(), out int totalEnergyProvided))
                {
                    Metrics.Measure("totalEnergyProvided", totalEnergyProvided);
                }

                if (context.Request.Query.ContainsKey("cp") && int.TryParse(context.Request.Query["cp"].First(), out int currentEnergyProvided))
                {
                    Metrics.Measure("currentEnergyProvided", currentEnergyProvided);
                }

                if (context.Request.Query.ContainsKey("cu") && int.TryParse(context.Request.Query["cu"].First(), out int currentEnergyUsed))
                {
                    Metrics.Measure("currentEnergyUsed", currentEnergyUsed);
                }

                context.Response.StatusCode = (int) HttpStatusCode.OK;
                await context.Response.CompleteAsync();
                return;
            });
        }         
    }
}
