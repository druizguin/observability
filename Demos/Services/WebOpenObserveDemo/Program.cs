using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Context.Propagation;
using WebOpenObserveDemo.Services;
using System.Diagnostics;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7097")
              .AllowAnyMethod()
              .WithHeaders("traceparent", "tracestate", "content-type", "x-traceid");
    });
});

// Configurar OpenTelemetry para el backend
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("arc.openobserve.web")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.version"] = "1.0.0",
            ["service.namespace"] = "weblayer"
        }))
    .WithTracing(tracerBuilder =>
    {
        tracerBuilder
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.request.path", request.Path);
                    activity.SetTag("http.request.method", request.Method);
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    activity.SetTag("http.response.status_code", response.StatusCode);
                };
                options.Filter = context =>
                {
                    // No trazar las llamadas al proxy de telemetría para evitar loops
                    return !context.Request.Path.StartsWithSegments("/api/telemetry");
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                // CRÍTICO: Habilitar propagación de contexto
                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    activity.SetTag("http.request.url", request.RequestUri?.ToString());
                    activity.SetTag("http.client.target", request.RequestUri?.Host);
                };
                options.EnrichWithHttpResponseMessage = (activity, response) =>
                {
                    activity.SetTag("http.response.status_code", (int)response.StatusCode);
                };
                // Filtrar telemetría propia
                options.FilterHttpRequestMessage = (request) =>
                {
                    return !request.RequestUri?.PathAndQuery.Contains("/api/telemetry") ?? true;
                };
            })
            .AddSource("WebOpenObserveDemo.Rating")
            .AddSource("WebOpenObserveDemo.RatingApi")
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["OtelCollector:Endpoint"] ?? "http://localhost:4317");
                opt.Protocol = OtlpExportProtocol.Grpc;
            });
    });

// CRÍTICO: Configurar el propagador W3C Trace Context (estándar)
Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[]
{
    new TraceContextPropagator(),
    new BaggagePropagator()
}));

// Registrar HttpClient y RatingApiService
builder.Services.AddHttpClient<RatingApiService>(client =>
{
    var ratingApiUrl = builder.Configuration["RatingApi:BaseUrl"] ?? "https://localhost:7152";
    client.BaseAddress = new Uri(ratingApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler() // Opcional: agregar políticas de resiliencia
;

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();