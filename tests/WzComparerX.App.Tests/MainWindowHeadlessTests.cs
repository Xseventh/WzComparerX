using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using WzComparerX.App.ViewModels;
using WzComparerX.App.Views;

namespace WzComparerX.App.Tests;

public class MainWindowHeadlessTests
{
    [AvaloniaFact]
    public void MainWindow_CreatesExpectedUiShell()
    {
        var viewModel = new MainWindowViewModel();
        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();

        Assert.Same(viewModel, window.DataContext);
        Assert.NotNull(FindControl<TreeView>(window));
        Assert.NotNull(FindTab(window, "Selection"));
        Assert.NotNull(FindTab(window, "Diagnostics"));
        Assert.NotNull(FindTab(window, "Activity"));
    }

    private static T? FindControl<T>(Control root)
        where T : Control
    {
        if (root is T match)
        {
            return match;
        }

        foreach (var child in root.GetVisualChildren().OfType<Control>())
        {
            var matchInChild = FindControl<T>(child);
            if (matchInChild is not null)
            {
                return matchInChild;
            }
        }

        return null;
    }

    private static TabItem? FindTab(Control root, string header)
    {
        if (root is TabItem tabItem &&
            string.Equals(tabItem.Header?.ToString(), header, StringComparison.Ordinal))
        {
            return tabItem;
        }

        foreach (var child in root.GetVisualChildren().OfType<Control>())
        {
            var match = FindTab(child, header);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
