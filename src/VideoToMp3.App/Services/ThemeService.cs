using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace VideoToMp3.App.Services;

public sealed class ThemeService : IThemeService
{
    public void Apply(string theme)
    {
        var useDark = theme == "Dark" || theme == "System" && IsSystemDark();
        var colors = useDark
            ? new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#111827",
                ["SurfaceBrush"] = "#182235",
                ["SurfaceAltBrush"] = "#202D43",
                ["InputBrush"] = "#111B2D",
                ["PrimaryBrush"] = "#5B9CFF",
                ["TextBrush"] = "#F8FAFC",
                ["MutedTextBrush"] = "#9BAAC0",
                ["BorderBrush"] = "#30405A",
                ["HoverBrush"] = "#253653",
                ["SelectionBrush"] = "#263E66",
                ["DisabledBrush"] = "#1B2639",
                ["ErrorBrush"] = "#FCA5A5",
                ["ErrorRowBrush"] = "#3B2028",
                ["StatusBrush"] = "#223A61"
            }
            : new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#F4F7FB",
                ["SurfaceBrush"] = "#FFFFFF",
                ["SurfaceAltBrush"] = "#F8FAFC",
                ["InputBrush"] = "#FFFFFF",
                ["PrimaryBrush"] = "#2563EB",
                ["TextBrush"] = "#172033",
                ["MutedTextBrush"] = "#667085",
                ["BorderBrush"] = "#DCE3EE",
                ["HoverBrush"] = "#EEF4FF",
                ["SelectionBrush"] = "#E8F0FF",
                ["DisabledBrush"] = "#E9EEF5",
                ["ErrorBrush"] = "#B42318",
                ["ErrorRowBrush"] = "#FFF1F0",
                ["StatusBrush"] = "#E8F0FF"
            };

        var resources = Application.Current.Resources;
        foreach (var (key, value) in colors)
        {
            resources[key] = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(value));
        }

        resources[SystemColors.ControlBrushKey] = resources["InputBrush"];
        resources[SystemColors.ControlTextBrushKey] = resources["TextBrush"];
        resources[SystemColors.WindowBrushKey] = resources["InputBrush"];
        resources[SystemColors.WindowTextBrushKey] = resources["TextBrush"];
        resources[SystemColors.MenuBrushKey] = resources["SurfaceBrush"];
        resources[SystemColors.MenuTextBrushKey] = resources["TextBrush"];
        resources[SystemColors.HighlightBrushKey] = resources["PrimaryBrush"];
        resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
        resources[SystemColors.GrayTextBrushKey] = resources["MutedTextBrush"];
    }

    private static bool IsSystemDark()
    {
        try
        {
            return Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1) is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
