using TextureMaker.Graph;
using TextureMaker.Nodes.Combine;
using TextureMaker.Nodes.Filters;
using TextureMaker.Nodes.Logic;
using TextureMaker.Nodes.Output;
using TextureMaker.Nodes.Sources;
using TextureMaker.Nodes.Special;

namespace TextureMaker.Core;

public static class NodeFactory
{
    public static GraphNodeViewModel? Create(string key) => key switch
    {
        "RootFolder"         => new RootFolderNodeViewModel(),
        "ImageLoad"          => new ImageLoadNodeViewModel(),
        "Color"              => new ColorNodeViewModel(),
        "SolidColor"         => new SolidColorNodeViewModel(),
        "Gradient"           => new GradientNodeViewModel(),
        "Noise"              => new NoiseNodeViewModel(),
        "Toggle"             => new ToggleNodeViewModel(),
        "Gate"               => new GateNodeViewModel(),
        "Blur"               => new BlurNodeViewModel(),
        "Sharpen"            => new SharpenNodeViewModel(),
        "BrightnessContrast" => new BrightnessContrastNodeViewModel(),
        "Blend"              => new BlendNodeViewModel(),
        "Mask"               => new MaskNodeViewModel(),
        "Composite"          => new CompositeNodeViewModel(),
        "NormalMap"          => new NormalMapNodeViewModel(),
        "Invert"             => new InvertNodeViewModel(),
        "Levels"             => new LevelsNodeViewModel(),
        "Switch"             => new SwitchNodeViewModel(),
        "Save"               => new SaveNodeViewModel(),
        _                    => null
    };
}
