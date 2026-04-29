$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $Root "../..")

function Invoke-Ovt {
    dotnet run --project (Join-Path $RepoRoot "src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj") -- @Args
}

# External pre-step, not an OVT command:
# yt-dlp -f "bestvideo[height<=1080][ext=mp4]+bestaudio[ext=m4a]/best[height<=1080]" --write-subs --write-auto-subs --sub-langs en --sub-format vtt -o "$Root/source/source.%(ext)s" "<video-url>"

Invoke-Ovt doctor --json-out (Join-Path $Root "doctor.json")
Invoke-Ovt probe (Join-Path $Root "source/source.mp4") --json-out (Join-Path $Root "signals/probe.json")

# Optional transcript path if whisper-cli and a model are configured:
# Invoke-Ovt transcribe (Join-Path $Root "source/source.mp4") --model "<whisper-model>" --output (Join-Path $Root "signals/transcript.json") --json-out (Join-Path $Root "signals/transcribe.json")
# Invoke-Ovt subtitle (Join-Path $Root "source/source.mp4") --transcript (Join-Path $Root "signals/transcript.json") --format srt --output (Join-Path $Root "signals/subtitles.srt") --json-out (Join-Path $Root "signals/subtitle.json")

# Current safe path: create a normal template task per selected chapter.
# The chapter time range is kept in signals/chapters.sample.json until a future chapter-aware command exists.
Invoke-Ovt scaffold-template (Join-Path $Root "source/source.mp4") --template shorts-captioned --dir (Join-Path $Root "tasks/chapter-02") --validate --check-files --render-output (Join-Path $Root "exports/chapter-02.mp4")
Invoke-Ovt inspect-plan --plan (Join-Path $Root "tasks/chapter-02/edit.json") --check-files --json-out (Join-Path $Root "tasks/chapter-02/inspect.json")

# If an external AI or human has produced a bilingual subtitle file, attach it:
# Invoke-Ovt attach-plan-material --plan (Join-Path $Root "tasks/chapter-02/edit.json") --subtitles --path (Join-Path $Root "tasks/chapter-02/subtitles.bilingual.srt") --check-files --json-out (Join-Path $Root "tasks/chapter-02/attach-subtitles.json")

Invoke-Ovt validate-plan --plan (Join-Path $Root "tasks/chapter-02/edit.json") --check-files --json-out (Join-Path $Root "tasks/chapter-02/validate.json")
Invoke-Ovt render --plan (Join-Path $Root "tasks/chapter-02/edit.json") --output (Join-Path $Root "exports/chapter-02.mp4") --preview --json-out (Join-Path $Root "tasks/chapter-02/render-preview.json")

# Execute only after preview is reviewed:
# Invoke-Ovt render --plan (Join-Path $Root "tasks/chapter-02/edit.json") --output (Join-Path $Root "exports/chapter-02.mp4")
