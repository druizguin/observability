using Observability;
using Observability.Demo.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
            .CreateObservabilityBuilder()
            .LoadFromConfiguration()
            .UseSerilog()
            .WithMetrics(metrics => {
                //A�adir m�tricas espec�ficas del proyecto
                //metrics.AddHttpClientInstrumentation();
            })
            .WithTraces(traces => {
                //A�adir trazas espec�ficas del proyecto
            })
            .BuildObservability();



builder.Services.AddSingleton<IWebApiDynamicMetricsService, WebApiDynamicMetricsService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
