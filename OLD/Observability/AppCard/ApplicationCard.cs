using System.Reflection;
using Observability.Abstractions;

namespace Observability;

/// <summary>
/// Represents an application card containing key information about the application's environment and version.
/// </summary>
/// <remarks>
/// The <see cref="ApplicationCard"/> class provides properties to access the application's key,
/// environment, and version. The key must be in the format 'area.proyecto[.app]'. The environment is determined from
/// environment variables or defaults to "des". The version is retrieved from the entry assembly or defaults to
/// "1.0.0".</remarks>
public class ApplicationCard : IApplicationCard
{
    private readonly string _key;

    /// <summary>
    /// The application key (Application Identity) in the format 'area.proyecto[.Grupo].app'.
    /// </summary>
    public string Key { get => _key; }

    /// <summary>
    /// The application environment (e.g., "dev", "prod").
    /// </summary>
    public string Entorno { get; set; } = string.Empty;

    /// <summary>
    /// The application version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Constructs an <see cref="ApplicationCard"/> from the provided service key.
    /// </summary>
    /// <param name="key">Application key in the format 'area.proyecto[.app]'.</param>
    /// <exception cref="ArgumentException">When key is null/empty or does not contain at least two dots.</exception>
    public ApplicationCard(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        if (key.Split('.').Length < 3) throw new ArgumentException("Key must be in the format 'area.proyecto[.app]'");

        Entorno = Environment.GetEnvironmentVariable("ENTORNO")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "des";

        //_key = $"{Entorno}.{key}".ToLower();
        _key = key.ToLower();

        Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// Returns a short string representation with key, version and environment.
    /// </summary>
    public override string ToString() => $"App={Key} Version={Version} Entorno={Entorno}";
}