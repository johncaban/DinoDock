using System.Windows.Media;

namespace DinoDock.Models;

/// <summary>
/// The websites shown in the launcher. To add another site, add a new <see cref="MiniApp"/> here
/// (see README "Adding another website").
/// </summary>
public static class AppCatalog
{
    // Icon glyphs are 24x24 path data. The globe and play-circle shapes are from Google's
    // Material Icons (Apache 2.0); the TV glyph is original.
    private const string GlobeGlyph =
        "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm6.93 6h-2.95c-.32-1.25-.78-2.45-1.38-3.56 1.84.63 3.37 1.91 4.33 3.56zM12 4.04c.83 1.2 1.48 2.53 1.91 3.96h-3.82c.43-1.43 1.08-2.76 1.91-3.96zM4.26 14C4.1 13.36 4 12.69 4 12s.1-1.36.26-2h3.38c-.08.66-.14 1.32-.14 2 0 .68.06 1.34.14 2H4.26zm.82 2h2.95c.32 1.25.78 2.45 1.38 3.56-1.84-.63-3.37-1.9-4.33-3.56zm2.95-8H5.08c.96-1.66 2.49-2.93 4.33-3.56C8.81 5.55 8.35 6.75 8.03 8zM12 19.96c-.83-1.2-1.48-2.53-1.91-3.96h3.82c-.43 1.43-1.08 2.76-1.91 3.96zM14.34 14H9.66c-.09-.66-.16-1.32-.16-2 0-.68.07-1.35.16-2h4.68c.09.65.16 1.32.16 2 0 .68-.07 1.34-.16 2zm.25 5.56c.6-1.11 1.06-2.31 1.38-3.56h2.95c-.96 1.65-2.49 2.93-4.33 3.56zM16.36 14c.08-.66.14-1.32.14-2 0-.68-.06-1.34-.14-2h3.38c.16.64.26 1.31.26 2s-.1 1.36-.26 2h-3.38z";

    private const string TvGlyph =
        "M4 4h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zM4.2 6v10h15.6V6zM10 8.2l5 2.8-5 2.8zM7 20h10v1.6H7z";

    private const string PlayCircleGlyph =
        "M10 16.5l6-4.5-6-4.5v9zM12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z";

    public static IReadOnlyList<MiniApp> All { get; } =
    [
        new MiniApp
        {
            // Opens Google inside DinoDock; it does not launch the installed Chrome browser.
            Id = "google",
            Name = "Google",
            Description = "Browse the web",
            HomeUrl = new Uri("https://www.google.com/"),
            IconPathData = GlobeGlyph,
            AccentStart = Color.FromRgb(0x4C, 0x8D, 0xF6),
            AccentEnd = Color.FromRgb(0x2F, 0xB5, 0x7C),
        },
        new MiniApp
        {
            Id = "hulu",
            Name = "Hulu",
            Description = "Watch Hulu",
            HomeUrl = new Uri("https://www.hulu.com/"),
            IconPathData = TvGlyph,
            AccentStart = Color.FromRgb(0x1C, 0xE7, 0x83),
            AccentEnd = Color.FromRgb(0x0B, 0x9E, 0x6A),
        },
        new MiniApp
        {
            Id = "youtube",
            Name = "YouTube",
            Description = "Watch YouTube",
            HomeUrl = new Uri("https://www.youtube.com/"),
            IconPathData = PlayCircleGlyph,
            AccentStart = Color.FromRgb(0xFF, 0x4E, 0x45),
            AccentEnd = Color.FromRgb(0xC4, 0x12, 0x12),
        },
    ];

    public static MiniApp? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
}
