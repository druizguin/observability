using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Cryptography;
using Observability.Demo.Observability;
using Observability.Abstractions;

namespace Observability.Demo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMetricsService _metricsService;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMetricsService metricService)
        {
            _logger = logger;
            _metricsService = metricService;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            var tagContext = new MetricsContext("project.Demo", "1.0.0");

            _metricsService.Counter(MetricNames.Todo_Metric_Test_Counter.ToString())
                .WithDescription("A sample counter metric")
                .LabelContext(tagContext)
                .Up();

            return SampleData();
        }

        [HttpGet("/histogram", Name = "HistogramRecord")]
        public ResultGet GetHistogramRecord()
        {
            var sw = new Stopwatch();
            sw.Start();

            var tagContext = new MetricsContext("project.Demo", "1.0.0");

            // Simulate a duration in milliseconds
            Thread.Sleep(TimeSpan.FromMilliseconds(Random.Shared.Next(100, 1000)));
            WeatherForecast[] result = SampleData();

            sw.Stop();
            var duration = sw.Elapsed.TotalMilliseconds;

            _metricsService.Histogram(MetricNames.Todo_Metric_Test_Histogram.ToString())
                .WithDescription("A sample histogram metric")
                .LabelContext(tagContext)
                .Record(duration);

            return new ResultGet
            {
                Data = result,
                Duration = duration.ToString("F2") + " ms"
            };
        }

        private static WeatherForecast[] SampleData()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }

  
    public class ResultGet
    {
        public IEnumerable<WeatherForecast>? Data { get; set; }
        public string? Duration { get; set; }
    }
}
