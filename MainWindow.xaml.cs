using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace CollageGenerator;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ApplyThemeToTitleBar();
    }

    private void ApplyThemeToTitleBar()
    {
        var titleBar = AppWindow.TitleBar;
        var background = ColorHelper.FromArgb(255, 31, 31, 31);
        var foreground = Colors.White;
        var hover = ColorHelper.FromArgb(255, 45, 45, 45);
        var pressed = ColorHelper.FromArgb(255, 62, 62, 62);

        titleBar.BackgroundColor = background;
        titleBar.ForegroundColor = foreground;
        titleBar.InactiveBackgroundColor = background;
        titleBar.InactiveForegroundColor = ColorHelper.FromArgb(255, 180, 180, 180);
        titleBar.ButtonBackgroundColor = background;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonHoverBackgroundColor = hover;
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonPressedBackgroundColor = pressed;
        titleBar.ButtonPressedForegroundColor = foreground;
        titleBar.ButtonInactiveBackgroundColor = background;
        titleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 180, 180, 180);
    }
}
