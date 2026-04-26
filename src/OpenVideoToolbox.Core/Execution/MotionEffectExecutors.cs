using System.Globalization;
using System.Text.Json;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

internal sealed class MoveEffectExecutor : IEffectExecutor
{
    public void AppendFilterGraph(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context,
        string inputLabel,
        string outputLabel,
        ICollection<string> filters)
    {
        MotionEffectSupport.AppendOverlayMotionFilterGraph(
            parameters,
            context,
            inputLabel,
            outputLabel,
            filters,
            buildPositionExpressions: progress => (
                MotionEffectSupport.InterpolateExpression(
                    MotionEffectSupport.GetRequiredString(parameters, "fromX"),
                    MotionEffectSupport.GetRequiredString(parameters, "toX"),
                    progress),
                MotionEffectSupport.InterpolateExpression(
                    MotionEffectSupport.GetRequiredString(parameters, "fromY"),
                    MotionEffectSupport.GetRequiredString(parameters, "toY"),
                    progress)));
    }
}

internal sealed class OrbitEffectExecutor : IEffectExecutor
{
    public void AppendFilterGraph(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context,
        string inputLabel,
        string outputLabel,
        ICollection<string> filters)
    {
        MotionEffectSupport.AppendOverlayMotionFilterGraph(
            parameters,
            context,
            inputLabel,
            outputLabel,
            filters,
            buildPositionExpressions: progress =>
            {
                var centerX = MotionEffectSupport.GetRequiredString(parameters, "centerX");
                var centerY = MotionEffectSupport.GetRequiredString(parameters, "centerY");
                var radiusX = MotionEffectSupport.GetRequiredDouble(parameters, "radiusX");
                var radiusY = MotionEffectSupport.GetRequiredDouble(parameters, "radiusY");
                var startAngle = MotionEffectSupport.GetOptionalDouble(parameters, "startAngleDeg", 0.0);
                var endAngle = MotionEffectSupport.GetOptionalDouble(parameters, "endAngleDeg", 360.0);
                var angleExpression = MotionEffectSupport.InterpolateExpression(
                    MotionEffectSupport.FormatNumber(startAngle),
                    MotionEffectSupport.FormatNumber(endAngle),
                    progress);
                var radiansExpression = $"(({angleExpression})*PI/180)";

                return (
                    $"(({centerX})+({MotionEffectSupport.FormatNumber(radiusX)}*cos({radiansExpression}))-(overlay_w/2))",
                    $"(({centerY})+({MotionEffectSupport.FormatNumber(radiusY)}*sin({radiansExpression}))-(overlay_h/2))");
            });
    }
}

internal sealed class ZoomPanEffectExecutor : IEffectExecutor
{
    public void AppendFilterGraph(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context,
        string inputLabel,
        string outputLabel,
        ICollection<string> filters)
    {
        var durationSeconds = MotionEffectSupport.GetRequiredDouble(parameters, "durationSeconds");
        var progress = MotionEffectSupport.BuildProgressExpression(
            durationSeconds,
            MotionEffectSupport.GetOptionalString(parameters, "easing", "linear"));
        var scaleExpression = MotionEffectSupport.InterpolateExpression(
            MotionEffectSupport.FormatNumber(MotionEffectSupport.GetRequiredDouble(parameters, "fromScale")),
            MotionEffectSupport.FormatNumber(MotionEffectSupport.GetRequiredDouble(parameters, "toScale")),
            progress);
        var positionedLabel = $"{outputLabel}_scaled";

        filters.Add(
            $"{MotionEffectSupport.Label(inputLabel)}format=rgba,scale=w='max(1,trunc(iw*({MotionEffectSupport.EscapeExpression(scaleExpression)})))':h='max(1,trunc(ih*({MotionEffectSupport.EscapeExpression(scaleExpression)})))':eval=frame{MotionEffectSupport.Label(positionedLabel)}");

        MotionEffectSupport.AppendOverlayMotionFilterGraph(
            parameters,
            context,
            positionedLabel,
            outputLabel,
            filters,
            progress,
            buildPositionExpressions: currentProgress => (
                MotionEffectSupport.InterpolateExpression(
                    MotionEffectSupport.GetRequiredString(parameters, "fromX"),
                    MotionEffectSupport.GetRequiredString(parameters, "toX"),
                    currentProgress),
                MotionEffectSupport.InterpolateExpression(
                    MotionEffectSupport.GetRequiredString(parameters, "fromY"),
                    MotionEffectSupport.GetRequiredString(parameters, "toY"),
                    currentProgress)),
            inputAlreadyFormatted: true);
    }
}

