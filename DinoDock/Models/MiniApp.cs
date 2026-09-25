using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DinoDock.Models;

/// <summary>
/// One website that can be opened in DinoDock.
/// All launcher cards are generated from <see cref="AppCatalog.All"/>, so adding a site
/// is just adding another <see cref="MiniApp"/> there.
/// </summary>
public sealed class MiniApp
{
    private Geometry? _iconGeometry;
    private Brush? _tileBrush;
    private Brush? _accentBrush;
    private ImageSource? _iconImage;
    private bool _iconImageLoaded;

    /// <summary>Stable identifier. Used for settings and for the optional Assets/Icons/{Id}.png override.</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required Uri HomeUrl { get; init; }

    /// <summary>SVG-style path data (24x24 viewbox) for the built-in icon glyph. Drawn with the even-odd fill rule.</summary>
    public required string IconPathData { get; init; }

    /// <summary>Start/end colors for the icon tile gradient. <see cref="AccentStart"/> is also the app's accent color.</summary>
    public required Color AccentStart { get; init; }

    public required Color AccentEnd { get; init; }

    /// <summary>"youtube.com" etc. Shown under the card so it's clear what actually opens.</summary>
    public string DisplayHost => HomeUrl.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
        ? HomeUrl.Host[4..]
        : HomeUrl.Host;

    public Geometry IconGeometry => _iconGeometry ??= CreateGeometry(IconPathData);

    public Brush AccentBrush => _accentBrush ??= Freeze(new SolidColorBrush(AccentStart));

    /// <summary>Colored gradient tile behind the glyph, or a neutral tile when a custom PNG icon is used.</summary>
    public Brush TileBrush => _tileBrush ??= HasIconImage
        ? (Brush)Freeze(new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)))
        : Freeze(new LinearGradientBrush(AccentStart, AccentEnd, new Point(0, 0), new Point(1, 1)));

    /// <summary>Optional user-supplied icon from Assets/Icons/{Id}.png next to the executable.</summary>
    public ImageSource? IconImage
    {
        get
        {
            if (!_iconImageLoaded)
            {
                _iconImageLoaded = true;
                _iconImage = TryLoadIconImage(Id);
            }
            return _iconImage;
        }
    }

    public bool HasIconImage => IconImage != null;

    public bool HasGlyph => !HasIconImage;

    private static Geometry CreateGeometry(string pathData)
    {
        try
        {
            // "F0" = even-odd fill, so inner shapes punch holes (e.g. the TV screen).
            var geometry = Geometry.Parse("F0 " + pathData);
            geometry.Freeze();
            return geometry;
        }
        catch (FormatException)
        {
            return Geometry.Empty;
        }
    }

    private static ImageSource? TryLoadIconImage(string id)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", id + ".png");
            if (!File.Exists(path))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file locked
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            Services.Log.Write($"Could not load icon for '{id}'", ex);
            return null;
        }
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
