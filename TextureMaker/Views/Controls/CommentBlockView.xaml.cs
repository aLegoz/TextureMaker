using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TextureMaker.Graph;
using TextureMaker.Views.Dialogs;
using WpfColor = System.Windows.Media.Color;

namespace TextureMaker.Views.Controls;

public partial class CommentBlockView : UserControl
{
    private readonly CommentBlockViewModel _vm;
    private bool _isPreview;
    private Border[] _colorBorders = null!;

    private static readonly WpfColor[] PresetColors =
    [
        WpfColor.FromRgb(50,  80,  110),  // blue
        WpfColor.FromRgb(60,  100, 60),   // green
        WpfColor.FromRgb(110, 60,  60),   // red
        WpfColor.FromRgb(100, 70,  30),   // amber
        WpfColor.FromRgb(70,  50,  110),  // purple
        WpfColor.FromRgb(50,  90,  90),   // teal
        WpfColor.FromRgb(75,  75,  75),   // grey
        WpfColor.FromRgb(90,  65,  50),   // brown
    ];

    public event Action<CommentBlockViewModel>? DeleteRequested;

    public CommentBlockView(CommentBlockViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Width  = vm.Width;
        Height = vm.Height;

        // In preview mode clicking the body (ScrollViewer) should drag the block.
        BodyPreview.MouseLeftButtonDown += BodyPreview_MouseDown;

        // Forward wheel events to the parent Canvas even when a child (TextBox /
        // ScrollViewer) has already marked the event as handled, so GraphCanvas
        // zoom/pan still works while the cursor is over a comment block.
        AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler((_, e) =>
        {
            if (!e.Handled) return; // already bubbling normally — don't double-fire
            if (VisualParent is UIElement parent)
                parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    { RoutedEvent = UIElement.MouseWheelEvent });
        }), handledEventsToo: true);

        BuildColorPalette();
        ApplyHeaderColor(vm.HeaderColor);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CommentBlockViewModel.HeaderColor))
            {
                ApplyHeaderColor(vm.HeaderColor);
                UpdateColorSelection();
            }
        };

        BodyEdit.TextChanged += (_, _) =>
        {
            if (_isPreview) MdViewer.Markdown = _vm.Body;
        };
    }

    private void BuildColorPalette()
    {
        _colorBorders = new Border[PresetColors.Length];
        for (int i = 0; i < PresetColors.Length; i++)
        {
            int idx = i;
            var border = new Border
            {
                Width           = 16,
                Height          = 16,
                CornerRadius    = new CornerRadius(8),
                Background      = new SolidColorBrush(PresetColors[i]),
                BorderThickness = new Thickness(2),
                Margin          = new Thickness(2, 0, 2, 0),
                Cursor          = Cursors.Hand,
            };
            border.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                _vm.HeaderColor = PresetColors[idx];
            };
            _colorBorders[i] = border;
            ColorPalette.Children.Add(border);
        }
        UpdateColorSelection();
    }

    private void UpdateColorSelection()
    {
        for (int i = 0; i < PresetColors.Length; i++)
            _colorBorders[i].BorderBrush = _vm.HeaderColor == PresetColors[i]
                ? Brushes.White
                : Brushes.Transparent;
    }

    private void ApplyHeaderColor(WpfColor c)
    {
        HeaderBorder.Background = new SolidColorBrush(c);
        RootBorder.Background   = new SolidColorBrush(
            WpfColor.FromRgb(
                (byte)(c.R * 0.3),
                (byte)(c.G * 0.3),
                (byte)(c.B * 0.3)));
    }

    // ── Mode toggle ───────────────────────────────────────────────────

    private void EditToggle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        e.Handled = true;   // stop event here — do NOT bubble to Header_MouseDown
        _isPreview = !_isPreview;
        ApplyMode();
    }

    public void SetPreviewMode(bool preview)
    {
        _isPreview = preview;
        ApplyMode();
    }

    public void ApplyMode()
    {
        if (_isPreview)
        {
            MdViewer.Markdown      = _vm.Body;
            BodyEdit.Visibility    = Visibility.Collapsed;
            BodyPreview.Visibility = Visibility.Visible;
            ColorRow.Visibility    = Visibility.Collapsed;
            // Title not editable in preview — clicks fall through to HeaderBorder (drag)
            TitleBox.IsHitTestVisible = false;
            EditToggleIcon.Text       = "\u270F";                               // pencil
            EditToggleIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
        }
        else
        {
            BodyEdit.Visibility    = Visibility.Visible;
            BodyPreview.Visibility = Visibility.Collapsed;
            ColorRow.Visibility    = Visibility.Visible;
            TitleBox.IsHitTestVisible = true;
            EditToggleIcon.Text       = "\u2713";                               // checkmark
            EditToggleIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        }
    }

    // ── Drag ──────────────────────────────────────────────────────────

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        e.Handled = true;
        StartDrag(e);
    }

    // Allow dragging via the body area when in preview mode.
    // Using normal (non-handledEventsToo) subscription so the scroll bar
    // on its track still works (ScrollViewer marks those events handled).
    private void BodyPreview_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        e.Handled = true;
        StartDrag(e);
    }

    private void StartDrag(MouseButtonEventArgs e)
    {
        if (VisualParent is not Canvas canvas) return;

        var startMouse = e.GetPosition(canvas);
        var startLeft  = Canvas.GetLeft(this);
        var startTop   = Canvas.GetTop(this);

        void OnMove(object s, MouseEventArgs me)
        {
            var p = me.GetPosition(canvas);
            double newX = startLeft + (p.X - startMouse.X);
            double newY = startTop  + (p.Y - startMouse.Y);
            Canvas.SetLeft(this, newX);
            Canvas.SetTop(this,  newY);
            _vm.Position = new Point(newX, newY);
        }

        void OnUp(object s, MouseButtonEventArgs me)
        {
            if (me.ChangedButton != MouseButton.Left) return;
            HeaderBorder.ReleaseMouseCapture();
            HeaderBorder.MouseMove         -= OnMove;
            HeaderBorder.MouseLeftButtonUp -= OnUp;
        }

        HeaderBorder.MouseMove         += OnMove;
        HeaderBorder.MouseLeftButtonUp += OnUp;
        HeaderBorder.CaptureMouse();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var owner = Window.GetWindow(this);
        var dlg = new ConfirmDialog(owner,
            "Delete Comment",
            "Delete this comment block from the canvas?");
        dlg.ShowDialog();
        if (dlg.Confirmed) DeleteRequested?.Invoke(_vm);
    }

    // Bottom-right corner: both axes
    private void Resize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newW = Math.Max(180, Width  + e.HorizontalChange);
        double newH = Math.Max(100, Height + e.VerticalChange);
        Width  = newW; _vm.Width  = newW;
        Height = newH; _vm.Height = newH;
    }

    // Left edge: width grows/shrinks, position adjusts so right edge stays fixed
    private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newW  = Math.Max(180, Width - e.HorizontalChange);
        double delta = Width - newW;            // actual change after clamping
        Width = newW; _vm.Width = newW;
        double newLeft = Canvas.GetLeft(this) + delta;
        Canvas.SetLeft(this, newLeft);
        _vm.Position = new Point(newLeft, Canvas.GetTop(this));
    }

    // Right edge: width only
    private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newW = Math.Max(180, Width + e.HorizontalChange);
        Width = newW; _vm.Width = newW;
    }

    // Bottom edge: height only
    private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newH = Math.Max(100, Height + e.VerticalChange);
        Height = newH; _vm.Height = newH;
    }
}