internal static class MotionEffectSupport
{
    public static void AppendOverlayMotionFilterGraph(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context,
        string inputLabel,
        string outputLabel,
        ICollection<string> filters,
        Func<string, (string X, string Y)> buildPositionExpressions)
    {
        var durationSeconds = GetRequiredDouble(parameters, "durationSeconds");
        var progress = BuildProgressExpression(durationSeconds, GetOptionalString(parameters, "easing", "linear"));
        AppendOverlayMotionFilterGraph(
            parameters,
            context,
            inputLabel,
            outputLabel,
            filters,
            progress,
            buildPositionExpressions,
            inputAlreadyFormatted: false);
    }

    public static void AppendOverlayMotionFilterGraph(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context,
        string inputLabel,
        string outputLabel,
        ICollection<string> filters,
        string progressExpression,
        Func<string, (string X, string Y)> buildPositionExpressions,
        bool inputAlreadyFormatted)
    {
        ValidateMotionContext(context);
        ArgumentNullException.ThrowIfNull(context.Timeline.Resolution);

        var duration = ResolveClipDurationSeconds(context.Clip);
        var frameRate = context.Timeline.FrameRate is > 0 ? context.Timeline.FrameRate.Value : 30;
        var sourceLabel = inputAlreadyFormatted ? inputLabel : $"{outputLabel}_src";
        var backgroundLabel = $"{outputLabel}_bg";
        var positionedLabel = $"{outputLabel}_composite";
        var (xExpression, yExpression) = buildPositionExpressions(progressExpression);

        if (!inputAlreadyFormatted)
        {
            filters.Add($"{Label(inputLabel)}format=rgba{Label(sourceLabel)}");
        }

        filters.Add(
            $"color=c=black@0.0:s={context.Timeline.Resolution.W}x{context.Timeline.Resolution.H}:r={frameRate.ToString(CultureInfo.InvariantCulture)}:d={FormatNumber(duration)}{Label(backgroundLabel)}");
        filters.Add(
            $"{Label(backgroundLabel)}{Label(sourceLabel)}overlay=x='{EscapeExpression(xExpression)}':y='{EscapeExpression(yExpression)}':eval=frame:format=auto,format=rgba{Label(positionedLabel)}");
        filters.Add($"{Label(positionedLabel)}null{Label(outputLabel)}");
    }

    public static string BuildProgressExpression(double durationSeconds, string easing)
    {
        var clampedLinear = $"min(max(t/{FormatNumber(durationSeconds)},0),1)";
        return easing.ToLowerInvariant() switch
        {
            "ease_in" => $"pow({clampedLinear},2)",
            "ease_out" => $"(1-pow(1-{clampedLinear},2))",
            "ease_in_out" => $"if(lt({clampedLinear},0.5),2*pow({clampedLinear},2),1-pow(-2*{clampedLinear}+2,2)/2)",
            _ => clampedLinear
        };
    }

    public static string InterpolateExpression(string fromExpression, string toExpression, string progressExpression)
    {
        return $"(({fromExpression})+((({toExpression})-({fromExpression}))*({progressExpression})))";
    }

    public static string GetRequiredString(IReadOnlyDictionary<string, JsonElement> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Motion effect parameter '{key}' is required and must be a string.");
        }

        return value.GetString() ?? string.Empty;
    }

    public static string GetOptionalString(IReadOnlyDictionary<string, JsonElement> parameters, string key, string defaultValue)
    {
        if (!parameters.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return defaultValue;
        }

        return value.GetString() ?? defaultValue;
    }

    public static double GetRequiredDouble(IReadOnlyDictionary<string, JsonElement> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
        {
            throw new ArgumentException($"Motion effect parameter '{key}' is required and must be numeric.");
        }

        return number;
    }

    public static double GetOptionalDouble(IReadOnlyDictionary<string, JsonElement> parameters, string key, double defaultValue)
    {
        if (!parameters.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
        {
            return defaultValue;
        }

        return number;
    }

    public static string Label(string value) => $"[{value}]";

    public static string EscapeExpression(string expression)
    {
        return expression
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);
    }

    public static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void ValidateMotionContext(EffectRenderContext context)
    {
        if (context.Track.Kind != TrackKind.Video)
        {
            throw new ArgumentException("Motion effects are only supported on video clips.");
        }

        if (context.Clip.Placeholder is not null)
        {
            throw new ArgumentException($"Motion effects are not supported on placeholder clip '{context.Clip.Id}'.");
        }

        if (string.IsNullOrWhiteSpace(context.Clip.Src))
        {
            throw new ArgumentException($"Motion effects require clip '{context.Clip.Id}' to set src explicitly in the first implementation.");
        }
    }

    private static double ResolveClipDurationSeconds(TimelineClip clip)
    {
        if (clip.Duration is { } duration)
        {
            return duration.TotalSeconds;
        }

        if (clip.InPoint is { } inPoint && clip.OutPoint is { } outPoint)
        {
            return (outPoint - inPoint).TotalSeconds;
        }

        throw new ArgumentException($"Motion effect clip '{clip.Id}' must provide duration or both in/out.");
    }
}
