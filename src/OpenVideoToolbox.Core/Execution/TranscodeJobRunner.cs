using OpenVideoToolbox.Core.Jobs;

namespace OpenVideoToolbox.Core.Execution;

public sealed class TranscodeJobRunner
{
    private readonly FfmpegCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public TranscodeJobRunner(FfmpegCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        JobDefinition job,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var plan = _commandBuilder.Build(job, executablePath);
        var result = await _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout,
                ProducedPaths = [FfmpegCommandBuilder.BuildOutputPath(job.Output)]
            },
            cancellationToken).ConfigureAwait(false);

        if (result.Status != ExecutionStatus.Succeeded
            || !string.Equals(job.SubtitleMode, "sidecar", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(job.SubtitlePath))
        {
            return result;
        }

        var sidecarPath = Path.Combine(job.Output.OutputDirectory, $"{job.Output.FileNameStem}{Path.GetExtension(job.SubtitlePath)}");
        try
        {
            File.Copy(job.SubtitlePath, sidecarPath, overwrite: job.Output.OverwriteExisting);
            return result with { ProducedPaths = result.ProducedPaths.Append(sidecarPath).ToArray() };
        }
        catch (Exception ex)
        {
            return result with
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = $"Subtitle sidecar copy failed: {ex.Message}"
            };
        }
    }
}

