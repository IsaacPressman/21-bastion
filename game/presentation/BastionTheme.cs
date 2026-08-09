using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The one theme, built in code alongside the rest of the UI.
/// </summary>
/// <remarks>
/// <para>
/// Built rather than authored for the same reason the views are (see <c>Bootstrap</c>): it stays under
/// version control as C#, and there is one wiring path instead of a scene file that has to be kept in
/// step with it. Applied once to the UI root, from where Godot propagates it to every Control below.
/// </para>
/// <para>
/// The <see cref="Mono"/> variation exists because several panels print aligned columns - the rank
/// composition, the per-lane damage ledger - by padding strings. Padding only aligns in a monospaced
/// face, so those labels ask for one explicitly instead of quietly rendering ragged in the proportional
/// default.
/// </para>
/// <para>
/// Fonts come from <see cref="SystemFont"/> rather than a shipped file: the prototype has no font asset
/// and does not need one, and a name list degrades to whatever the machine has.
/// </para>
/// </remarks>
internal static class BastionTheme
{
    /// <summary>Theme type variation for labels printing aligned numeric columns.</summary>
    internal const string Mono = "Mono";

    /// <summary>Theme type variation for a phase's single most important action.</summary>
    internal const string PrimaryButton = "PrimaryButton";

    /// <summary>Theme type variation for a panel's title line.</summary>
    internal const string PanelTitle = "PanelTitle";

    /// <summary>Theme type variation for the small print under a control.</summary>
    internal const string Hint = "Hint";

    internal static Font MonoFont { get; } = new SystemFont
    {
        FontNames = ["Cascadia Mono", "Consolas", "DejaVu Sans Mono", "Courier New", "monospace"],
    };

    internal static Font UiFont { get; } = new SystemFont
    {
        FontNames = ["Segoe UI", "Inter", "Noto Sans", "DejaVu Sans", "sans-serif"],
    };

    internal static Theme Create()
    {
        var theme = new Theme
        {
            DefaultFont = UiFont,
            DefaultFontSize = 15,
        };

        StyleLabels(theme);
        StylePanels(theme);
        StyleButtons(theme);
        StyleScrolling(theme);

        return theme;
    }

    private static void StyleLabels(Theme theme)
    {
        theme.SetColor("font_color", "Label", Palette.Text);

        // The default 3px leading is generous for a column of short data lines, and the information
        // panels overflow their column with it. Tightened so the pile and the review stay in view.
        theme.SetConstant("line_spacing", "Label", 1);

        theme.SetTypeVariation(Mono, "Label");
        theme.SetFont("font", Mono, MonoFont);
        theme.SetFontSize("font_size", Mono, 14);
        theme.SetColor("font_color", Mono, Palette.Text);
        theme.SetConstant("line_spacing", Mono, 2);

        // Small caps-ish section headers: dim, so the numbers under them carry the emphasis.
        theme.SetTypeVariation(PanelTitle, "Label");
        theme.SetFontSize("font_size", PanelTitle, 12);
        theme.SetColor("font_color", PanelTitle, Palette.TextFaint);

        theme.SetTypeVariation(Hint, "Label");
        theme.SetFontSize("font_size", Hint, 13);
        theme.SetColor("font_color", Hint, Palette.TextDim);
    }

    private static void StylePanels(Theme theme)
    {
        theme.SetStylebox("panel", "PanelContainer", Surface(Palette.Surface));

        // A bare Panel is used for the header and the action bar, which sit against the window edge
        // and so want a flat edge rather than a floating card.
        theme.SetStylebox("panel", "Panel", new StyleBoxFlat
        {
            BgColor = Palette.SurfaceRaised,
            BorderColor = Palette.SurfaceEdge,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        });
    }

    private static void StyleButtons(Theme theme)
    {
        theme.SetStylebox("normal", "Button", Button(Palette.SurfaceRaised, Palette.SurfaceEdge));
        theme.SetStylebox("hover", "Button", Button(new Color("222d3d"), Palette.SocketHover));
        theme.SetStylebox("pressed", "Button", Button(new Color("2b394d"), Palette.SocketHover));
        theme.SetStylebox("disabled", "Button", Button(new Color("141a24"), new Color("1e2634")));
        theme.SetStylebox("focus", "Button", Button(new Color(0, 0, 0, 0), Palette.SocketHover));

        theme.SetColor("font_color", "Button", Palette.Text);
        theme.SetColor("font_hover_color", "Button", Palette.Text);
        theme.SetColor("font_pressed_color", "Button", Palette.Text);
        theme.SetColor("font_disabled_color", "Button", Palette.TextFaint);

        // The primary action: larger, and the only filled control on screen, so it reads as the way
        // forward without needing a colour that would imply the move itself is a good one.
        theme.SetTypeVariation(PrimaryButton, "Button");
        theme.SetFontSize("font_size", PrimaryButton, 17);
        theme.SetStylebox("normal", PrimaryButton, Primary(new Color("2d3c52")));
        theme.SetStylebox("hover", PrimaryButton, Primary(new Color("3a4c67")));
        theme.SetStylebox("pressed", PrimaryButton, Primary(new Color("445876")));
        theme.SetStylebox("disabled", PrimaryButton, Primary(new Color("1a212c")));
        theme.SetStylebox("focus", PrimaryButton, Primary(new Color(0, 0, 0, 0)));
    }

    private static void StyleScrolling(Theme theme)
    {
        theme.SetStylebox("panel", "ScrollContainer", new StyleBoxEmpty());

        // The playback scrubber. Flat and low-contrast: it reports progress, it is not a control.
        theme.SetStylebox("background", "ProgressBar", new StyleBoxFlat
        {
            BgColor = Palette.SurfaceEdge,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
        });

        theme.SetStylebox("fill", "ProgressBar", new StyleBoxFlat
        {
            BgColor = Palette.SocketHover,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
        });
    }

    private static StyleBoxFlat Surface(Color fill) => new()
    {
        BgColor = fill,
        BorderColor = Palette.SurfaceEdge,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = 6,
        CornerRadiusTopRight = 6,
        CornerRadiusBottomLeft = 6,
        CornerRadiusBottomRight = 6,
        ContentMarginLeft = 12,
        ContentMarginRight = 12,
        ContentMarginTop = 10,
        ContentMarginBottom = 10,
    };

    private static StyleBoxFlat Button(Color fill, Color border) => new()
    {
        BgColor = fill,
        BorderColor = border,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4,
        ContentMarginLeft = 10,
        ContentMarginRight = 10,
        ContentMarginTop = 6,
        ContentMarginBottom = 6,
    };

    private static StyleBoxFlat Primary(Color fill)
    {
        StyleBoxFlat box = Button(fill, Palette.SocketHover);
        box.ContentMarginTop = 12;
        box.ContentMarginBottom = 12;
        box.ContentMarginLeft = 18;
        box.ContentMarginRight = 18;
        return box;
    }
}
