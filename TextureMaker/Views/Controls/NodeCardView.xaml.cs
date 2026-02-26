using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ReactiveUI;
using SixLabors.ImageSharp.Processing;
using ISResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;
using TextureMaker.Nodes.Combine;
using TextureMaker.Nodes.Filters;
using TextureMaker.Nodes.Output;
using TextureMaker.Nodes.Sources;
using TextureMaker.Nodes.Special;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace TextureMaker.Views.Controls;

public partial class NodeCardView : UserControl
{
    private readonly GraphNodeViewModel _node;
    private bool _dragging;
    private Point _dragOffset;
    private readonly Dictionary<PinViewModel, Ellipse> _pinSockets = new();

    public event Action<NodeCardView, PinViewModel, Point>? PinDragStarted;
    public event Action<NodeCardView, PinViewModel, Point>? PinDropped;
    public event Action<GraphNodeViewModel>? NodeSelected;
    public event Action<GraphNodeViewModel>? NodeDeleteRequested;

    public NodeCardView(GraphNodeViewModel node)
    {
        InitializeComponent();
        _node = node;
        DataContext = node;
        BuildPins(node);
        AddHandler(MouseLeftButtonDownEvent,
            new MouseButtonEventHandler((_, _) => NodeSelected?.Invoke(_node)),
            handledEventsToo: true);

        // Thumbnail preview
        IObservable<TextureData?>? outputObs = node switch
        {
            TextureNodeViewModel tn => tn.Output.Value!,
            SaveNodeViewModel sn    => sn.InputTexture.Value,
            _                       => null
        };
        if (outputObs != null)
        {
            ThumbBorder.Visibility = Visibility.Visible;
            outputObs
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(tex => tex == null ? ((BitmapSource?)null, false) : MakeThumb(tex))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(result =>
                {
                    ThumbImage.Source = result.Item1;
                    RenderOptions.SetBitmapScalingMode(ThumbImage,
                        result.Item2 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
                });
        }
    }

    private static (BitmapSource? Bmp, bool IsSmall) MakeThumb(TextureData tex)
    {
        try
        {
            const int maxDim = 120;
            if (tex.Image.Width <= maxDim && tex.Image.Height <= maxDim)
            {
                // Small image — pixel-perfect, no resize
                return (ImageConverter.ToBitmapSource(tex.Image), true);
            }
            else
            {
                // Large image — smooth downscale
                using var small = tex.Image.Clone(ctx =>
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(maxDim, maxDim),
                        Mode = ISResizeMode.Max,
                        Sampler = SixLabors.ImageSharp.Processing.KnownResamplers.Lanczos3
                    }));
                return (ImageConverter.ToBitmapSource(small), false);
            }
        }
        catch { return (null, false); }
    }

    private void BuildPins(GraphNodeViewModel node)
    {
        foreach (var pin in node.AllPins)
        {
            var socket = CreateSocket(pin);
            _pinSockets[pin] = socket;
            var wrapper = WrapSocket(socket, pin.Name, pin.IsOutput);
            if (pin.IsOutput) OutputPinsPanel.Children.Add(wrapper);
            else              InputPinsPanel.Children.Add(wrapper);
        }
    }

    private Ellipse CreateSocket(PinViewModel pin)
    {
        var normalFill = PinFill(pin);
        var e = new Ellipse
        {
            Width = 12, Height = 12,
            Fill = normalFill,
            Stroke = Brushes.White, StrokeThickness = 1,
            Cursor = Cursors.Cross, Tag = pin
        };
        e.MouseLeftButtonDown += Socket_MouseDown;
        e.MouseLeftButtonUp   += Socket_MouseUp;
        e.MouseEnter += (_, _) => e.Fill = Brushes.Yellow;
        e.MouseLeave += (_, _) => e.Fill = PinFill(pin);
        return e;
    }

    private static Brush PinFill(PinViewModel pin) => pin.PinType switch
    {
        "color"  => new SolidColorBrush(Color.FromRgb(80, 200, 100)),
        "folder" => Brushes.LightSteelBlue,
        "bool"   => new SolidColorBrush(Color.FromRgb(180, 100, 210)),
        _        => Brushes.Orange,
    };

    private static StackPanel WrapSocket(Ellipse socket, string name, bool isOutput)
    {
        var label = new TextBlock
        {
            Text = name, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)), FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(isOutput ? 0 : 3, 0, isOutput ? 3 : 0, 0)
        };
        var sp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 1, 0, 1),
            HorizontalAlignment = isOutput ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };
        if (isOutput) { sp.Children.Add(label); sp.Children.Add(socket); }
        else          { sp.Children.Add(socket); sp.Children.Add(label); }
        return sp;
    }

    private void Socket_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is PinViewModel pin)
        {
            e.Handled = true;
            PinDragStarted?.Invoke(this, pin, el.PointToScreen(new Point(el.Width / 2, el.Height / 2)));
        }
    }

    private void Socket_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is PinViewModel pin)
        {
            e.Handled = true;
            PinDropped?.Invoke(this, pin, el.PointToScreen(new Point(el.Width / 2, el.Height / 2)));
        }
    }

    // ── Node drag ─────────────────────────────────────────────────────
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        NodeDeleteRequested?.Invoke(_node);
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        NodeSelected?.Invoke(_node);
        _dragging = true;
        if (VisualParent is not Canvas parent) return;
        _dragOffset = e.GetPosition(parent);
        _dragOffset.X -= Canvas.GetLeft(this);
        _dragOffset.Y -= Canvas.GetTop(this);
        Header.CaptureMouse();
        e.Handled = true;
    }

    private void Header_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || VisualParent is not Canvas parent) return;
        var pos = e.GetPosition(parent);
        _node.Position = new Point(pos.X - _dragOffset.X, pos.Y - _dragOffset.Y);
    }

    private void Header_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        Header.ReleaseMouseCapture();
    }

    // ── Pin helpers ───────────────────────────────────────────────────
    public bool ContainsPin(PinViewModel pin) => _pinSockets.ContainsKey(pin);

    public Point? GetPinCenter(PinViewModel pin, UIElement relativeTo)
    {
        if (!_pinSockets.TryGetValue(pin, out var socket) || !socket.IsLoaded) return null;
        // Use screen coordinates as intermediary — works even when relativeTo is a sibling, not ancestor
        var screenPt = socket.PointToScreen(new Point(socket.Width / 2, socket.Height / 2));
        return relativeTo.PointFromScreen(screenPt);
    }

    public bool TryConnectDataPin(PinViewModel outPinVm, NodeCardView targetCard, PinViewModel inPinVm)
    {
        if (ReferenceEquals(this, targetCard)) return false;
        return TryConnect(_node, outPinVm, targetCard._node, inPinVm)
        || TryConnect(targetCard._node, inPinVm, _node, outPinVm)
        || TryConnectString(_node, outPinVm, targetCard._node, inPinVm)
        || TryConnectString(targetCard._node, inPinVm, _node, outPinVm)
        || TryConnectColor(_node, outPinVm, targetCard._node, inPinVm)
        || TryConnectColor(targetCard._node, inPinVm, _node, outPinVm)
        || TryConnectBool(_node, outPinVm, targetCard._node, inPinVm)
        || TryConnectBool(targetCard._node, inPinVm, _node, outPinVm);
    }

    // ── TextureData connections ───────────────────────────────────────
    private static bool TryConnect(
        GraphNodeViewModel outNode, PinViewModel outPinVm,
        GraphNodeViewModel inNode,  PinViewModel inPinVm)
    {
        if (!outPinVm.IsOutput || inPinVm.IsOutput) return false;
        if (outNode is not TextureNodeViewModel tn) return false;

        var inputPin = FindInputPin(inNode, inPinVm);
        if (inputPin == null) return false;

        var fakeOut = new OutputPin<TextureData>
        {
            Name = outPinVm.Name,
            Value = tn.Output.Value
        };
        fakeOut.ViewModel.IsConnected = true;
        inputPin.Connect(fakeOut);
        return true;
    }

    // ── String (root folder) connections ─────────────────────────────
    private static bool TryConnectString(
        GraphNodeViewModel outNode, PinViewModel outPinVm,
        GraphNodeViewModel inNode,  PinViewModel inPinVm)
    {
        if (!outPinVm.IsOutput || inPinVm.IsOutput) return false;
        if (outNode is not RootFolderNodeViewModel rfn) return false;

        var inputPin = FindStringInputPin(inNode, inPinVm);
        if (inputPin == null) return false;

        var fakeOut = new OutputPin<string> { Name = outPinVm.Name, Value = rfn.FolderOutput.Value };
        fakeOut.ViewModel.IsConnected = true;
        inputPin.Connect(fakeOut);
        return true;
    }

    /// <summary>Called by GraphCanvas when a source node is deleted, to reset connected input pins.</summary>
    public static void DisconnectDataPin(GraphNodeViewModel node, PinViewModel pinVm)
    {
        FindInputPin(node, pinVm)?.Disconnect();
        FindStringInputPin(node, pinVm)?.Disconnect();
        FindColorInputPin(node, pinVm)?.Disconnect();
        FindBoolInputPin(node, pinVm)?.Disconnect();
    }

    private static InputPin<string>? FindStringInputPin(GraphNodeViewModel node, PinViewModel vm) => node switch
    {
        ImageLoadNodeViewModel n => MatchStr(n.RootFolder, vm),
        SaveNodeViewModel n      => MatchStr(n.RootFolder, vm),
        _                        => null
    };

    private static InputPin<string>? MatchStr(InputPin<string> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    private static InputPin<TextureData>? FindInputPin(GraphNodeViewModel node, PinViewModel vm) => node switch
    {
        BlurNodeViewModel n               => Match(n.InputTexture, vm),
        SharpenNodeViewModel n            => Match(n.InputTexture, vm),
        BrightnessContrastNodeViewModel n => Match(n.InputTexture, vm),
        BlendNodeViewModel n              => Match(n.InputA, vm) ?? Match(n.InputB, vm),
        CompositeNodeViewModel n          => Match(n.InputBase, vm) ?? Match(n.InputOverlay, vm),
        MaskNodeViewModel n               => Match(n.InputTexture, vm) ?? Match(n.InputMask, vm),
        NormalMapNodeViewModel n          => Match(n.InputTexture, vm),
        InvertNodeViewModel n             => Match(n.InputTexture, vm),
        LevelsNodeViewModel n             => Match(n.InputTexture, vm),
        SaveNodeViewModel n               => Match(n.InputTexture, vm),
        SwitchNodeViewModel n             => Match(n.IfTrue, vm) ?? Match(n.IfFalse, vm),
        _                                 => null
    };

    private static InputPin<TextureData>? Match(InputPin<TextureData> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    // ── WpfColor connections ──────────────────────────────────────────
    private static bool TryConnectColor(
        GraphNodeViewModel outNode, PinViewModel outPinVm,
        GraphNodeViewModel inNode,  PinViewModel inPinVm)
    {
        if (!outPinVm.IsOutput || inPinVm.IsOutput) return false;
        if (outNode is not ColorNodeViewModel cn) return false;

        var inputPin = FindColorInputPin(inNode, inPinVm);
        if (inputPin == null) return false;

        var fakeOut = new OutputPin<WpfColor>
        {
            Name  = outPinVm.Name,
            Value = cn.ColorOutput.Value
        };
        fakeOut.ViewModel.IsConnected = true;
        inputPin.Connect(fakeOut);
        return true;
    }

    private static InputPin<WpfColor>? FindColorInputPin(GraphNodeViewModel node, PinViewModel vm) => node switch
    {
        SolidColorNodeViewModel n => MatchColor(n.ColorInput, vm),
        _                         => null
    };

    private static InputPin<WpfColor>? MatchColor(InputPin<WpfColor> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    // ── Bool connections ──────────────────────────────────────────────
    private static bool TryConnectBool(
        GraphNodeViewModel outNode, PinViewModel outPinVm,
        GraphNodeViewModel inNode,  PinViewModel inPinVm)
    {
        if (!outPinVm.IsOutput || inPinVm.IsOutput) return false;
        if (outNode is not ToggleNodeViewModel tn) return false;

        var inputPin = FindBoolInputPin(inNode, inPinVm);
        if (inputPin == null) return false;

        var fakeOut = new OutputPin<bool> { Name = outPinVm.Name, Value = tn.BoolOutput.Value };
        fakeOut.ViewModel.IsConnected = true;
        inputPin.Connect(fakeOut);
        return true;
    }

    private static InputPin<bool>? FindBoolInputPin(GraphNodeViewModel node, PinViewModel vm) => node switch
    {
        SwitchNodeViewModel n => MatchBool(n.ConditionPin, vm),
        _                     => null
    };

    private static InputPin<bool>? MatchBool(InputPin<bool> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;
}
