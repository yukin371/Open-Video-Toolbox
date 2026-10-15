using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegFrameExtractCommandBuilderTests
{
    [Fact]
    public void Build_CreatesOneInputPerRequestedFrameAndProducedPathsAreStable()
    {
        var request = new FrameExtractRequest
        {
            InputPath = "input.mp4",
            Points =
            [
                new FrameExtractPoint { At = TimeSpan.FromSeconds(1.5), OutputPath = Path.Combine("out", "a.png") },
                new FrameExtractPoint { At = TimeSpan.FromSeconds(4), OutputPath = Path.Combine("out", "b.png") }
            ],
            OverwriteExisting = false
        };

        var plan = new FfmpegFrameExtractCommandBuilder().Build(request);

        Assert.Equal(["-n", "-ss", "00:00:01.500", "-i", "input.mp4", "-frames:v", "1", "-an", Path.Combine("out", "a.png"), "-ss", "00:00:04.000", "-i", "input.mp4", "-frames:v", "1", "-an", Path.Combine("out", "b.png")], plan.Arguments);
    }

    [Fact]
    public void Build_RejectsOutputThatWouldOverwriteInput()
    {
        var inputPath = Path.GetFullPath("input.mp4");
        var request = new FrameExtractRequest
        {
            InputPath = inputPath,
            Points = [new FrameExtractPoint { At = TimeSpan.Zero, OutputPath = inputPath }]
        };

        Assert.Throws<ArgumentException>(() => new FfmpegFrameExtractCommandBuilder().Build(request));
    }
}
