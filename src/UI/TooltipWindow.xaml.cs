namespace Talkeo.Windows.UI;

using Microsoft.UI.Xaml;

// Minimal WinUI 3 window kept only to satisfy the app lifecycle requirement.
// The actual tooltip UI is rendered by TooltipForm (WinForms, truly borderless).
public sealed partial class TooltipWindow : Window
{
    public TooltipWindow()
    {
        InitializeComponent();
        AppWindow.Move(new global::Windows.Graphics.PointInt32(-32000, -32000));
    }
}
