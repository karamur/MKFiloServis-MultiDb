using KOAFiloServis.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace KOAFiloServis.Web.Services;

// ═══════════════════════════════════════════════════════════════════
// Interface — test edilebilir, mock'lanabilir
// ═══════════════════════════════════════════════════════════════════

public interface IPuantajRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
        string operationContext, CancellationToken ct);
}

// ═══════════════════════════════════════════════════════════════════
// Config-driven, scoped, OTel-compatible implementation
// ═══════════════════════════════════════════════════════════════════

public sealed class PuantajRetryPolicy : IPuantajRetryPolicy, IDisposable
{
    private readonly ResiliencePipeline _pipeline;
    private readonly PuantajJobOptions _options;
    private readonly ILogger<PuantajRetryPolicy> _logger;

    public PuantajRetryPolicy(
        IOptions<PuantajJobOptions> options,
        ILogger<PuantajRetryPolicy> logger)
    {
        _options = options.Value;
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.RetryCount,
                Delay = TimeSpan.FromMilliseconds(_options.RetryBaseDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<PuantajInfrastructureException>(ex => ex.IsTransientFailure)
                    .Handle<NpgsqlException>()
                    .Handle<TimeoutException>()
                    .Handle<PostgresException>(ex => ex.IsTransient),
                OnRetry = args =>
                {
                    var ex = args.Outcome.Exception;
                    _logger.LogWarning(ex,
                        "Retry {Attempt}/{Max} delay={DelayMs}ms: {ErrorType} — {Error}",
                        args.AttemptNumber + 1,
                        _options.RetryCount,
                        args.RetryDelay.TotalMilliseconds,
                        ex?.GetType().Name,
                        ex?.Message);

                    // OTel: retry event
                    var activity = System.Diagnostics.Activity.Current;
                    activity?.AddEvent(new System.Diagnostics.ActivityEvent(
                        "polly.retry",
                        tags: new System.Diagnostics.ActivityTagsCollection
                        {
                            ["retry.attempt"] = args.AttemptNumber + 1,
                            ["retry.delay_ms"] = args.RetryDelay.TotalMilliseconds,
                            ["error.type"] = ex?.GetType().Name ?? "unknown"
                        }));

                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_options.EngineTimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogError(
                        "Engine timeout ({Timeout}s) — {Context}",
                        _options.EngineTimeoutSeconds,
                        args.Context.OperationKey);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        string operationContext, CancellationToken ct)
    {
        using var activity = System.Diagnostics.Activity.Current?.Source
            .StartActivity("puantaj.retry.execute");

        activity?.SetTag("retry.context", operationContext);
        activity?.SetTag("retry.max_attempts", _options.RetryCount);
        activity?.SetTag("retry.timeout_s", _options.EngineTimeoutSeconds);

        try
        {
            var result = await _pipeline.ExecuteAsync(
                async _ => await action(ct), ct);
            activity?.SetTag("retry.outcome", "success");
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("retry.outcome", "exhausted");
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }

    public void Dispose() { } // ResiliencePipeline managed by GC
}

// ═══════════════════════════════════════════════════════════════════
// Config
// ═══════════════════════════════════════════════════════════════════

public sealed class PuantajJobOptions
{
    public const string Section = "PuantajEngine:AutoProcess";

    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = "0 30 0 1 * ?";
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 1000;
    public int EngineTimeoutSeconds { get; set; } = 300;
    public int StaleTimeoutMinutes { get; set; } = 15;
}
