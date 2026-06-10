namespace Observability.Abstractions;

public interface IActivityProcessAsync : IDisposable, IObservabilityLabels
{
    /// <summary>
    /// Executes an asynchronous function within the activity process and returns its result.
    /// </summary>
    /// <typeparam name="TResult">Return type.</typeparam>
    /// <param name="func">Async function that receives the <see cref="IActivityProcess"/>.</param>
    /// <param name="onError">Optional async error handler invoked when an exception occurs.</param>
    /// <returns>The task that resolves to the result of the function or the onError handler.</returns>
    Task<TResult> ExecuteAsync<TResult>(Func<IActivityProcess, Task<TResult>> func, Func<IActivityProcess, Exception, Task<TResult>>? onError = null);

    /// <summary>
    /// Executes an asynchronous action within the activity process.
    /// </summary>
    /// <param name="func">Async action that receives the <see cref="IActivityProcess"/>.</param>
    /// <param name="onError">Optional async error handler invoked when an exception occurs.</param>
    /// <returns>A task that completes when execution finishes.</returns>
    Task ExecuteAsync(Func<IActivityProcess, Task> func, Func<IActivityProcess, Exception, Task>? onError = null);
}
