namespace OpenVideoToolbox.Core.Execution;

public sealed class FrameExtractRunner
{
    private readonly FfmpegFrameExtractCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public FrameExtractRunner(FfmpegFrameExtractCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public Task<ExecutionResult> RunAsync(
        FrameExtractRequest request,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = _commandBuilder.Build(request, executablePath);
        return _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout,
                ProducedPaths = request.Points.Select(point => point.OutputPath).ToArray()
            },
            cancellationToken);
    }
}
