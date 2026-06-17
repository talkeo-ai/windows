using Microsoft.UI.Xaml;
using Talkeo.Windows.Hooks;
using Talkeo.Windows.Selection;
using Talkeo.Windows.Tray;
using Talkeo.Windows.UI;

namespace Talkeo.Windows;

public partial class App : Application
{
    private TrayIcon?        _tray;
    private MouseHook?       _hook;
    private SelectionReader? _reader;
    private TooltipPopup?    _tooltip;

    private Microsoft.UI.Dispatching.DispatcherQueue? _queue;

    public App()
    {
        Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.Initialize(0x00010006);
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Invisible WinUI 3 window — required to keep the app alive.
        var lifecycle = new TooltipWindow();
        lifecycle.Activate();

        _queue   = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _reader  = new SelectionReader();
        _tray    = new TrayIcon(onQuit: QuitApp);
        _tooltip = new TooltipPopup();

        _hook = new MouseHook(onGesture: OnSelectionGesture, onAnyDown: OnAnyMouseDown);
        _hook.Install();
    }

    private void OnSelectionGesture(System.Drawing.Point cursorPos)
    {
        _queue!.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            async () =>
            {
                await System.Threading.Tasks.Task.Delay(80);
                string? text = _reader!.ReadSelectedText();
                if (string.IsNullOrWhiteSpace(text)) { _tooltip!.HideTooltip(); return; }
                _tooltip!.ShowCollapsed(text, cursorPos);
            });
    }

    private void OnAnyMouseDown(System.Drawing.Point pt)
    {
        if (_tooltip == null || !_tooltip.IsTooltipVisible) return;
        if (!_tooltip.ContainsScreenPoint(pt))
            _queue!.TryEnqueue(() => _tooltip.HideTooltip());
    }

    private void QuitApp()
    {
        _hook?.Uninstall();
        _tray?.Dispose();
        _reader?.Dispose();
        _tooltip?.Dispose();
        Exit();
    }
}
