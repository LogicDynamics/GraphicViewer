using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using GraphicViewer.Models;

namespace GraphicViewer.Services;

/// <summary>
/// Scoped service that owns all MP4 player state.
/// Uses blob URLs (not base64) for memory efficiency with large video files.
/// Always revokes old blob URLs before replacing them.
/// </summary>
public class VideoPlayerService
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly IJSRuntime _js;

    public VideoPlayerService(IJSRuntime js)
    {
        _js = js;
    }

    // ── State ────────────────────────────────────────────────────────────────
    public IReadOnlyList<VideoFileModel> Files => _files;
    public VideoFileModel? CurrentFile => _files.Count > 0 ? _files[CurrentIndex] : null;
    public int CurrentIndex { get; private set; } = 0;
    public bool IsPlaying { get; set; } = false;

    public bool HasFiles => _files.Count > 0;
    public bool CanGoPrev => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < _files.Count - 1;
    public string FileCounter => HasFiles ? $"{CurrentIndex + 1} / {_files.Count}" : "—";

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action? OnStateChanged;

    // ── Private ──────────────────────────────────────────────────────────────
    private readonly List<VideoFileModel> _files = [];

    // 2 GB max per video
    private const long MaxFileSizeBytes = 2L * 1024 * 1024 * 1024;

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".webm", ".ogv", ".mov", ".avi" };

    // ── File Loading ─────────────────────────────────────────────────────────
    public async Task LoadFilesAsync(IEnumerable<IBrowserFile> browserFiles)
    {
        // Revoke all existing blob URLs to free memory
        await RevokeAllUrlsAsync();
        _files.Clear();

        var videoFiles = browserFiles
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f.Name)))
            .OrderBy(f => f.Name)
            .ToList();

        for (int i = 0; i < videoFiles.Count; i++)
        {
            var file = videoFiles[i];
            try
            {
                var mimeType = VideoFileModel.GetMimeType(file.Name);

                // Read file bytes then create blob URL via JS
                using var stream = file.OpenReadStream(MaxFileSizeBytes);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes = ms.ToArray();

                // Create blob URL in the browser
                var objectUrl = await _js.InvokeAsync<string>(
                    "GraphicViewer.createBlobUrl", bytes, mimeType);

                _files.Add(new VideoFileModel
                {
                    Index    = i,
                    FileName = file.Name,
                    ObjectUrl = objectUrl,
                    MimeType = mimeType,
                    SizeBytes = file.Size
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[VideoPlayer] Failed to load {file.Name}: {ex.Message}");
            }
        }

        CurrentIndex = 0;
        IsPlaying    = false;
        NotifyStateChanged();
    }

    // ── Navigation ───────────────────────────────────────────────────────────
    public void SelectFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        CurrentIndex = index;
        IsPlaying    = false;
        NotifyStateChanged();
    }

    public void NavigatePrev() { if (CanGoPrev) SelectFile(CurrentIndex - 1); }
    public void NavigateNext() { if (CanGoNext) SelectFile(CurrentIndex + 1); }

    // ── Playback ─────────────────────────────────────────────────────────────
    public void SetPlaying(bool playing)
    {
        IsPlaying = playing;
        NotifyStateChanged();
    }

    // ── Clipboard ────────────────────────────────────────────────────────────
    public async Task<bool> CopyFileNameAsync()
    {
        if (CurrentFile is null) return false;
        return await _js.InvokeAsync<bool>(
            "GraphicViewer.copyToClipboard", CurrentFile.FileName);
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
            catch { /* ignore — page may be unloading */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await RevokeAllUrlsAsync();
    }

    // ── Internal ─────────────────────────────────────────────────────────────
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
