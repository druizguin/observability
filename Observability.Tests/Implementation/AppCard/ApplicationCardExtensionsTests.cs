namespace Observability.Tests.Implementation.AppCard;

using System;
using System.ComponentModel;
using AutoFixture;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Observability.Abstractions;

public class ApplicationCardExtensionsTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("BuildAppCard debe crear ApplicationCard con Key, Version y Entorno correctos")]
    public void BuildAppCard_ShouldCreateApplicationCardWithCorrectProperties()
    {
        // Arrange
        var serviceName = "area.proyecto.app";
        var builder = Host.CreateApplicationBuilder();

        // Act
        var appCard = InvokeBuildAppCard(builder, serviceName);

        // Assert
        Assert.NotNull(appCard);
        Assert.Equal(serviceName.ToLower(), appCard.Key);
        Assert.False(string.IsNullOrWhiteSpace(appCard.Version));
        Assert.False(string.IsNullOrWhiteSpace(appCard.Entorno));
    }

    [Fact]
    [DisplayName("BuildAppCard debe agregar configuraciones al Configuration")]
    public void BuildAppCard_ShouldAddSettingsToConfiguration()
    {
        // Arrange
        var serviceName = "area.proyecto.app";
        var builder = Host.CreateApplicationBuilder();

        // Act
        var appCard = InvokeBuildAppCard(builder, serviceName);

        // Assert
        Assert.Equal(appCard.Key, builder.Configuration["ApplicationCard:Key"]);
        Assert.Equal(appCard.Version, builder.Configuration["ApplicationCard:Version"]);
        Assert.Equal(appCard.Entorno, builder.Configuration["ApplicationCard:Entorno"]);
    }

    [Fact]
    [DisplayName("BuildAppCard debe registrar IOptions<IApplicationCard> en el contenedor")]
    public void BuildAppCard_ShouldRegisterIOptionsInServices()
    {
        // Arrange
        var serviceName = "area.proyecto.app";
        var builder = Host.CreateApplicationBuilder();

        // Act
        var appCard = InvokeBuildAppCard(builder, serviceName);
        var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IApplicationCard>>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(appCard.Key, options.Value.Key);
    }

    [Fact]
    [DisplayName("BuildAppCard debe lanzar ArgumentException si serviceName es inválido")]
    public void BuildAppCard_ShouldThrowIfServiceNameIsInvalid()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => InvokeBuildAppCard(builder, "invalid.key"));
        Assert.Contains("format", ex.Message);
    }

    // Helper para invocar el método interno mediante reflexión
    private static IApplicationCard InvokeBuildAppCard(IHostApplicationBuilder builder, string serviceName)
    {
        try
        {
            var method = typeof(ApplicationCardExtensions).GetMethod("BuildAppCard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (IApplicationCard)method?.Invoke(null, new object[] { builder, serviceName })!;

        }
        catch (Exception ex)
        {
            throw ex.InnerException != null ? ex.InnerException : ex;
        }
    }
}
