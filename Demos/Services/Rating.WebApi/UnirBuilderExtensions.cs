using Rating.BusinessLayer.Data;
using Rating.BusinessLayer.Dom;
using Serilog;

public static class UnirBuilderExtensions
{
    public static IHostApplicationBuilder AddUnirConfiguration(this IHostApplicationBuilder builder)
    {
        builder.Services.AddLogging();
        builder.Logging.ClearProviders();

        builder.Services.AddSwaggerGen();

        var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
               .AddEnvironmentVariables()
               .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            //.WriteTo.Console()
            //.WriteTo.OpenTelemetry()
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });

        return builder;
    }

    public static WebApplication DatabaseStartup(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VotingDbContext>();
            db.Database.EnsureCreated();

            if (!db.Products.Any())
            {
                var sampleProducts = new List<Product>
                {
                    new Product {Category = "Comida", Name = "Café Colombia" },
                    new Product {Category = "Comida", Name = "Té Verde Japonés" },
                    new Product {Category = "Comida", Name = "Chocolate Negro 70%" },
                    new Product {Category = "Comida", Name = "Galletas Artesanas" },
                    new Product { Category= "Comida", Name = "Zumo de Naranja Natural" },
                    new Product { Category= "Tecnología", Name = "Ratón inalámbrico" }
                };

                db.Products.AddRange(sampleProducts);
                db.SaveChanges();
            }
        }

        return app;
    }
}


