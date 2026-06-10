//using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rating.BusinessLayer.Data;
using Rating.BusinessLayer.Services;
using Serilog;
using StackExchange.Redis;
using Unir.Framework.Observability;
//using Rating.BusinessLayer.Models;
//using System.Collections.Generic;
//using System.Reflection.Emit;
//using Shaper.Core.BusinessLayer;
//using Shaper.Core.BusinessLayer.StartupComponents;
//

//namespace Rating.BusinessLayer.Data
//{
//    public class RatingDbContext : DbContext
//    {
//        public RatingDbContext(DbContextOptions<RatingDbContext> options) : base(options) { }
//        public DbSet<Rating> Ratings { get; set; }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            modelBuilder.Entity<Rating>()
//                .HasIndex(r => new { r.ArticleId, r.UserId })
//                .IsUnique();
//        }
//    }
//}

//namespace Rating.BusinessLayer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddUnirConfiguration();

        // OBSERVABILITY CONFIGURATION
        // [Demo V2] 2.1. Code: Startup
        
        builder
            .CreateObservabilityBuilder()
            .LoadFromConfiguration()
            .UseSerilog()
            .WithMetrics(metrics => {
                //Añadir métricas específicas del proyecto
                //metrics.AddHttpClientInstrumentation();
            })
            .WithTraces(traces => {
                //Añadir trazas específicas del proyecto
                traces.AddRabbitMQInstrumentation();
            })
            .BuildObservability();


        //.Configure(options =>
        //{
        //    options.EnableMetrics = true;
        //    options.EnableTracing = true;
        //    options.Metrics.Prefix = "unir";
        //    options.Metrics.WithConsoleExporter = false;S
        //    options.Metrics.Meters = ["System.Net.Http", "System.Runtime"];
        //    options.Tracing.WithConsoleExporter= false;
        //    options.Tracing.RedisUrl = "localhost:6379";
        //    return options;
        //})
        //------------------------------------

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<RedisCacheService>();


        builder.Services.AddTransient<IVoteService, VoteService>();

        builder.Services.AddDbContextFactory<VotingDbContext>(
            options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ??
                          "Data Source=./data/voting.db"));

        // Añadir CORS si es necesario
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .WithHeaders("traceparent", "tracestate", "x-traceid", "content-type", "authorization");
            });
        });



        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<VotePublisher>();
        //builder.Services.AddHostedService<RatingGeneratorHostedService>();
        //builder.Services.AddHostedService<VoteApprovalService>();

        // Add EF Core with SQLite
        //builder.Services.AddDbContext<RatingDbContext>(options =>
        //    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ??
        //                      "Data Source=ratings.db"));

        // OpenTelemetry configuration
        var otelResource = ResourceBuilder.CreateDefault()
            .AddService("RatingWebApi");

        var app = builder.Build();
        app.UseCors();

        // Apply migrations at startup
        //using (var scope = app.Services.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<RatingDbContext>();
        //    db.Database.EnsureCreated();
        //    db.Database.Migrate();
        //}

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();


        app.DatabaseStartup();


        //app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        Console.WriteLine("Service name: " + builder.Configuration.GetValue<string>("Serilog:WriteTo:1:Args:ResourceAttributes:service.name"));


        app.Run();
    }
}
