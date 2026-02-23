using TextureMaker.Graph;
using TextureMaker.Nodes.Combine;
using TextureMaker.Nodes.Filters;
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
        "SolidColor"         => new SolidColorNodeViewModel(),
        "Gradient"           => new GradientNodeViewModel(),
        "Noise"              => new NoiseNodeViewModel(),
        "Blur"               => new BlurNodeViewModel(),
        "Sharpen"            => new SharpenNodeViewModel(),
        "BrightnessContrast" => new BrightnessContrastNodeViewModel(),
        "Blend"              => new BlendNodeViewModel(),
        "Mask"               => new MaskNodeViewModel(),
        "Composite"          => new CompositeNodeViewModel(),
        "NormalMap"          => new NormalMapNodeViewModel(),
        "Invert"             => new InvertNodeViewModel(),
        "Levels"             => new LevelsNodeViewModel(),
        "Save"               => new SaveNodeViewModel(),
        _                    => null
    };
}
