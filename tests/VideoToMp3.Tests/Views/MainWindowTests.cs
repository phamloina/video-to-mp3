using System.Windows;
using VideoToMp3.App;

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
                _ = Application.Current ?? new Application();
                var window = new MainWindow();
                Assert.NotNull(window.Content);
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
