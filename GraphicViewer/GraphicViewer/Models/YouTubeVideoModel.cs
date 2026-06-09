namespace GraphicViewer.Models;

/// <summary>
/// Represents a saved YouTube video in the playlist.
/// </summary>
public class YouTubeVideoModel
{
    public int Index { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AddedOn { get; set; } = string.Empty;

    /// <summary>Extracted YouTube video ID from any YouTube URL format.</summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>Embed URL for the IFrame API.</summary>
    public string EmbedUrl =>
        string.IsNullOrEmpty(VideoId)
            ? string.Empty
            : $"https://www.youtube.com/embed/{VideoId}?autoplay=1&rel=0";

    /// <summary>Thumbnail URL from YouTube.</summary>
    public string ThumbnailUrl =>
        string.IsNullOrEmpty(VideoId)
            ? string.Empty
            : $"https://img.youtube.com/vi/{VideoId}/mqdefault.jpg";

    /// <summary>
    /// Extract video ID from various YouTube URL formats:
    /// - https://www.youtube.com/watch?v=VIDEO_ID
    /// - https://youtu.be/VIDEO_ID
    /// - https://www.youtube.com/embed/VIDEO_ID
    /// - https://www.youtube.com/shorts/VIDEO_ID
    /// </summary>
    public static string ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        url = url.Trim();

        // youtu.be/VIDEO_ID
        if (url.Contains("youtu.be/"))
        {
            var id = url.Split("youtu.be/")[1].Split('?')[0].Split('&')[0];
            return id.Length == 11 ? id : string.Empty;
        }

        // youtube.com/watch?v=VIDEO_ID
        if (url.Contains("v="))
        {
            var id = url.Split("v=")[1].Split('&')[0];
            return id.Length == 11 ? id : string.Empty;
        }

        // youtube.com/embed/VIDEO_ID or youtube.com/shorts/VIDEO_ID
        foreach (var segment in new[] { "/embed/", "/shorts/" })
        {
            if (url.Contains(segment))
            {
                var id = url.Split(segment)[1].Split('?')[0].Split('/')[0];
                return id.Length == 11 ? id : string.Empty;
            }
        }

        // Raw 11-character video ID
        if (url.Length == 11 && !url.Contains('/') && !url.Contains('.'))
            return url;

        return string.Empty;
    }
}
