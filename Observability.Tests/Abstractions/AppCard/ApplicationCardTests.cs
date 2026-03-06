namespace Observability.Tests.Abstractions.AppCard;


using System.ComponentModel;
using AutoFixture;
using Moq;
using Observability.Abstractions;
using Xunit;

public class ApplicationCardTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe permitir establecer y obtener propiedades Entorno y Version")]
    public void ShouldSetAndGetProperties()
    {
        // Arrange
        var mock = new Mock<IApplicationCard>();
        var entorno = _fixture.Create<string>();
        var version = _fixture.Create<string>();

        // Act
        mock.Setup(p=>p.Entorno).Returns(entorno);
        mock.Setup(p => p.Version).Returns(version);

        // Assert
        Assert.Equal(entorno, mock.Object.Entorno);
        Assert.Equal(version, mock.Object.Version);
    }

    [Fact]
    [DisplayName("Debe devolver la propiedad Key configurada en el mock")]
    public void ShouldReturnConfiguredKey()
    {
        // Arrange
        var expectedKey = _fixture.Create<string>();
        var mock = new Mock<IApplicationCard>();
        mock.SetupGet(x => x.Key).Returns(expectedKey);

        // Act
        var key = mock.Object.Key;

        // Assert
        Assert.Equal(expectedKey, key);
    }

    [Fact]
    [DisplayName("Debe verificar que se asigna Entorno y Version correctamente")]
    public void ShouldVerifyPropertyAssignments()
    {
        // Arrange
        var mock = new Mock<IApplicationCard>();
        var entorno = _fixture.Create<string>();
        var version = _fixture.Create<string>();

        // Act
        mock.Object.Entorno = entorno;
        mock.Object.Version = version;

        // Assert
        mock.VerifySet(x => x.Entorno = entorno, Times.Once);
        mock.VerifySet(x => x.Version = version, Times.Once);
    }
}
