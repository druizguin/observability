namespace Observability.Tests.Implementation.AppCard;

using System;
using System.ComponentModel;
using System.Reflection;
using AutoFixture;
using Xunit;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public class ApplicationCardTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe inicializar Key en minúsculas y asignar Entorno y Version")]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var key = "area.proyecto.app";

        // Act
        var card = new ApplicationCard(key);

        // Assert
        Assert.Equal(key.ToLower(), card.Key);
        Assert.False(string.IsNullOrWhiteSpace(card.Entorno));
        Assert.False(string.IsNullOrWhiteSpace(card.Version));
    }

    [Fact]
    [DisplayName("Debe lanzar ArgumentException si key es nulo o vacío")]
    public void Constructor_ShouldThrowIfKeyIsNullOrEmpty()
    {
        // Act & Assert
        var ex1 = Assert.Throws<ArgumentNullException>(() => new ApplicationCard(null!));
        Assert.Equal("key", ex1.ParamName);

        var ex2 = Assert.Throws<ArgumentException>(() => new ApplicationCard(string.Empty));
        Assert.Equal("key", ex2.ParamName);
    }

    [Fact]
    [DisplayName("Debe lanzar ArgumentException si key no tiene al menos 3 segmentos")]
    public void Constructor_ShouldThrowIfKeyHasLessThanThreeSegments()
    {
        // Arrange
        var invalidKey = "area.proyecto";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new ApplicationCard(invalidKey));
        Assert.Contains("format", ex.Message);
    }

    [Fact]
    [DisplayName("Debe usar variable de entorno ENTORNO si está definida")]
    public void Constructor_ShouldUseEntornoEnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENTORNO", "prod");
        var key = "area.proyecto.app";

        // Act
        var card = new ApplicationCard(key);

        // Assert
        Assert.Equal("prod", card.Entorno);

        // Cleanup
        Environment.SetEnvironmentVariable("ENTORNO", null);
    }

    [Fact]
    [DisplayName("Debe usar variable de entorno ASPNETCORE_ENVIRONMENT si ENTORNO no está definida")]
    public void Constructor_ShouldUseAspNetCoreEnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENTORNO", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "stage");
        var key = "area.proyecto.app";

        // Act
        var card = new ApplicationCard(key);

        // Assert
        Assert.Equal("stage", card.Entorno);

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    [Fact]
    [DisplayName("Debe asignar 'des' como Entorno si no hay variables definidas")]
    public void Constructor_ShouldDefaultEntornoToDes()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENTORNO", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        var key = "area.proyecto.app";

        // Act
        var card = new ApplicationCard(key);

        // Assert
        Assert.Equal("des", card.Entorno);
    }

    [Fact]
    [DisplayName("ToString debe devolver formato correcto con Key, Version y Entorno")]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var key = "area.proyecto.app";
        var card = new ApplicationCard(key);

        // Act
        var result = card.ToString();

        // Assert
        Assert.Contains($"App={card.Key}", result);
        Assert.Contains($"Version={card.Version}", result);
        Assert.Contains($"Entorno={card.Entorno}", result);
    }
}
