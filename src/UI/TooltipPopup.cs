namespace Talkeo.Windows.UI;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;  // Ellipse, Rectangle (separator)

public sealed class TooltipPopup : System.Windows.Window, IDisposable
{
    // ── Win32 ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int n);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int n, int v);
    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_TOOLWINDOW = 0x0080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // ── Layout ─────────────────────────────────────────────────────────────
    private const double CSz    = 44;
    private const double W      = 300;
    private const double PadY   = 5;
    private const double RowH   = 58;
    private const double IconSz = 28;
    private const double IconX0 = 14;
    private const double Radius = 16;
    private static readonly double H = PadY * 2 + RowH * 4;  // 248

    // ── Colors ─────────────────────────────────────────────────────────────
    private static readonly Color BgColor   = Color.FromRgb(28,  28,  30);
    private static readonly Color RowHover  = Color.FromRgb(44,  44,  48);
    private static readonly Color FgPrimary = Color.FromRgb(242, 242, 247);
    private static readonly Color FgSub     = Color.FromRgb(142, 142, 147);
    private static readonly Color SepColor  = Color.FromRgb(40,  40,  42);
    private static readonly Color IconClr   = Color.FromRgb(210, 210, 215);

    // ── Menu ───────────────────────────────────────────────────────────────
    private static readonly (string Title, string Sub)[] Menu =
    {
        ("Capture text",      "OCR on a screen region"),
        ("Switch language",   "Auto ES ⇄ EN"),
        ("Improve copy",      "Polish the selected text"),
        ("Open TalkeoSelect", "Settings & history"),
    };

    // Segoe MDL2 Assets glyphs — native Windows icon font, available Win10+
    private static readonly string[] Glyphs = { "", "", "" };

    // ── State ──────────────────────────────────────────────────────────────
    private BitmapImage?          _logo;
    private bool                  _expanded;
    private System.Drawing.Point  _cursor;
    private readonly Grid         _root;

    // DPI scale: logical/physical (= 96/dpi). Set on first show, used everywhere.
    private double _sx = 1.0, _sy = 1.0;

    // Thread-safe hit-test state (written on WPF thread, read on hook thread)
    private volatile bool _isVisible;
    private volatile bool _expandedVol;
    private double _cacheLeft, _cacheTop;

    // ── Public API ─────────────────────────────────────────────────────────

    public bool IsTooltipVisible => _isVisible;

    public bool ContainsScreenPoint(System.Drawing.Point pt)
    {
        if (!_isVisible) return false;
        // pt is physical; _cacheLeft/Top are logical. _sx = logical/physical.
        double lx = pt.X * _sx - _cacheLeft;
        double ly = pt.Y * _sy - _cacheTop;
        if (!_expandedVol)
            return lx >= 0 && lx < CSz * _sx && ly >= 0 && ly < CSz * _sy;
        return lx >= 0 && lx < W * _sx && ly >= 0 && ly < H * _sy;
    }

    public void ShowCollapsed(string text, System.Drawing.Point cursor)
    {
        _cursor = cursor; _expanded = false; _expandedVol = false;
        if (!IsVisible) { Left = -32000; Top = -32000; Show(); }
        RefreshScale();
        Width = CSz * _sx; Height = CSz * _sy;
        UpdateContent();
        PositionWindow(cursor.X + 14, cursor.Y + 14, Width, Height);
        _isVisible = true;
        Opacity = 0;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
    }

    public void HideTooltip()
    {
        _isVisible = false;
        var fadeOut = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (_, _) =>
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
            Hide();
            _expanded = false; _expandedVol = false;
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    public void Dispose() => Close();

    // ── Constructor ────────────────────────────────────────────────────────

    public TooltipPopup()
    {
        WindowStyle        = WindowStyle.None;
        AllowsTransparency = true;
        Background         = Brushes.Transparent;
        ShowInTaskbar      = false;
        Topmost            = true;
        ShowActivated      = false;
        Width              = CSz;
        Height             = CSz;
        Left               = -32000;
        Top                = -32000;

        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
        if (System.IO.File.Exists(iconPath))
            _logo = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

        _root = new Grid();
        Content = _root;

        SourceInitialized += (_, _) =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        };
    }

    // ── Internal ───────────────────────────────────────────────────────────

