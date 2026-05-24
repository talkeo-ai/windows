namespace Talkeo.Windows.Selection;

using System.Runtime.InteropServices;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;

internal sealed class SelectionReader : IDisposable
{
    private readonly UIA3Automation _automation = new();

    public string? ReadSelectedText() =>
        TryViaUiAutomation() ?? TryViaClipboard();

    private string? TryViaUiAutomation()
    {
        try
        {
            AutomationElement? focused = _automation.FocusedElement();
            if (focused is null) return null;

            var textPattern = focused.Patterns.Text.PatternOrDefault;
            if (textPattern is null) return null;

            var ranges = textPattern.GetSelection();
            if (ranges.Length == 0) return null;

            string text = ranges[0].GetText(-1);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch { return null; }
    }

    private string? TryViaClipboard()
    {
        string? prior = null;
        try { if (System.Windows.Forms.Clipboard.ContainsText()) prior = System.Windows.Forms.Clipboard.GetText(); }
        catch { }

        SendCtrlC();
        System.Threading.Thread.Sleep(120);

        string? captured = null;
        try { if (System.Windows.Forms.Clipboard.ContainsText()) captured = System.Windows.Forms.Clipboard.GetText(); }
        catch { }

        try
        {
            if (prior != null) System.Windows.Forms.Clipboard.SetText(prior);
            else System.Windows.Forms.Clipboard.Clear();
        }
        catch { }

        return captured != prior && !string.IsNullOrEmpty(captured) ? captured : null;
    }

    private static void SendCtrlC()
    {
        var inputs = new NativeMethods.INPUT[4];
        inputs[0] = NativeMethods.KeyInput(0x11, down: true);
        inputs[1] = NativeMethods.KeyInput(0x43, down: true);
        inputs[2] = NativeMethods.KeyInput(0x43, down: false);
        inputs[3] = NativeMethods.KeyInput(0x11, down: false);
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    public void Dispose() => _automation.Dispose();
}
