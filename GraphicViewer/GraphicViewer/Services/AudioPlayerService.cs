using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using GraphicViewer.Models;

namespace GraphicViewer.Services;

/// <summary>
/// Scoped service that owns all MP3 player state.
/// Uses blob URLs (not base64) for memory efficiency.
/// Supports shuffle and repeat modes.
/// </summary>
public class AudioPlayerService
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly IJSRuntime _js;

    public AudioPlayerService(IJSRuntime js)
    {
        _js = js;
    }

    // ── State ────────────────────────────────────────────────────────────────
    public IReadOnlyList<AudioFileModel> Files => _files;
    public AudioFileModel? CurrentFile => _files.Count > 0 ? _files[CurrentIndex] : null;
    public int CurrentIndex { get; private set; } = 0;
    public bool IsPlaying { get; set; } = false;
    public bool IsShuffle { get; private set; } = false;
    public bool IsRepeat { get; private set; } = false;
    public double CurrentTime { get; private set; } = 0;
    public double Duration { get; private set; } = 0;

    public bool HasFiles => _files.Count > 0;
    public bool CanGoPrev => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < _files.Count - 1;
    public string FileCounter => HasFiles ? $"{CurrentIndex + 1} / {_files.Count}" : "—";

    public double ProgressPercent =>
        Duration > 0 ? (CurrentTime / Duration) * 100 : 0;

    public string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action? OnStateChanged;

    // ── Private ──────────────────────────────────────────────────────────────
    private readonly List<AudioFileModel> _files = [];
    private readonly Random _random = new();
    private const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500 MB

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a", ".opus" };

    // ── File Loading ─────────────────────────────────────────────────────────
    public async Task LoadFilesAsync(IEnumerable<IBrowserFile> browserFiles)
    {
        await RevokeAllUrlsAsync();
        _files.Clear();

        var audioFiles = browserFiles
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f.Name)))
            .OrderBy(f => f.Name)
            .ToList();

        for (int i = 0; i < audioFiles.Count; i++)
        {
            var file = audioFiles[i];
            try
            {
                var mimeType = AudioFileModel.GetMimeType(file.Name);

                using var stream = file.OpenReadStream(MaxFileSizeBytes);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                var objectUrl = await _js.InvokeAsync<string>(
                    "GraphicViewer.createBlobUrl", ms.ToArray(), mimeType);

                _files.Add(new AudioFileModel
                {
                    Index     = i,
                    FileName  = file.Name,
                    ObjectUrl = objectUrl,
                    MimeType  = mimeType,
                    SizeBytes = file.Size
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[AudioPlayer] Failed to load {file.Name}: {ex.Message}");
            }
        }

        CurrentIndex = 0;
        IsPlaying    = false;
        CurrentTime  = 0;
        Duration     = 0;
        NotifyStateChanged();
    }

    // ── Navigation ───────────────────────────────────────────────────────────
    public void SelectFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        CurrentIndex = index;
        IsPlaying    = false;
        CurrentTime  = 0;
        Duration     = 0;
        NotifyStateChanged();
    }

    public void NavigatePrev()
    {
        if (IsShuffle)
            SelectFile(_random.Next(_files.Count));
        else if (CanGoPrev)
            SelectFile(CurrentIndex - 1);
    }

    public void NavigateNext()
    {
        if (IsShuffle)
            SelectFile(_random.Next(_files.Count));
        else if (IsRepeat)
            SelectFile(CurrentIndex);
        else if (CanGoNext)
            SelectFile(CurrentIndex + 1);
    }

    // ── Playback Controls ────────────────────────────────────────────────────
    public void SetPlaying(bool playing)
    {
        IsPlaying = playing;
        NotifyStateChanged();
    }

    public void ToggleShuffle()
    {
        IsShuffle = !IsShuffle;
        NotifyStateChanged();
    }

    public void ToggleRepeat()
    {
        IsRepeat = !IsRepeat;
        NotifyStateChanged();
    }

    public void UpdateTime(double currentTime, double duration)
    {
        CurrentTime = currentTime;
        Duration    = duration;
        NotifyStateChanged();
    }

    public void HandleTrackEnded()
    {
        if (IsRepeat)
            SelectFile(CurrentIndex);
        else if (IsShuffle)
            SelectFile(_random.Next(_files.Count));
        else if (CanGoNext)
            SelectFile(CurrentIndex + 1);
        else
            SetPlaying(false);
    }

    // ── Clipboard ────────────────────────────────────────────────────────────
    public async Task<bool> CopyTrackNameAsync()
    {
        if (CurrentFile is null) return false;
        return await _js.InvokeAsync<bool>(
            "GraphicViewer.copyToClipboard", CurrentFile.Title);
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────
    private async Task RevokeAllUrlsAsync()
    {
        foreach (var file in _files.Where(f => !string.IsNullOrEmpty(f.ObjectUrl)))
        {
            try
            {
                await _js.InvokeVoidAsync(
                    "GraphicViewer.revokeBlobUrl", file.ObjectUrl);
            }
            catch { }
        }
    }

    public async ValueTask DisposeAsync() => await RevokeAllUrlsAsync();

    // ── Internal ─────────────────────────────────────────────────────────────
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
