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
                ["SurfaceBrush"] = "#1F2937",
                ["InputBrush"] = "#273449",
                ["PrimaryBrush"] = "#60A5FA",
                ["TextBrush"] = "#F3F4F6",
                ["MutedTextBrush"] = "#AAB4C4",
                ["BorderBrush"] = "#3B475B",
                ["ErrorBrush"] = "#FCA5A5",
                ["ErrorRowBrush"] = "#3B2028",
                ["StatusBrush"] = "#263A5B"
            }
            : new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#F4F7FB",
                ["SurfaceBrush"] = "#FFFFFF",
                ["InputBrush"] = "#FFFFFF",
                ["PrimaryBrush"] = "#2563EB",
                ["TextBrush"] = "#172033",
                ["MutedTextBrush"] = "#667085",
                ["BorderBrush"] = "#DCE3EE",
                ["ErrorBrush"] = "#B42318",
                ["ErrorRowBrush"] = "#FFF1F0",
                ["StatusBrush"] = "#E8F0FF"
            };

        foreach (var (key, value) in colors)
        {
            Application.Current.Resources[key] = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(value));
        }
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
