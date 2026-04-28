using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Skia;

[assembly: AvaloniaTestApplication(typeof(WzComparerX.App.Tests.AvaloniaHeadlessTestApplication))]

namespace WzComparerX.App.Tests;

public static class AvaloniaHeadlessTestApplication
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<WzComparerX.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            });
    }
}
