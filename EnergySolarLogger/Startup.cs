using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        private SolarMaxCollector _solarmaxCollector;
        private static Timer _solarStatsTimer;

        private int _lastCurrentEnergyUsed = 0;
        private int _lastCurrentEnergyProvided = 0;
        private int _lastCurrentSolarEnergy = 0;

        private readonly Dictionary<string, string> _queryStringToMeasurementMapping = new Dictionary<string, string>()
        {
            { "u", "totalEnergyUsed" },
            { "p", "totalEnergyProvided" },
            { "cu", "currentEnergyUsed" },
            { "cp", "currentEnergyProvided" }
        };

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            Metrics.Collector = new CollectorConfiguration()
                .Tag.With("host", Environment.GetEnvironmentVariable("COMPUTERNAME"))
                .Batch.AtInterval(TimeSpan.FromSeconds(5))
                .WriteTo.InfluxDB(Environment.GetEnvironmentVariable("INFLUX_ADDRESS"), Environment.GetEnvironmentVariable("INFLUX_DB"))
                .CreateCollector();

            _solarmaxCollector = new SolarMaxCollector(Environment.GetEnvironmentVariable("SOLARMAX_IP"), int.Parse(Environment.GetEnvironmentVariable("SOLARMAX_PORT")));
            _solarStatsTimer = new Timer(o => CollectSolarStats(), null, 0, 30000);
        }

        private void CollectSolarStats()
        {
            try
            {
                _lastCurrentEnergyUsed = 0;
                Console.WriteLine("Collection solar stats...");
       
                var message = new SolarMaxMessage(new string[] { "PAC", "KDY", "KT0" });
                var response = _solarmaxCollector.SendMessage(message);

                Metrics.Measure("currentSolarProduction", response["PAC"] / 2);
                Metrics.Measure("todaySolarProduction", response["KDY"]);
                Metrics.Measure("totalSolarProduction", response["KT0"]);
                _lastCurrentSolarEnergy = response["PAC"] / 2;

                Console.WriteLine($"Solar stats collected successfully: {string.Join(", ", response)}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Could not collect solar stats {ex}");
            }
            finally
            {
                var currentHomeUsage = _lastCurrentEnergyUsed + _lastCurrentSolarEnergy - _lastCurrentEnergyProvided;
                Metrics.Measure("currentHomeUsage", currentHomeUsage);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Run(async (context) =>
            {
                try
                {
                    Console.WriteLine("Receiving request...");
                    Console.WriteLine(string.Join(", ", context.Request.Query.Select(x => $"{x.Key} : {x.Value.FirstOrDefault()}")));

                    foreach(var query in _queryStringToMeasurementMapping)
                    {
                        if (context.Request.Query.ContainsKey(query.Key) && int.TryParse(context.Request.Query[query.Key].First(), out int value))
                        {
                            Metrics.Measure(query.Value, value);

                            if (query.Key == "cu")
                            {
                                _lastCurrentEnergyUsed = value;
                            }
                            else if (query.Key == "cp")
                            {
                                _lastCurrentEnergyProvided = value;
                            }
                        }
                    }

                    Console.WriteLine("Request processed successfully");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Could not process request {ex}");
                }
                finally
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.CompleteAsync();
                }
            });
        }
    }
}
