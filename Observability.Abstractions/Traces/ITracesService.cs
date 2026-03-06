namespace Observability.Abstractions;

using System.Diagnostics;

/// <summary>
/// Service responsible for creating and retrieving activities (traces).
/// </summary>
public interface ITracesService
{
    /// <summary>
    /// Returns the current activity created from the provided <see cref="TraceBuilder"/>.
    /// Caller can inspect or modify the returned <see cref="Activity"/>.
    /// </summary>
    /// <param name="builder">Builder containing trace name, kind and optional parent/propagation context.</param>
    /// <returns>The created or current <see cref="Activity"/> instance, or <c>null</c> if no activity was created.</returns>
    Activity? GetCurrentActivity(TraceBuilder builder);

    /// <summary>
    /// Registers a new activity process using the provided <see cref="TraceBuilder"/>.
    /// The returned <see cref="IActivityProcess"/> encapsulates lifecycle and labels for the activity.
    /// </summary>
    /// <param name="builder">Builder that configures the activity to be registered.</param>
    /// <returns>An <see cref="IActivityProcess"/> representing the registered activity.</returns>
    IActivityProcess RegisterActivity(TraceBuilder builder);
}