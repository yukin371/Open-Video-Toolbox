using OpenVideoToolbox.Core.Subtitles;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class SubtitleWorkflowTests
{
    [Fact]
    public void Parse_VttAndRenderSrt_PreservesCueTextAndTiming()
    {
        var document = SubtitleDocumentParser.Parse("WEBVTT\n\n00:00:01.000 --> 00:00:03.500\nHello\n", "vtt");

        var output = SubtitleDocumentRenderer.RenderSrt(document);

        Assert.Single(document.Cues);
        Assert.Contains("00:00:01,000 --> 00:00:03,500", output);
        Assert.Contains("Hello", output);
    }

    [Fact]
    public void Clip_OverlappingCuesAreTrimmedAndRebased()
    {
        var document = new SubtitleDocument
        {
            Cues =
            [
                new SubtitleCue { Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(4), Text = "first" },
                new SubtitleCue { Start = TimeSpan.FromSeconds(5), End = TimeSpan.FromSeconds(7), Text = "second" }
            ]
        };

        var clipped = SubtitleWorkflow.Clip(new SubtitleClipRequest
        {
            Document = document,
            Start = TimeSpan.FromSeconds(2),
            End = TimeSpan.FromSeconds(6)
        });

        Assert.Equal(2, clipped.Cues.Count);
        Assert.Equal(TimeSpan.Zero, clipped.Cues[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(2), clipped.Cues[0].End);
        Assert.Equal(TimeSpan.FromSeconds(3), clipped.Cues[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(4), clipped.Cues[1].End);
    }

    [Fact]
    public void MergeBilingual_RequiresAlignedCueCountAndTiming()
    {
        var primary = new SubtitleDocument { Cues = [new SubtitleCue { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(1), Text = "Hello" }] };
        var secondary = new SubtitleDocument { Cues = [new SubtitleCue { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(1), Text = "你好" }] };

        var merged = SubtitleWorkflow.MergeBilingual(primary, secondary);

        Assert.Equal("Hello\n你好", merged.Cues[0].Text);
    }
}
