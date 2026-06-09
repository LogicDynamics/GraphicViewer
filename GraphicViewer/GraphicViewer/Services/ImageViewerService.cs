using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using GraphicViewer.Models;

namespace GraphicViewer.Services;

/// <summary>
/// Scoped service that owns all PNG/JPEG viewer state.
/// Mirrors SvgViewerService pattern exactly.
/// </summary>
public class ImageViewerService
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly IJSRuntime _js;

    public ImageViewerService(IJSRuntime js)
    {
        _js = js;
    }

    // ── State ────────────────────────────────────────────────────────────────
    public IReadOnlyList<ImageFileModel> Files => _files;
    public ImageFileModel? CurrentFile => _files.Count > 0 ? _files[CurrentIndex] : null;
    public int CurrentIndex { get; private set; } = 0;
    public double ZoomLevel { get; private set; } = 1.0;

    public bool HasFiles => _files.Count > 0;
    public bool CanGoPrev => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < _files.Count - 1;
    public string FileCounter => HasFiles ? $"{CurrentIndex + 1} / {_files.Count}" : "—";
    public string ZoomLabel => $"{(int)(ZoomLevel * 100)}%";

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action? OnStateChanged;

    // ── Private ──────────────────────────────────────────────────────────────
    private readonly List<ImageFileModel> _files = [];

    // Max 10 MB per image
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".ico"
    };

    // ── File Loading ─────────────────────────────────────────────────────────
    public async Task LoadFilesAsync(IEnumerable<IBrowserFile> browserFiles)
    {
        _files.Clear();

        var imageFiles = browserFiles
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f.Name)))
            .OrderBy(f => f.Name)
            .ToList();

        for (int i = 0; i < imageFiles.Count; i++)
        {
            var file = imageFiles[i];
            try
            {
                var mimeType = ImageFileModel.GetMimeType(file.Name);

                using var stream = file.OpenReadStream(MaxFileSizeBytes);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                var dataUrl = $"data:{mimeType};base64,{base64}";

                _files.Add(new ImageFileModel
                {
                    Index = i,
                    FileName = file.Name,
                    DataUrl = dataUrl,
                    MimeType = mimeType,
                    SizeBytes = file.Size
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImageViewer] Failed to load {file.Name}: {ex.Message}");
            }
        }

        CurrentIndex = 0;
        ZoomLevel = 1.0;
        NotifyStateChanged();
    }

    // ── Navigation ───────────────────────────────────────────────────────────
    public void SelectFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        CurrentIndex = index;
        ZoomLevel = 1.0;
        NotifyStateChanged();
    }

    public void NavigatePrev() { if (CanGoPrev) SelectFile(CurrentIndex - 1); }
    public void NavigateNext() { if (CanGoNext) SelectFile(CurrentIndex + 1); }

    // ── Zoom ─────────────────────────────────────────────────────────────────
    public void ZoomIn() => SetZoom(ZoomLevel + 0.25);
    public void ZoomOut() => SetZoom(ZoomLevel - 0.25);
    public void ResetZoom() => SetZoom(1.0);

    private void SetZoom(double value)
    {
        ZoomLevel = Math.Clamp(value, 0.25, 4.0);
        NotifyStateChanged();
    }

    // ── Clipboard ────────────────────────────────────────────────────────────
    /// <summary>Copy the current image filename to clipboard.</summary>
    public async Task<bool> CopyFileNameAsync()
    {
        if (CurrentFile is null) return false;
        return await _js.InvokeAsync<bool>(
            "GraphicViewer.copyToClipboard", CurrentFile.FileName);
    }

    // ── Internal ─────────────────────────────────────────────────────────────
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
