namespace Observability;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Observability.Abstractions;

/// <summary>
/// Default implementation of <see cref="IActivityProcess"/> that manages activity lifecycle,
/// labels and convenient Execute/ExecuteAsync helpers which set activity status and dispose the activity.
/// </summary>
public class ActivityProcess : IActivityProcess
{
    internal readonly ILabelNameBuilder _tracesNameBuilder;

    /// <summary>
    /// The activity being processed.
    /// </summary>
    public Activity? Activity { get; internal set; }

    /// <summary>
    /// The traces service that created the activity.
    /// </summary>
    public ITracesService? Service { get; internal set; }

    /// <summary>
    /// The labels to set as tags on the activity when disposed.
    /// </summary>
    public IDictionary<string, object?> Labels { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Creates a new <see cref="ActivityProcess"/>.
    /// </summary>
    /// <param name="tracesNameBuilder">Used to map label keys (internal name builder).</param>
    public ActivityProcess(ILabelNameBuilder tracesNameBuilder)
    {
        _tracesNameBuilder = tracesNameBuilder;
    }

    /// <summary>
    /// Executes a synchronous function inside the activity process, sets status and disposes the activity.
    /// </summary>
    public TResult Execute<TResult>(
        Func<IActivityProcess, TResult> func, 
        Func<IActivityProcess, Exception, TResult>? onError = null)
    {
        var process = this;

        try
        {
            var result = func(process);
            Activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity?.AddException(ex);

            if (onError != null) return onError(process, ex);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    /// <summary>
    /// Executes a synchronous action inside the activity process, sets status and disposes the activity.
    /// </summary>
    public void Execute(
        Action<IActivityProcess> func, 
        Action<IActivityProcess, Exception>? onError = null)
    {
        var process = this;

        try
        {
            func(process);
            Activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity?.AddException(ex);

            if (onError != null) onError(process, ex);
            throw;
        }
        finally
        {
           Dispose();
        }
    }

    /// <summary>
    /// Dispose the activity: set tags from the Labels dictionary and stop the activity.
    /// </summary>
    public void Dispose()
    {
        if (Activity == null) return;

        Activity.SetTagsFromDictionary(Labels, _tracesNameBuilder);
        Activity.Stop();
        Activity.Dispose();
    }

    /// <summary>
    /// Executes an asynchronous function inside the activity process, sets status and disposes the activity.
    /// </summary>
    public async Task<TResult> ExecuteAsync<TResult>(Func<IActivityProcess, Task<TResult>> func, Func<IActivityProcess, Exception, Task<TResult>>? onError = null)
    {
        var process = this;

        try
        {
            var result = await func(process);
            Activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity?.AddException(ex);

            if (onError != null) return await onError(process, ex);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    /// <summary>
    /// Executes an asynchronous action inside the activity process, sets status and disposes the activity.
    /// </summary>
    public async Task ExecuteAsync(Func<IActivityProcess, Task> func, Func<IActivityProcess, Exception, Task>? onError = null)
    {
        var process = this;

        try
        {
            await func(process);
            Activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity?.AddException(ex);

            if (onError != null) await onError(process, ex);
            throw;
        }
        finally
        {
            Dispose();
        }
    }
}