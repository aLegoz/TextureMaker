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

        // Allow dragging the entire block in preview mode (even over handled events)
        RootBorder.AddHandler(
            UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(RootBorder_MouseDown),
            handledEventsToo: true);

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

    // ── Drag ──────────────────────────────────────────────────────────

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        e.Handled = true;
        StartDrag(e);
    }

    // In preview mode we also allow dragging by clicking anywhere on the block
    private void RootBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isPreview) return;
        if (e.ChangedButton != MouseButton.Left) return;
        if (HeaderBorder.IsMouseCaptured) return; // header drag already active
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

    private void EditToggle_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
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
            EditToggleIcon.Text    = "\u270F"; // pencil → back to edit
        }
        else
        {
            BodyEdit.Visibility    = Visibility.Visible;
            BodyPreview.Visibility = Visibility.Collapsed;
            ColorRow.Visibility    = Visibility.Visible;
            EditToggleIcon.Text    = "\u25B6"; // triangle → show preview
        }
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

    private void Resize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newW = Math.Max(180, Width  + e.HorizontalChange);
        double newH = Math.Max(100, Height + e.VerticalChange);
        Width  = newW; _vm.Width  = newW;
        Height = newH; _vm.Height = newH;
    }
}
