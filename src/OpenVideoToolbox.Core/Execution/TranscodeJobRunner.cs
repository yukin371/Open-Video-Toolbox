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

        var finalOutputPath = FfmpegCommandBuilder.BuildOutputPath(job.Output);
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var finalSidecarPath = BuildSidecarPath(job);
        var finalPaths = finalSidecarPath is null
            ? new[] { finalOutputPath }
            : new[] { finalOutputPath, finalSidecarPath };

        var finalCommandPlan = _commandBuilder.Build(job, executablePath);
        if (finalPaths.Any(path => PathsEqual(path, job.Source.InputPath, pathComparison)))
        {
            return BuildFailure(finalCommandPlan, "Output path must differ from the source path.");
        }

        if (!job.Output.OverwriteExisting && finalPaths.Any(File.Exists))
        {
            return BuildFailure(finalCommandPlan, "An output file already exists; specify --overwrite to replace it.");
        }

        var temporaryOutputPath = BuildTemporaryOutputPath(finalOutputPath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(temporaryOutputPath)!);
        }
        catch (Exception ex)
        {
            return BuildFailure(finalCommandPlan, $"Could not prepare output directory: {ex.Message}");
        }

        var temporaryJob = job with
        {
            Output = job.Output with
            {
                FileNameStem = Path.GetFileNameWithoutExtension(temporaryOutputPath),
                OutputDirectory = Path.GetDirectoryName(temporaryOutputPath)!
            }
        };
        var plan = _commandBuilder.Build(temporaryJob, executablePath);

        ExecutionResult result;
        try
        {
            result = await _processRunner.ExecuteAsync(
                new ProcessExecutionRequest
                {
                    CommandPlan = plan,
                    Timeout = timeout,
                    ProducedPaths = [temporaryOutputPath]
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return BuildFailure(plan, AppendCleanupError(ex.Message, TryDelete(temporaryOutputPath)));
        }

        if (result.Status != ExecutionStatus.Succeeded)
        {
            var cleanupError = TryDelete(temporaryOutputPath);
            return result with
            {
                ErrorMessage = AppendCleanupError(result.ErrorMessage, cleanupError),
                ProducedPaths = []
            };
        }

        if (!File.Exists(temporaryOutputPath))
        {
            return result with
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = $"Transcode completed without producing '{temporaryOutputPath}'.",
                ProducedPaths = []
            };
        }

        OutputSizeComparison sizeComparison;
        try
        {
            var inputBytes = new FileInfo(job.Source.InputPath).Length;
            var outputBytes = new FileInfo(temporaryOutputPath).Length;
            sizeComparison = BuildSizeComparison(job.Source.InputPath, finalOutputPath, inputBytes, outputBytes);
        }
        catch (Exception ex)
        {
            var cleanupError = TryDelete(temporaryOutputPath);
            return result with
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = AppendCleanupError($"Could not compare source and output sizes: {ex.Message}", cleanupError),
                ProducedPaths = []
            };
        }

        if (!job.Output.AllowLargerOutput && sizeComparison.OutputBytes > sizeComparison.InputBytes)
        {
            var cleanupError = TryDelete(temporaryOutputPath);
            return result with
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = AppendCleanupError(
                    $"Output is larger than input ({sizeComparison.OutputBytes} > {sizeComparison.InputBytes} bytes); final output was not committed. Use --allow-larger-output to keep it.",
                    cleanupError),
                ProducedPaths = [],
                OutputSizeComparison = sizeComparison,
                Diagnostics = [new ExecutionDiagnostic
                {
                    Code = "transcode.output.larger.rejected",
                    Severity = "warning",
                    Message = "The larger output was rejected. The source and any previous output were preserved.",
                    Path = finalOutputPath
                }]
            };
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return result with
            {
                Status = ExecutionStatus.Cancelled,
                ErrorMessage = AppendCleanupError("Transcode output commit was cancelled.", TryDelete(temporaryOutputPath)),
                ProducedPaths = [],
                OutputSizeComparison = sizeComparison
            };
        }

        var stagedOutputs = new List<(string TemporaryPath, string FinalPath)>
        {
            (temporaryOutputPath, finalOutputPath)
        };

        try
        {
            var stagedSidecar = StageSidecar(job, finalSidecarPath);
            if (stagedSidecar is not null)
            {
                stagedOutputs.Add(stagedSidecar.Value);
            }

            var diagnostics = CommitOutputs(stagedOutputs, job.Output.OverwriteExisting).ToList();
            if (sizeComparison.IsLarger)
            {
                diagnostics.Add(new ExecutionDiagnostic
                {
                    Code = "transcode.output.larger.allowed",
                    Severity = "warning",
                    Message = "The larger output was committed because AllowLargerOutput was explicitly enabled.",
                    Path = finalOutputPath
                });
            }
            return result with
            {
                ProducedPaths = finalPaths,
                OutputSizeComparison = sizeComparison,
                Diagnostics = diagnostics.Count == 0 ? null : diagnostics
            };
        }
        catch (Exception ex)
        {
            var cleanupErrors = stagedOutputs
                .Select(output => TryDelete(output.TemporaryPath))
                .Where(error => error is not null)
                .ToArray();
            var message = $"Transcode output commit failed: {ex.Message}";
            if (cleanupErrors.Length > 0)
            {
                message += $" Temporary output cleanup failed: {string.Join("; ", cleanupErrors)}";
            }

            return result with
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = message,
                ProducedPaths = [],
                OutputSizeComparison = sizeComparison
            };
        }
    }

    private static ExecutionResult BuildFailure(CommandPlan commandPlan, string message)
    {
        var now = DateTimeOffset.UtcNow;
        return new ExecutionResult
        {
            Status = ExecutionStatus.Failed,
            StartedAtUtc = now,
            FinishedAtUtc = now,
            Duration = TimeSpan.Zero,
            CommandPlan = commandPlan,
            ErrorMessage = message,
            ProducedPaths = []
        };
    }

    private static string BuildTemporaryOutputPath(string finalOutputPath)
    {
        var directory = Path.GetDirectoryName(finalOutputPath)
            ?? throw new InvalidOperationException("Output path must have a directory.");
        var extension = Path.GetExtension(finalOutputPath);
        return Path.Combine(directory, $".{Path.GetFileNameWithoutExtension(finalOutputPath)}.{Guid.NewGuid():N}.tmp{extension}");
    }

    private static string? BuildSidecarPath(JobDefinition job)
    {
        if (!string.Equals(job.SubtitleMode, "sidecar", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(job.SubtitlePath))
        {
            return null;
        }

        return Path.Combine(
            job.Output.OutputDirectory,
            $"{job.Output.FileNameStem}{Path.GetExtension(job.SubtitlePath)}");
    }

    private static (string TemporaryPath, string FinalPath)? StageSidecar(JobDefinition job, string? finalSidecarPath)
    {
        if (finalSidecarPath is null || string.IsNullOrWhiteSpace(job.SubtitlePath))
        {
            return null;
        }

        var temporarySidecarPath = BuildTemporaryOutputPath(finalSidecarPath);
        try
        {
            File.Copy(job.SubtitlePath, temporarySidecarPath, overwrite: false);
            return (temporarySidecarPath, finalSidecarPath);
        }
        catch
        {
            TryDelete(temporarySidecarPath);
            throw;
        }
    }

    private static IReadOnlyList<ExecutionDiagnostic> CommitOutputs(
        IReadOnlyList<(string TemporaryPath, string FinalPath)> outputs,
        bool overwrite)
    {
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        if (outputs.Select(output => Path.GetFullPath(output.FinalPath)).Distinct(pathComparer).Count() != outputs.Count)
        {
            throw new InvalidOperationException("Output paths must be unique.");
        }

        foreach (var output in outputs)
        {
            if (!overwrite && File.Exists(output.FinalPath))
            {
                throw new IOException($"Output file already exists: {output.FinalPath}");
            }
        }

        var backups = new List<(string BackupPath, string FinalPath)>();
        var committedPaths = new List<string>();
        try
        {
            if (overwrite)
            {
                foreach (var output in outputs)
                {
                    if (!File.Exists(output.FinalPath))
                    {
                        continue;
                    }

                    var backupPath = BuildTemporaryOutputPath(output.FinalPath);
                    File.Move(output.FinalPath, backupPath);
                    backups.Add((backupPath, output.FinalPath));
                }
            }

            foreach (var output in outputs)
            {
                File.Move(output.TemporaryPath, output.FinalPath);
                committedPaths.Add(output.FinalPath);
            }
        }
        catch (Exception commitError)
        {
            var rollbackErrors = new List<string>();
            foreach (var committedPath in committedPaths.AsEnumerable().Reverse())
            {
                var deleteError = TryDelete(committedPath);
                if (deleteError is not null)
                {
                    rollbackErrors.Add($"Could not remove '{committedPath}': {deleteError}");
                }
            }

            foreach (var backup in backups.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Move(backup.BackupPath, backup.FinalPath);
                    }
                }
                catch (Exception rollbackError)
                {
                    rollbackErrors.Add($"Could not restore '{backup.FinalPath}' from '{backup.BackupPath}': {rollbackError.Message}");
                }
            }

            var rollbackDetails = rollbackErrors.Count == 0
                ? string.Empty
                : $" Rollback details: {string.Join("; ", rollbackErrors)}";
            throw new IOException($"Output commit failed: {commitError.Message}.{rollbackDetails}", commitError);
        }

        var diagnostics = new List<ExecutionDiagnostic>();
        foreach (var backup in backups)
        {
            var cleanupError = TryDelete(backup.BackupPath);
            if (cleanupError is not null)
            {
                diagnostics.Add(new ExecutionDiagnostic
                {
                    Code = "transcode.backup.cleanup.failed",
                    Severity = "warning",
                    Message = $"Output committed but the previous output backup could not be removed: {cleanupError}",
                    Path = backup.BackupPath
                });
            }
        }
        return diagnostics;
    }

    private static OutputSizeComparison BuildSizeComparison(string inputPath, string outputPath, long inputBytes, long outputBytes)
    {
        var deltaBytes = outputBytes - inputBytes;
        return new OutputSizeComparison
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            InputBytes = inputBytes,
            OutputBytes = outputBytes,
            DeltaBytes = deltaBytes,
            OutputToInputRatio = inputBytes == 0 ? null : Math.Round((double)outputBytes / inputBytes, 4),
            ChangePercent = inputBytes == 0 ? null : Math.Round((double)deltaBytes / inputBytes * 100, 2),
            IsLarger = outputBytes > inputBytes
        };
    }

    private static string? TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string AppendCleanupError(string? message, string? cleanupError) => cleanupError is null
        ? message ?? string.Empty
        : $"{message ?? "Execution failed."} Temporary output cleanup failed: {cleanupError}";

    private static bool PathsEqual(string first, string second, StringComparison comparison) =>
        string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), comparison);
}

