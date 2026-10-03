using System.Runtime.InteropServices;
using System.Text.Json;

namespace OpenMD;

internal sealed record ThemePalette(Color Surface, Color Editor, Color Text, Color Muted, Color Border, Color Hover)
{
    public static ThemePalette Light { get; } = new(Color.White, Color.FromArgb(249, 250, 252),
        Color.FromArgb(30, 41, 59), Color.FromArgb(100, 116, 139), Color.FromArgb(226, 232, 240), Color.FromArgb(224, 231, 255));
    public static ThemePalette Dark { get; } = new(Color.FromArgb(24, 31, 46), Color.FromArgb(15, 23, 42),
        Color.FromArgb(226, 232, 240), Color.FromArgb(148, 163, 184), Color.FromArgb(51, 65, 85), Color.FromArgb(49, 46, 89));
}

internal sealed class ThemeColorTable(ThemePalette palette) : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => palette.Surface;
    public override Color ImageMarginGradientBegin => palette.Surface;
    public override Color ImageMarginGradientMiddle => palette.Surface;
    public override Color ImageMarginGradientEnd => palette.Surface;
    public override Color MenuItemSelected => palette.Hover;
    public override Color MenuItemSelectedGradientBegin => palette.Hover;
    public override Color MenuItemSelectedGradientEnd => palette.Hover;
    public override Color MenuItemPressedGradientBegin => palette.Hover;
    public override Color MenuItemPressedGradientMiddle => palette.Hover;
    public override Color MenuItemPressedGradientEnd => palette.Hover;
    public override Color ButtonSelectedGradientBegin => palette.Hover;
    public override Color ButtonSelectedGradientMiddle => palette.Hover;
    public override Color ButtonSelectedGradientEnd => palette.Hover;
    public override Color ButtonPressedGradientBegin => palette.Hover;
    public override Color ButtonPressedGradientMiddle => palette.Hover;
    public override Color ButtonPressedGradientEnd => palette.Hover;
    public override Color ButtonCheckedGradientBegin => palette.Hover;
    public override Color ButtonCheckedGradientMiddle => palette.Hover;
    public override Color ButtonCheckedGradientEnd => palette.Hover;
    public override Color MenuBorder => palette.Border;
    public override Color MenuItemBorder => palette.Border;
    public override Color ToolStripBorder => palette.Border;
    public override Color SeparatorDark => palette.Border;
    public override Color SeparatorLight => palette.Surface;
    public override Color ToolStripGradientBegin => palette.Surface;
    public override Color ToolStripGradientMiddle => palette.Surface;
    public override Color ToolStripGradientEnd => palette.Surface;
    public override Color StatusStripGradientBegin => palette.Surface;
    public override Color StatusStripGradientEnd => palette.Surface;
}

internal static class ThemeSettings
{
    private sealed record Preferences(bool DarkMode);
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenMD", "settings.json");

    internal static bool Load(string? path = null)
    {
        try { return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path ?? SettingsPath))?.DarkMode ?? false; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return false; }
    }

    internal static void Save(bool dark, string? path = null)
    {
        var destination = path ?? SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temp = destination + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new Preferences(dark)));
        File.Move(temp, destination, true);
    }
}

internal static class WindowTheme
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    internal static void Apply(IntPtr handle, bool dark)
    {
        var value = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, 20, ref value, sizeof(int));
    }
}

