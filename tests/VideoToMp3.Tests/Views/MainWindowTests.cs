using System.Windows;
using System.Windows.Media;
using VideoToMp3.App;
using VideoToMp3.App.Services;

namespace VideoToMp3.Tests.Views;

public sealed class MainWindowTests
{
    [Fact]
    public void Constructor_LoadsCompiledXamlWithoutBindingCycles()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = new global::VideoToMp3.App.App();
                application.InitializeComponent();
                var window = new MainWindow();
                Assert.NotNull(window.Content);

                new ThemeService().Apply("Dark");
                Assert.Equal(
                    Color.FromRgb(0x17, 0x20, 0x33),
                    Assert.IsType<SolidColorBrush>(application.Resources[SystemColors.ControlTextBrushKey]).Color);
                Assert.Equal(
                    Color.FromRgb(0x27, 0x34, 0x49),
                    Assert.IsType<SolidColorBrush>(application.Resources[SystemColors.ControlBrushKey]).Color);
                window.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "MainWindow construction timed out.");
        Assert.Null(failure);
    }
}
