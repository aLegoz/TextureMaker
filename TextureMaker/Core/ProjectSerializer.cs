using System.IO;
using System.Text.Json;
using System.Windows;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;
using TextureMaker.Nodes.Combine;
using TextureMaker.Nodes.Filters;
using TextureMaker.Nodes.Logic;
using TextureMaker.Nodes.Output;
using TextureMaker.Nodes.Sources;
using TextureMaker.Nodes.Special;
using WpfColor = System.Windows.Media.Color;

namespace TextureMaker.Core;

public static class ProjectSerializer
{
    private record NodeEntry(int Id, string Type, double X, double Y, JsonElement Props);
    private record ConnectionEntry(int OutNodeId, string OutPinName, int InNodeId, string InPinName);
    private record ProjectFile(List<NodeEntry> Nodes, List<ConnectionEntry> Connections);

    private static readonly JsonSerializerOptions s_opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // ── Save ──────────────────────────────────────────────────────────

    public static void Save(GraphViewModel graph, string path)
    {
        var nodeIndex = new Dictionary<GraphNodeViewModel, int>();
        var nodes = new List<NodeEntry>();

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            var node = graph.Nodes[i];
            nodeIndex[node] = i;
            nodes.Add(new NodeEntry(i, NodeTypeKey(node), node.Position.X, node.Position.Y, BuildProps(node)));
        }

        var connections = new List<ConnectionEntry>();
        foreach (var conn in graph.Connections)
        {
            var outNode = graph.Nodes.FirstOrDefault(n => n.AllPins.Contains(conn.OutputPin));
            var inNode  = graph.Nodes.FirstOrDefault(n => n.AllPins.Contains(conn.InputPin));
            if (outNode == null || inNode == null) continue;
            if (!nodeIndex.TryGetValue(outNode, out int outId) || !nodeIndex.TryGetValue(inNode, out int inId)) continue;
            connections.Add(new ConnectionEntry(outId, conn.OutputPin.Name, inId, conn.InputPin.Name));
        }