    private void Expand()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
        fadeOut.Completed += (_, _) =>
        {
            _expanded = true; _expandedVol = true;
            RefreshScale();
            Width = W * _sx; Height = H * _sy;
            UpdateContent();
            PositionWindow(_cursor.X + 14, _cursor.Y + 14, Width, Height);
            Opacity = 0;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    // Computes DPI scale from PresentationSource (falls back to Graphics.FromHwnd),
    // caches it in _sx/_sy, and applies LayoutTransform so content renders at
    // design-pixel size regardless of the display's scale factor.
    private void RefreshScale()
    {
        var src = PresentationSource.FromVisual(this);
        if (src != null)
        {
            _sx = src.CompositionTarget.TransformFromDevice.M11;
            _sy = src.CompositionTarget.TransformFromDevice.M22;
        }
        else
        {
            using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            _sx = _sy = 96.0 / g.DpiX;
        }
        _root.LayoutTransform = new System.Windows.Media.ScaleTransform(_sx, _sy);
    }

    private void PositionWindow(int px, int py, double logW, double logH)
    {
        double lx = px * _sx, ly = py * _sy;

        var wa = System.Windows.Forms.Screen.FromPoint(_cursor).WorkingArea;
        double waR = wa.Right  * _sx, waB = wa.Bottom * _sy;
        double waL = wa.Left   * _sx, waT = wa.Top    * _sy;

        if (lx + logW > waR)  lx = _cursor.X * _sx - logW - 14 * _sx;
        if (ly + logH > waB)  ly = _cursor.Y * _sy - logH - 14 * _sy;
        if (lx < waL) lx = waL + 4;
        if (ly < waT) ly = waT + 4;

        Left = lx; Top = ly;
        _cacheLeft = lx; _cacheTop = ly;
    }

    private void UpdateContent()
    {
        _root.Children.Clear();
        if (_expanded) BuildExpanded();
        else           BuildCollapsed();
    }

    // ── Collapsed ──────────────────────────────────────────────────────────

    private void BuildCollapsed()
    {
        var bg = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background   = new SolidColorBrush(BgColor),
            Width        = CSz,
            Height       = CSz,
            Cursor       = Cursors.Hand,
        };

        if (_logo != null)
        {
            var e = new Ellipse { Width = CSz - 10, Height = CSz - 10 };
            RenderOptions.SetBitmapScalingMode(e, BitmapScalingMode.HighQuality);
            e.Fill     = new ImageBrush(_logo) { Stretch = Stretch.UniformToFill };
            bg.Child   = e;
        }
        else
        {
            bg.Child = new TextBlock
            {
                Text                = "T",
                FontSize            = 14,
                FontWeight          = FontWeights.Bold,
                Foreground          = new SolidColorBrush(FgPrimary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            };
        }

        bg.MouseEnter          += (_, _) => bg.Background = new SolidColorBrush(RowHover);
        bg.MouseLeave          += (_, _) => bg.Background = new SolidColorBrush(BgColor);
        bg.MouseLeftButtonDown += (_, _) => Expand();

        _root.Children.Add(bg);
    }

    // ── Expanded ───────────────────────────────────────────────────────────

    private void BuildExpanded()
    {
        var outer = new Border
        {
            CornerRadius = new CornerRadius(Radius),
            Background   = new SolidColorBrush(BgColor),
            Padding      = new Thickness(0, PadY, 0, PadY),
        };

        var stack = new StackPanel();
        for (int i = 0; i < Menu.Length; i++)
            stack.Children.Add(BuildRow(i));

        outer.Child = stack;
        _root.Children.Add(outer);
    }

    private UIElement BuildRow(int idx)
    {
        var (title, sub) = Menu[idx];

        var container = new Grid { Height = RowH, Cursor = Cursors.Hand, Background = Brushes.Transparent };

        if (idx > 0)
        {
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var sep = new Rectangle
            {
                Fill   = new SolidColorBrush(SepColor),
                Margin = new Thickness(5, 0, 5, 0),
            };
            Grid.SetRow(sep, 0);
            container.Children.Add(sep);
        }
        else
        {
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        // Hover rect: inset on sides only — top/bottom margin=0 so it meets separators flush
        var hover = new Border
        {
            CornerRadius = new CornerRadius(10),
            Margin       = new Thickness(5, 0, 5, 0),
            Background   = Brushes.Transparent,
        };
        Grid.SetRow(hover, idx > 0 ? 1 : 0);

        var content = new DockPanel
        {
            Margin            = new Thickness(8, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            LastChildFill     = true,
        };

        // Icons have no background — they float directly on the tooltip surface
        var iconBox = new Border
        {
            Width        = IconSz,
            Height       = IconSz,
            Background   = Brushes.Transparent,
            Margin       = new Thickness(0, 0, 12, 0),
        };
        DockPanel.SetDock(iconBox, Dock.Left);

        if (idx == Menu.Length - 1 && _logo != null)
        {
            var e = new Ellipse { Width = IconSz, Height = IconSz };
            RenderOptions.SetBitmapScalingMode(e, BitmapScalingMode.HighQuality);
            e.Fill        = new ImageBrush(_logo) { Stretch = Stretch.UniformToFill };
            iconBox.Child = e;
        }
        else
        {
            iconBox.Child = new TextBlock
            {
                Text                = Glyphs[idx],
                FontFamily          = new FontFamily("Segoe MDL2 Assets"),
                FontSize            = 20,
                Foreground          = new SolidColorBrush(IconClr),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            };
        }

        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock
        {
            Text       = title,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize   = 14.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(FgPrimary),
        });
        texts.Children.Add(new TextBlock
        {
            Text       = sub,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize   = 12.5,
            Foreground = new SolidColorBrush(FgSub),
        });

        content.Children.Add(iconBox);
        content.Children.Add(texts);
        hover.Child = content;
        container.Children.Add(hover);

        container.MouseEnter          += (_, _) => hover.Background = new SolidColorBrush(RowHover);
        container.MouseLeave          += (_, _) => hover.Background = Brushes.Transparent;
        container.MouseLeftButtonDown += (_, _) =>
        {
            System.Diagnostics.Debug.WriteLine($"[Talkeo] {title}");
            HideTooltip();
        };

        return container;
    }

}
