# TextureMaker

A node-based procedural texture editor for Windows, built with WPF and .NET 8.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Visual node graph** — connect nodes by dragging wires between pins
- **Live preview** — every node shows a real-time thumbnail of its output
- **13+ node types** across sources, filters, combine, and special categories
- **Save / Load projects** — `.txmk` JSON format preserves the full graph
- **Canvas navigation** — zoom with `Ctrl+Scroll`, pan with scroll wheel or middle-button drag
- **Real-time file monitoring** — ImageLoad node highlights red if the source file is deleted or renamed

## Nodes

### Sources
| Node | Description |
|---|---|
| **Image Load** | Load any image from disk (PNG, JPG, etc.) |
| **Solid Color** | Generate a flat color fill at any resolution |
| **Noise** | Perlin noise with scale, octaves and persistence |
| **Gradient** | Linear gradient in 4 directions |
| **Root Folder** | Set a base path for relative file references |

### Filters
| Node | Description |
|---|---|
| **Blur** | Gaussian blur with adjustable radius |
| **Sharpen** | Edge sharpening |
| **Brightness / Contrast** | Adjust luminance and contrast |

### Combine
| Node | Description |
|---|---|
| **Blend** | Mix two textures: Normal, Multiply, Add, Screen, Overlay |
| **Mask** | Apply a grayscale mask to a texture |
| **Composite** | Overlay one image on another with X/Y offset and opacity |

### Special
| Node | Description |
|---|---|
| **Invert** | Invert RGB channels |
| **Levels** | Black/white point and gamma correction |
| **Normal Map** | Generate a normal map from a height map |

### Output
| Node | Description |
|---|---|
| **Save** | Export the result to PNG, JPG, BMP, or TGA |

## Controls

| Action | Input |
|---|---|
| Zoom in / out | `Ctrl + Scroll` |
| Pan vertical | `Scroll` |
| Pan horizontal | `Shift + Scroll` |
| Pan free | Middle-button drag |
| Connect pins | Drag from output pin to input pin |
| Delete selected node | `Delete` |
| New project | `Ctrl + N` |
| Open project | `Ctrl + O` |
| Save project | `Ctrl + S` |

## Tech Stack

- **WPF** on **.NET 8** (`net8.0-windows`)
- [ReactiveUI.WPF](https://www.reactiveui.net/) 20.1.1 — MVVM + reactive bindings
- [DynamicData](https://github.com/reactivemarbles/DynamicData) 8.4.1 — reactive collections
- [SixLabors.ImageSharp](https://sixlabors.com/products/imagesharp/) 3.1.5 — image processing
- Custom node graph engine (no third-party graph library)

## Build & Run

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows.

```bash
git clone https://github.com/aLegoz/TextureMaker.git
cd TextureMaker
dotnet run --project TextureMaker/TextureMaker.csproj
```