        File.WriteAllText(path, JsonSerializer.Serialize(new ProjectFile(nodes, connections), s_opts));
    }

    // ── Load ──────────────────────────────────────────────────────────

    public static GraphViewModel Load(string path)
    {
        var project = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(path), s_opts)!;
        var graph = new GraphViewModel();
        var nodeList = new List<GraphNodeViewModel?>();

        foreach (var entry in project.Nodes)
        {
            var node = NodeFactory.Create(entry.Type);
            if (node == null) { nodeList.Add(null); continue; }
            node.Position = new Point(entry.X, entry.Y);
            ApplyProps(node, entry.Props);
            graph.Nodes.Add(node);
            nodeList.Add(node);
        }

        foreach (var conn in project.Connections)
        {
            if (conn.OutNodeId >= nodeList.Count || conn.InNodeId >= nodeList.Count) continue;
            var outNode = nodeList[conn.OutNodeId];
            var inNode  = nodeList[conn.InNodeId];
            if (outNode == null || inNode == null) continue;
            var connVm = ConnectPins(outNode, conn.OutPinName, inNode, conn.InPinName);
            if (connVm != null) graph.Connections.Add(connVm);
        }

        return graph;
    }

    // ── Node type key ─────────────────────────────────────────────────

    private static string NodeTypeKey(GraphNodeViewModel node) => node switch
    {
        RootFolderNodeViewModel         => "RootFolder",
        ImageLoadNodeViewModel          => "ImageLoad",
        ColorNodeViewModel              => "Color",
        SolidColorNodeViewModel         => "SolidColor",
        GradientNodeViewModel           => "Gradient",
        NoiseNodeViewModel              => "Noise",
        ToggleNodeViewModel             => "Toggle",
        AndNodeViewModel                => "And",
        OrNodeViewModel                 => "Or",
        GateNodeViewModel               => "Gate",
        BlurNodeViewModel               => "Blur",
        SharpenNodeViewModel            => "Sharpen",
        BrightnessContrastNodeViewModel => "BrightnessContrast",
        BlendNodeViewModel              => "Blend",
        MaskNodeViewModel               => "Mask",
        CompositeNodeViewModel          => "Composite",
        NormalMapNodeViewModel          => "NormalMap",
        InvertNodeViewModel             => "Invert",
        LevelsNodeViewModel             => "Levels",
        SwitchNodeViewModel             => "Switch",
        SaveNodeViewModel               => "Save",
        _                               => "Unknown"
    };

    // ── Props serialization ───────────────────────────────────────────

    private static JsonElement BuildProps(GraphNodeViewModel node)
    {
        object props = node switch
        {
            RootFolderNodeViewModel n         => new { folderPath = n.FolderPath },
            ImageLoadNodeViewModel n          => new { filePath = n.FilePath },
            ColorNodeViewModel n              => new { color = ToHex(n.SelectedColor) },
            SolidColorNodeViewModel n         => new { color = ToHex(n.SelectedColor), width = n.Width, height = n.Height },
            GradientNodeViewModel n           => new { colorA = ToHex(n.ColorA), colorB = ToHex(n.ColorB), direction = (int)n.Direction, width = n.Width, height = n.Height },
            NoiseNodeViewModel n              => new { scale = n.Scale, octaves = n.Octaves, seed = n.Seed, width = n.Width, height = n.Height },
            ToggleNodeViewModel n             => new { value = n.IsActive },
            AndNodeViewModel _               => (object)new { },
            OrNodeViewModel _                => (object)new { },
            GateNodeViewModel n               => new { active = n.IsActive },
            BlurNodeViewModel n               => new { sigma = n.Sigma },
            SharpenNodeViewModel n            => new { amount = n.Amount, radius = n.Radius },
            BrightnessContrastNodeViewModel n => new { brightness = n.Brightness, contrast = n.Contrast },
            BlendNodeViewModel n              => new { mode = (int)n.Mode, opacity = n.Opacity },
            CompositeNodeViewModel n          => new { offsetX = n.OffsetX, offsetY = n.OffsetY, opacity = n.Opacity },
            NormalMapNodeViewModel n          => new { strength = n.Strength },
            LevelsNodeViewModel n             => new { inBlack = n.InBlack, inWhite = n.InWhite, gamma = n.Gamma, outBlack = n.OutBlack, outWhite = n.OutWhite },
            SwitchNodeViewModel n             => new { active = n.IsActive },
            SaveNodeViewModel n               => new { fileName = n.FileName },
            _                                 => (object)new { }
        };
        return JsonSerializer.SerializeToElement(props);
    }

    // ── Props deserialization ─────────────────────────────────────────

    private static void ApplyProps(GraphNodeViewModel node, JsonElement p)
    {
        switch (node)
        {
            case RootFolderNodeViewModel n:
                n.FolderPath = Str(p, "folderPath") ?? "";
                break;
            case ImageLoadNodeViewModel n:
                n.FilePath = Str(p, "filePath");
                break;
            case ColorNodeViewModel n:
                if (Str(p, "color") is { } cc) n.SelectedColor = FromHex(cc);
                break;
            case SolidColorNodeViewModel n:
                if (Str(p, "color") is { } c) n.SelectedColor = FromHex(c);
                n.Width  = Int(p, "width",  n.Width);
                n.Height = Int(p, "height", n.Height);
                break;
            case GradientNodeViewModel n:
                if (Str(p, "colorA") is { } ca) n.ColorA = FromHex(ca);
                if (Str(p, "colorB") is { } cb) n.ColorB = FromHex(cb);
                n.Direction = (GradientDirection)Int(p, "direction", (int)n.Direction);
                n.Width  = Int(p, "width",  n.Width);
                n.Height = Int(p, "height", n.Height);
                break;
            case NoiseNodeViewModel n:
                n.Scale   = Flt(p, "scale",   n.Scale);
                n.Octaves = Int(p, "octaves", n.Octaves);
                n.Seed    = Int(p, "seed",    n.Seed);
                n.Width   = Int(p, "width",   n.Width);
                n.Height  = Int(p, "height",  n.Height);
                break;
            case BlurNodeViewModel n:
                n.Sigma = Flt(p, "sigma", n.Sigma);
                break;
            case SharpenNodeViewModel n:
                n.Amount = Flt(p, "amount", n.Amount);
                n.Radius = Flt(p, "radius", n.Radius);
                break;
            case BrightnessContrastNodeViewModel n:
                n.Brightness = Flt(p, "brightness", n.Brightness);
                n.Contrast   = Flt(p, "contrast",   n.Contrast);
                break;
            case BlendNodeViewModel n:
                n.Mode    = (BlendMode)Int(p, "mode",    (int)n.Mode);
                n.Opacity = Flt(p,         "opacity", n.Opacity);
                break;
            case CompositeNodeViewModel n:
                n.OffsetX  = Int(p, "offsetX",  n.OffsetX);
                n.OffsetY  = Int(p, "offsetY",  n.OffsetY);
                n.Opacity  = Flt(p, "opacity",  n.Opacity);
                break;
            case NormalMapNodeViewModel n:
                n.Strength = Flt(p, "strength", n.Strength);
                break;
            case LevelsNodeViewModel n:
                n.InBlack  = Flt(p, "inBlack",  n.InBlack);
                n.InWhite  = Flt(p, "inWhite",  n.InWhite);
                n.Gamma    = Flt(p, "gamma",    n.Gamma);
                n.OutBlack = Flt(p, "outBlack", n.OutBlack);
                n.OutWhite = Flt(p, "outWhite", n.OutWhite);
                break;
            case ToggleNodeViewModel n:
                n.IsActive = Bool(p, "value", false);
                break;
            case AndNodeViewModel:
            case OrNodeViewModel:
                break;
            case GateNodeViewModel n:
                n.IsActive = Bool(p, "active", false);
                break;
            case SwitchNodeViewModel n:
                n.IsActive = Bool(p, "active", false);
                break;
            case SaveNodeViewModel n:
                n.FileName = Str(p, "fileName") ?? "";
                break;
        }
    }

    // ── Connection wiring ─────────────────────────────────────────────

    private static ConnectionViewModel? ConnectPins(
        GraphNodeViewModel outNode, string outPinName,
        GraphNodeViewModel inNode,  string inPinName)
    {
        var outPinVm = outNode.AllPins.FirstOrDefault(p =>  p.IsOutput && p.Name == outPinName);
        var inPinVm  = inNode.AllPins.FirstOrDefault(p => !p.IsOutput && p.Name == inPinName);
        if (outPinVm == null || inPinVm == null) return null;

        bool connected = false;

        if (outNode is TextureNodeViewModel tn)
        {
            var pin = FindTexPin(inNode, inPinVm);
            if (pin != null)
            {
                var fake = new OutputPin<TextureData> { Name = outPinName, Value = tn.Output.Value };
                pin.Connect(fake);
                connected = true;
            }
        }
        else if (outNode is RootFolderNodeViewModel rfn)
        {
            var pin = FindStrPin(inNode, inPinVm);
            if (pin != null)
            {
                var fake = new OutputPin<string> { Name = outPinName, Value = rfn.FolderOutput.Value };
                pin.Connect(fake);
                connected = true;
            }
        }
        else if (outNode is ColorNodeViewModel cn)
        {
            var pin = FindColorPin(inNode, inPinVm);
            if (pin != null)
            {
                var fake = new OutputPin<WpfColor> { Name = outPinName, Value = cn.ColorOutput.Value };
                pin.Connect(fake);
                connected = true;
            }
        }
        else if (outNode is ToggleNodeViewModel tog)
        {
            var pin = FindBoolPin(inNode, inPinVm);
            if (pin != null)
            {
                var fake = new OutputPin<bool> { Name = outPinName, Value = tog.BoolOutput.Value };
                pin.Connect(fake);
                connected = true;
            }
        }
        else if (outNode is AndNodeViewModel an)
        {
            var pin = FindBoolPin(inNode, inPinVm);
            if (pin != null)
            {
                var fake = new OutputPin<bool> { Name = outPinName, Value = an.BoolOutput.Value };
                pin.Connect(fake);
                connected = true;
            }
        }
        else if (outNode is OrNodeViewModel on)
        {
            var pin = FindBoolPin(inNode, inPinVm);
            if (pin != null)
            {
                var fake = new OutputPin<bool> { Name = outPinName, Value = on.BoolOutput.Value };
                pin.Connect(fake);
                connected = true;
            }
        }

        if (!connected) return null;

        outPinVm.IsConnected = true;
        return new ConnectionViewModel(outPinVm, inPinVm);
    }

    private static InputPin<TextureData>? FindTexPin(GraphNodeViewModel n, PinViewModel vm) => n switch
    {
        BlurNodeViewModel x               => Chk(x.InputTexture, vm),
        SharpenNodeViewModel x            => Chk(x.InputTexture, vm),
        BrightnessContrastNodeViewModel x => Chk(x.InputTexture, vm),
        BlendNodeViewModel x              => Chk(x.InputA, vm) ?? Chk(x.InputB, vm),
        CompositeNodeViewModel x          => Chk(x.InputBase, vm) ?? Chk(x.InputOverlay, vm),
        MaskNodeViewModel x               => Chk(x.InputTexture, vm) ?? Chk(x.InputMask, vm),
        NormalMapNodeViewModel x          => Chk(x.InputTexture, vm),
        InvertNodeViewModel x             => Chk(x.InputTexture, vm),
        LevelsNodeViewModel x             => Chk(x.InputTexture, vm),
        GateNodeViewModel x               => Chk(x.InputTexture, vm),
        SwitchNodeViewModel x             => Chk(x.IfTrue, vm) ?? Chk(x.IfFalse, vm),
        SaveNodeViewModel x               => Chk(x.InputTexture, vm),
        _                                 => null
    };

    private static InputPin<string>? FindStrPin(GraphNodeViewModel n, PinViewModel vm) => n switch
    {
        ImageLoadNodeViewModel x => ChkStr(x.RootFolder, vm),
        SaveNodeViewModel x      => ChkStr(x.RootFolder, vm),
        _                        => null
    };

    private static InputPin<WpfColor>? FindColorPin(GraphNodeViewModel n, PinViewModel vm) => n switch
    {
        SolidColorNodeViewModel x => ChkColor(x.ColorInput, vm),
        _                         => null
    };

    private static InputPin<WpfColor>? ChkColor(InputPin<WpfColor> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    private static InputPin<bool>? FindBoolPin(GraphNodeViewModel n, PinViewModel vm) => n switch
    {
        GateNodeViewModel x   => ChkBool(x.ConditionPin, vm),
        SwitchNodeViewModel x => ChkBool(x.ConditionPin, vm),
        AndNodeViewModel x    => ChkBool(x.InputA, vm) ?? ChkBool(x.InputB, vm),
        OrNodeViewModel x     => ChkBool(x.InputA, vm) ?? ChkBool(x.InputB, vm),
        _                     => null
    };

    private static InputPin<bool>? ChkBool(InputPin<bool> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    private static InputPin<TextureData>? Chk(InputPin<TextureData> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    private static InputPin<string>? ChkStr(InputPin<string> pin, PinViewModel vm)
        => pin.ViewModel == vm ? pin : null;

    // ── Color helpers ─────────────────────────────────────────────────

    private static string ToHex(WpfColor c) => $"{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    private static WpfColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) return System.Windows.Media.Colors.Gray;
        return WpfColor.FromArgb(
            Convert.ToByte(hex[0..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16),
            Convert.ToByte(hex[6..8], 16));
    }

    // ── JSON helpers ──────────────────────────────────────────────────

    private static string? Str(JsonElement el, string key)
        => el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int Int(JsonElement el, string key, int fallback = 0)
        => el.TryGetProperty(key, out var p) && p.TryGetInt32(out int v) ? v : fallback;

    private static float Flt(JsonElement el, string key, float fallback = 0f)
        => el.TryGetProperty(key, out var p) && p.TryGetSingle(out float v) ? v : fallback;

    private static bool Bool(JsonElement el, string key, bool fallback = false)
        => el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.True ? true
         : el.TryGetProperty(key, out p)     && p.ValueKind == JsonValueKind.False ? false
         : fallback;
}
