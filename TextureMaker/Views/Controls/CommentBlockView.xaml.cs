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

    // Preset header colors
    private static readonly WpfColor[] PresetColors =
    [
        Color.FromRgb(50,  80, 110),   // blue
        Color.FromRgb(60,  100, 60),   // green
        Color.FromRgb(110, 60,  60),   // red
        Color.FromRgb(100, 70,  30),   // amber
        Color.FromRgb(70,  50,  110),  // purple
        Color.FromRgb(50,  90,  90),   // teal
    ];

    public event Action<CommentBlockViewModel>? DeleteRequested;

    public CommentBlockView(CommentBlockViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // Set initial size
        Width  = vm.Width;
        Height = vm.Height;

        // Sync header color
        ApplyHeaderColor(vm.HeaderColor);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CommentBlockViewModel.HeaderColor))
                ApplyHeaderColor(vm.HeaderColor);
        };

        // Sync markdown when body changes and in preview mode
        BodyEdit.TextChanged += (_, _) =>
        {
            if (_isPreview) MdViewer.Markdown = _vm.Body;
        };
    }

    private void ApplyHeaderColor(WpfColor c)
    {
        HeaderBorder.Background = new SolidColorBrush(c);
        ColorSwatch.Background  = new SolidColorBrush(
            Color.FromRgb(
                (byte)Math.Min(255, c.R + 40),
                (byte)Math.Min(255, c.G + 40),
                (byte)Math.Min(255, c.B + 40)));
        RootBorder.Background = new SolidColorBrush(
            Color.FromRgb(
                (byte)(c.R * 0.3),
                (byte)(c.G * 0.3),
                (byte)(c.B * 0.3)));
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return; // allow double-click in title box
        e.Handled = true;
        // Drag is handled by GraphCanvas via Canvas position — raise a drag event
        // We use a simple approach: capture and move via parent canvas
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
            HeaderBorder.MouseMove -= OnMove;
            HeaderBorder.MouseLeftButtonUp -= OnUp;
        }

        HeaderBorder.MouseMove += OnMove;
        HeaderBorder.MouseLeftButtonUp += OnUp;
        HeaderBorder.CaptureMouse();
    }

    private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        // Cycle through preset colors
        int idx = Array.IndexOf(PresetColors, _vm.HeaderColor);
        _vm.HeaderColor = PresetColors[(idx + 1) % PresetColors.Length];
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        _isPreview = !_isPreview;
        if (_isPreview)
        {
            MdViewer.Markdown = _vm.Body;
            BodyEdit.Visibility    = Visibility.Collapsed;
            BodyPreview.Visibility = Visibility.Visible;
            ((TextBlock)((Button)sender).Content).Text = "\u270F";
        }
        else
        {
            BodyEdit.Visibility    = Visibility.Visible;
            BodyPreview.Visibility = Visibility.Collapsed;
            ((TextBlock)((Button)sender).Content).Text = "\u2B1B";
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
        Width  = newW;
        Height = newH;
        _vm.Width  = newW;
        _vm.Height = newH;
    }
}
