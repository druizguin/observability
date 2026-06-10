namespace Observability.Abstractions;

public interface IApplicationCard
{
    /// <summary>
    /// Deployment or environment name (for example "PROD", "staging", "des").
    /// </summary>
    string Entorno { get; set; }

    /// <summary>
    /// Unique application key, typically in the format "area.project.app".
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Application version string.
    /// </summary>
    string Version { get; set; }
}
