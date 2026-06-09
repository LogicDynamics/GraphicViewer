using Microsoft.JSInterop;
using GraphicViewer.Models;
using System.Text.Json;

namespace GraphicViewer.Services;

/// <summary>
/// Scoped service that owns all YouTube player state.
/// Persists playlist to localStorage via JSInterop.
/// </summary>
public class YouTubePlayerService
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly IJSRuntime _js;

    public YouTubePlayerService(IJSRuntime js)
    {
        _js = js;
    }

    // ── State ────────────────────────────────────────────────────────────────
    public IReadOnlyList<YouTubeVideoModel> Playlist => _playlist;
    public YouTubeVideoModel? CurrentVideo =>
        _playlist.Count > 0 ? _playlist[CurrentIndex] : null;
    public int CurrentIndex { get; private set; } = 0;

    public bool HasVideos => _playlist.Count > 0;
    public bool CanGoPrev => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < _playlist.Count - 1;
    public string VideoCounter =>
        HasVideos ? $"{CurrentIndex + 1} / {_playlist.Count}" : "—";

    // ── Input state ──────────────────────────────────────────────────────────
    public string UrlInput { get; set; } = string.Empty;
    public string TitleInput { get; set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action? OnStateChanged;

    // ── Private ──────────────────────────────────────────────────────────────
    private readonly List<YouTubeVideoModel> _playlist = [];
    private const string StorageKey = "gv_youtube_playlist";

    // ── Playlist Management ──────────────────────────────────────────────────
    public async Task InitAsync()
    {
        await LoadPlaylistFromStorageAsync();
    }

    public async Task AddVideoAsync()
    {
        ErrorMessage = string.Empty;

        var videoId = YouTubeVideoModel.ExtractVideoId(UrlInput);
        if (string.IsNullOrEmpty(videoId))
        {
            ErrorMessage = "Invalid YouTube URL. Please paste a valid YouTube link.";
            NotifyStateChanged();
            return;
        }

        // Check for duplicates
        if (_playlist.Any(v => v.VideoId == videoId))
        {
            ErrorMessage = "This video is already in your playlist.";
            NotifyStateChanged();
            return;
        }

        var video = new YouTubeVideoModel
        {
            Index    = _playlist.Count,
            Url      = UrlInput.Trim(),
            VideoId  = videoId,
            Title    = string.IsNullOrWhiteSpace(TitleInput)
                           ? $"Video {_playlist.Count + 1}"
                           : TitleInput.Trim(),
            AddedOn  = DateTime.Now.ToString("MMM dd, yyyy")
        };

        _playlist.Add(video);
        RebuildIndexes();

        // Auto-select if first video
        if (_playlist.Count == 1) CurrentIndex = 0;

        UrlInput   = string.Empty;
        TitleInput = string.Empty;

        await SavePlaylistToStorageAsync();
        NotifyStateChanged();
    }

    public async Task RemoveVideoAsync(int index)
    {
        if (index < 0 || index >= _playlist.Count) return;
        _playlist.RemoveAt(index);
        RebuildIndexes();

        if (CurrentIndex >= _playlist.Count)
            CurrentIndex = Math.Max(0, _playlist.Count - 1);

        await SavePlaylistToStorageAsync();
        NotifyStateChanged();
    }

    public async Task ClearPlaylistAsync()
    {
        _playlist.Clear();
        CurrentIndex = 0;
        await SavePlaylistToStorageAsync();
        NotifyStateChanged();
    }

    public void SelectVideo(int index)
    {
        if (index < 0 || index >= _playlist.Count) return;
        CurrentIndex = index;
        NotifyStateChanged();
    }

    public void NavigatePrev() { if (CanGoPrev) SelectVideo(CurrentIndex - 1); }
    public void NavigateNext() { if (CanGoNext) SelectVideo(CurrentIndex + 1); }

    // ── Persistence ──────────────────────────────────────────────────────────
    private async Task SavePlaylistToStorageAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_playlist);
            await _js.InvokeVoidAsync(
                "GraphicViewer.setLocalStorage", StorageKey, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YouTube] Save failed: {ex.Message}");
        }
    }

    private async Task LoadPlaylistFromStorageAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>(
                "GraphicViewer.getLocalStorage", StorageKey);

            if (!string.IsNullOrEmpty(json))
            {
                var items = JsonSerializer.Deserialize<List<YouTubeVideoModel>>(json);
                if (items != null)
                {
                    _playlist.Clear();
                    _playlist.AddRange(items);
                    RebuildIndexes();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YouTube] Load failed: {ex.Message}");
        }

        NotifyStateChanged();
    }

    // ── Clipboard ────────────────────────────────────────────────────────────
    public async Task<bool> CopyCurrentUrlAsync()
    {
        if (CurrentVideo is null) return false;
        return await _js.InvokeAsync<bool>(
            "GraphicViewer.copyToClipboard", CurrentVideo.Url);
    }

    // ── Internal ─────────────────────────────────────────────────────────────
    private void RebuildIndexes()
    {
        for (int i = 0; i < _playlist.Count; i++)
            _playlist[i].Index = i;
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
