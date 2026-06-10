namespace Observability.Abstractions;

using System.Diagnostics;

/// <summary>
/// Represents an activity process: the Activity instance plus associated labels and execution helpers.
/// Implementations manage activity lifecycle and provide helper Execute/ExecuteAsync methods that set activity status and dispose the activity.
/// </summary>
public interface IActivityProcess : IDisposable, IObservabilityLabels, IActivityProcessAsync
{
    /// <summary>
    /// The underlying <see cref="Activity"/> for this process. May be <c>null</c>.
    /// </summary>
    Activity? Activity { get; }

    /// <summary>
    /// The traces service that created this activity process. May be <c>null</c>.
    /// </summary>
    ITracesService? Service { get; }

    /// <summary>
    /// Executes the provided synchronous action inside the activity process.
    /// The implementation should mark activity status and dispose the activity when execution finishes or on error.
    /// </summary>
    /// <param name="func">Action to execute passing this process.</param>
    /// <param name="onError">Optional error handler invoked when an exception occurs.</param>
    void Execute(Action<IActivityProcess> func, Action<IActivityProcess, Exception>? onError = null);

    /// <summary>
    /// Executes the provided synchronous function inside the activity process and returns its result.
    /// The implementation should mark activity status and dispose the activity when execution finishes or on error.
    /// </summary>
    /// <typeparam name="TResult">Return type.</typeparam>
    /// <param name="func">Function to execute passing this process.</param>
    /// <param name="onError">Optional error handler invoked when an exception occurs, must return a TResult.</param>
    /// <returns>The result returned by the provided function or by the onError handler if invoked.</returns>
    TResult Execute<TResult>(Func<IActivityProcess, TResult> func, Func<IActivityProcess, Exception, TResult>? onError = null);
}
