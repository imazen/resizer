# Watermark Configuration Documentation

## Overview

This document describes the XML configuration format for the ImageResizer watermark plugin and how it maps to the Imageflow.NET WatermarkOptions API.

## XML Configuration Structure

### Root Element

```xml
<watermarks defaultImageQuery="scache=true">
  <!-- watermark definitions go here -->
</watermarks>
```

**Attributes**:
- `defaultImageQuery` - Default preprocessing settings for all image watermarks (default: "scache=true")

### Configuration Elements

#### 1. Image Layers

```xml
<image name="watermark1" 
       path="~/watermarks/logo.png" 
       imageQuery="alpha=0.5&amp;width=100" 
       left="10px" 
       top="10px" 
       width="100px" 
       height="50px" 
       relativeTo="image" 
       align="topleft" 
       drawAs="overlay" 
       fill="false" />
```

#### 2. Text Layers

```xml
<text name="copyright" 
      text="© #{year} Company Name" 
      font="Arial" 
      fontSize="14" 
      style="Bold" 
      color="ffffff" 
      outlineColor="000000" 
      outlineWidth="2" 
      glowColor="0000ff" 
      glowWidth="4" 
      angle="0" 
      vertical="false" 
      rendering="AntiAliasGridFit" 
      left="10px" 
      bottom="10px" 
      relativeTo="canvas" 
      align="bottomleft" />
```

#### 3. Layer Groups

```xml
<group name="composite">
  <image path="~/watermarks/background.png" drawAs="background" fill="true" />
  <text text="Watermarked" align="center" />
</group>
```

#### 4. Legacy OtherImages

```xml
<otherimages path="~/watermarks" 
             right="20" 
             bottom="20" 
             width="20" 
             height="20" />
```

## Attribute Reference

### Common Layer Attributes

| Attribute | Type | Description | Default | Maps to Imageflow |
|-----------|------|-------------|---------|-------------------|
| `name` | string | Unique identifier for the watermark | Required (except for otherimages) | N/A - used for lookup |
| `left` | DistanceUnit | Distance from left edge | null | WatermarkMargins.Left or WatermarkFitBox.X1 |
| `top` | DistanceUnit | Distance from top edge | null | WatermarkMargins.Top or WatermarkFitBox.Y1 |
| `right` | DistanceUnit | Distance from right edge | null | WatermarkMargins.Right or WatermarkFitBox.X2 |
| `bottom` | DistanceUnit | Distance from bottom edge | null | WatermarkMargins.Bottom or WatermarkFitBox.Y2 |
| `width` | DistanceUnit | Layer width | null | Affects FitMode |
| `height` | DistanceUnit | Layer height | null | Affects FitMode |
| `relativeTo` | string | Container reference | "image" | WatermarkAlign (Canvas/Image only) |
| `align` | ContentAlignment | Alignment within bounds | MiddleCenter | ConstraintGravity |
| `fill` | boolean | Fill container bounds | false | Affects FitMode |
| `drawAs` | string | "overlay" or "background" | "overlay" | Not supported in Imageflow |

### DistanceUnit Format
- Pixels: "20px" or "20" (default)
- Percentage: "20%", "20percent", "20pct"

### ContentAlignment Values
- TopLeft, TopCenter, TopRight
- MiddleLeft, MiddleCenter, MiddleRight
- BottomLeft, BottomCenter, BottomRight

### RelativeTo Values
- `image` - The image itself (default)
- `imageArea` - Image plus padding to maintain aspect ratio
- `canvas` - Final output dimensions
- `padding` - **Not supported in Imageflow**
- `border` - **Not supported in Imageflow**
- `margin` - **Not supported in Imageflow**

### Image Layer Specific Attributes

| Attribute | Type | Description | Maps to Imageflow |
|-----------|------|-------------|-------------------|
| `path` | string | Virtual path to watermark image | Source for watermark |
| `imageQuery` | string | Preprocessing commands | Applied before watermarking |

### Text Layer Specific Attributes

| Attribute | Type | Description | Default |
|-----------|------|-------------|---------|
| `text` | string | Text to display (supports #{key} placeholders) | "" |
| `font` | string | Font family name | GenericSansSerif |
| `fontSize` | int | Font size in pixels | 48 |
| `style` | FontStyle | Regular, Bold, Italic, etc. | Bold |
| `color` | string | Text color (hex) | Black |
| `outlineColor` | string | Outline color (hex) | White |
| `outlineWidth` | int | Outline width in pixels | 0 |
| `glowColor` | string | Glow effect color (hex) | null |
| `glowWidth` | int | Glow width in pixels | 0 |
| `angle` | double | Rotation angle in degrees | 0 |
| `vertical` | boolean | Display text vertically | false |
| `rendering` | TextRenderingHint | Text rendering quality | AntiAliasGridFit |

## Mapping to Imageflow WatermarkOptions

### Core Type Mapping

The XML configuration maps to these Imageflow.Fluent types:

```csharp
// Main watermark configuration
WatermarkOptions options = new WatermarkOptions();

// Position/layout configuration
IWatermarkConstraintBox fitBox; // Either WatermarkMargins or WatermarkFitBox
WatermarkConstraintMode fitMode;
ConstraintGravity gravity;

// Additional settings
ResampleHints hints = new ResampleHints();
```

### Position Mapping Logic

1. **All pixel values** → `WatermarkMargins`:
```csharp
// XML: left="10px" top="10px" right="20px" bottom="20px"
var margins = new WatermarkMargins()
{
    RelativeTo = WatermarkAlign.Image, // or Canvas
    Left = 10,
    Top = 10,
    Right = 20,
    Bottom = 20
};
```

2. **Any percentage values** → `WatermarkFitBox`:
```csharp
// XML: left="10%" top="10%" right="10%" bottom="10%"
var fitBox = new WatermarkFitBox()
{
    RelativeTo = WatermarkAlign.Image, // or Canvas
    X1 = 10,  // left percentage
    Y1 = 10,  // top percentage
    X2 = 90,  // 100 - right percentage
    Y2 = 90   // 100 - bottom percentage
};
```

3. **FitMode Determination**:
```csharp
WatermarkConstraintMode GetFitMode(Layer layer)
{
    if (layer.Fill) return WatermarkConstraintMode.Fit;
    if (layer.Width != null || layer.Height != null) return WatermarkConstraintMode.Within;
    return WatermarkConstraintMode.Within; // default
}
```

4. **Gravity Mapping**:
```csharp
ConstraintGravity MapAlignment(ContentAlignment align)
{
    return align switch
    {
        ContentAlignment.TopLeft => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 0, Y = 0 } },
        ContentAlignment.TopCenter => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 50, Y = 0 } },
        ContentAlignment.TopRight => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 100, Y = 0 } },
        ContentAlignment.MiddleLeft => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 0, Y = 50 } },
        ContentAlignment.MiddleCenter => new ConstraintGravity { Center = new ConstraintGravityCenter() },
        ContentAlignment.MiddleRight => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 100, Y = 50 } },
        ContentAlignment.BottomLeft => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 0, Y = 100 } },
        ContentAlignment.BottomCenter => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 50, Y = 100 } },
        ContentAlignment.BottomRight => new ConstraintGravity { Percentage = new ConstraintGravityPercentage { X = 100, Y = 100 } },
        _ => new ConstraintGravity { Center = new ConstraintGravityCenter() }
    };
}
```

### ResampleHints Configuration

```csharp
// ImageResizer 4 compatible settings
var hints = new ResampleHints()
{
    DownFilter = InterpolationFilter.Mitchell,
    SharpenPercent = 15,
    InterpolationColorspace = ScalingFloatspace.Srgb,
    ResampleWhen = ResampleWhen.Size_Differs_Or_Sharpening_Requested,
    SharpenWhen = SharpenWhen.Downscaling
};
```

### Opacity and Preprocessing

```csharp
// Extract opacity from imageQuery
var imageQuery = new ResizeSettings(layer.ImageQuery);
if (imageQuery["alpha"] != null)
{
    // Convert alpha (0-1 or 0-255) to opacity (0-1)
    float alpha = ParseAlpha(imageQuery["alpha"]);
    options.Opacity = alpha;
}

// Preprocess watermark image with remaining imageQuery settings
var preprocessedWatermark = ProcessWatermarkImage(watermarkPath, imageQuery);
```

## Implementation Plan

### 1. Configuration Parser

```csharp
public class WatermarkConfigurationParser
{
    private readonly IIssueProvider issueProvider;
    private readonly List<IIssue> issues = new List<IIssue>();
    
    public Dictionary<string, WatermarkConfiguration> ParseWatermarks(Node watermarksNode)
    {
        var watermarks = new Dictionary<string, WatermarkConfiguration>();
        var defaultImageQuery = ParseDefaultImageQuery(watermarksNode);
        
        foreach (var node in watermarksNode.Children)
        {
            try
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "image":
                        ParseImageLayer(node, watermarks, defaultImageQuery);
                        break;
                    case "text":
                        ParseTextLayer(node); // Will add error issue
                        break;
                    case "group":
                        ParseGroup(node, watermarks, defaultImageQuery);
                        break;
                    case "otherimages":
                        ParseOtherImages(node, defaultImageQuery);
                        break;
                }
            }
            catch (Exception ex)
            {
                AddIssue(IssueSeverity.Error, $"Failed to parse {node.Name}: {ex.Message}");
            }
        }
        
        return watermarks;
    }
}
```

### 2. Watermark Configuration Model

```csharp
public class WatermarkConfiguration
{
    public string Name { get; set; }
    public List<WatermarkLayerConfig> Layers { get; set; } = new List<WatermarkLayerConfig>();
}

public class WatermarkLayerConfig
{
    public string Path { get; set; }
    public ResizeSettings PreprocessingQuery { get; set; }
    public WatermarkOptions Options { get; set; }
    public bool IsTextLayer { get; set; } // For future use
}
```

### 3. Diagnostic System

```csharp
public enum WatermarkIssueSeverity
{
    Error,      // Prevents processing
    Warning,    // Allows processing but may have unexpected results
    Info        // Informational only
}

public class WatermarkConfigIssue : IIssue
{
    public string Source => "WatermarkPlugin";
    public IssueSeverity Severity { get; set; }
    public string Summary { get; set; }
    public string Details { get; set; }
    public string AffectedNode { get; set; }
}

// Diagnostic methods
private void ValidateLayerConfig(Node node, Layer layer)
{
    // Text layer check
    if (node.Name.Equals("text", StringComparison.OrdinalIgnoreCase))
    {
        AddIssue(IssueSeverity.Error, 
            "Text watermarks are not supported in Imageflow",
            "Please convert text watermarks to image watermarks",
            node);
        return;
    }
    
    // Background layer check
    if (layer.DrawAs == Layer.LayerPlacement.Background)
    {
        AddIssue(IssueSeverity.Error,
            "Background watermarks are not supported in Imageflow",
            $"Layer '{layer.Name}' uses drawAs='background'",
            node);
    }
    
    // RelativeTo validation
    if (!IsValidRelativeTo(layer.RelativeTo))
    {
        AddIssue(IssueSeverity.Error,
            $"RelativeTo value '{layer.RelativeTo}' is not supported",
            "Only 'image' and 'canvas' are supported in Imageflow",
            node);
    }
    
    // Mixed units warning
    if (HasMixedUnits(layer))
    {
        AddIssue(IssueSeverity.Warning,
            "Mixing pixels and percentages in positioning",
            "This may produce unexpected results when mapping to Imageflow",
            node);
    }
    
    // Complex positioning warning
    if (HasComplexPositioning(layer))
    {
        AddIssue(IssueSeverity.Warning,
            "Complex positioning detected",
            "Specifying all of left/right/width or top/bottom/height may not map precisely",
            node);
    }
}
```

### 4. Position Converter

```csharp
public class PositionConverter
{
    public (IWatermarkConstraintBox, List<IIssue>) ConvertPosition(Layer layer)
    {
        var issues = new List<IIssue>();
        
        // Determine if all values are pixels or if any are percentages
        var positions = new[] { layer.Left, layer.Top, layer.Right, layer.Bottom };
        var hasPercentages = positions.Any(p => p?.Type == DistanceUnit.Units.Percentage);
        var hasPixels = positions.Any(p => p?.Type == DistanceUnit.Units.Pixels);
        
        if (hasPercentages && hasPixels)
        {
            issues.Add(new WatermarkConfigIssue
            {
                Severity = IssueSeverity.Warning,
                Summary = "Mixed pixel and percentage units",
                Details = "Converting all values to percentages for Imageflow compatibility"
            });
        }
        
        if (hasPercentages || (!hasPixels && !hasPercentages))
        {
            return (ConvertToFitBox(layer), issues);
        }
        else
        {
            return (ConvertToMargins(layer), issues);
        }
    }
    
    private WatermarkFitBox ConvertToFitBox(Layer layer)
    {
        // Convert to percentage-based positioning
        var fitBox = new WatermarkFitBox
        {
            RelativeTo = MapRelativeTo(layer.RelativeTo)
        };
        
        // Calculate X1, Y1, X2, Y2 from layer positions
        // Handle null values appropriately
        // ...
        
        return fitBox;
    }
    
    private WatermarkMargins ConvertToMargins(Layer layer)
    {
        // Convert to pixel-based margins
        var margins = new WatermarkMargins
        {
            RelativeTo = MapRelativeTo(layer.RelativeTo)
        };
        
        // Extract pixel values
        // Handle null values appropriately
        // ...
        
        return margins;
    }
}
```

### 5. Runtime Adapter

```csharp
public class WatermarkAdapter
{
    private readonly WatermarkConfigurationParser parser;
    private readonly PositionConverter positionConverter;
    private readonly Dictionary<string, WatermarkConfiguration> configurations;
    
    public WatermarkAdapter(Config config)
    {
        parser = new WatermarkConfigurationParser();
        positionConverter = new PositionConverter();
        configurations = parser.ParseWatermarks(config.getConfigXml().queryFirst("watermarks"));
    }
    
    public List<WatermarkOptions> GetWatermarkOptions(string[] watermarkNames)
    {
        var options = new List<WatermarkOptions>();
        
        foreach (var name in watermarkNames)
        {
            if (configurations.TryGetValue(name, out var config))
            {
                options.AddRange(config.Layers.Select(l => l.Options));
            }
            else
            {
                // Handle legacy file-based watermark
                var legacyOptions = CreateLegacyWatermarkOptions(name);
                if (legacyOptions != null) options.Add(legacyOptions);
            }
        }
        
        return options;
    }
}
```

### 6. Integration with ImageflowBackendPlugin

```csharp
// In ImageflowBackendPlugin.cs
private void ApplyWatermarks(ImageJob imageflowJob, ImageState state)
{
    var watermarkNames = state.settings["watermark"]?.Split(',');
    if (watermarkNames == null || watermarkNames.Length == 0) return;
    
    var adapter = new WatermarkAdapter(this.c);
    var watermarkOptions = adapter.GetWatermarkOptions(watermarkNames);
    
    foreach (var options in watermarkOptions)
    {
        // Apply watermark using Imageflow.Fluent API
        imageflowJob.Watermark(watermarkSource, options);
    }
}
```

## Required Diagnostic Issues

### Error Issues (Prevent Processing)

1. **Missing name attribute**
   - Message: "The name attribute for each watermark or watermark group must be specified"
   - Applies to: `<image>`, `<text>`, `<group>`

2. **Duplicate name attribute**
   - Message: "Watermark name '{name}' is already defined"
   - Applies to: All named elements

3. **Invalid path**
   - Message: "Watermark image path '{path}' does not exist or is inaccessible"
   - Applies to: `<image>` elements

4. **Text layer usage**
   - Message: "Text watermarks are not supported in Imageflow. Please use image watermarks instead."
   - Applies to: `<text>` elements

5. **Unsupported relativeTo value**
   - Message: "RelativeTo value '{value}' is not supported. Only 'image' and 'canvas' are supported in Imageflow."
   - Applies to: padding, border, margin, imageArea

6. **Background watermarks**
   - Message: "Background watermarks (drawAs='background') are not supported in Imageflow"
   - Applies to: Any layer with drawAs="background"

### Warning Issues (Allow Processing)

1. **Mixed positioning units**
   - Message: "Mixing pixels and percentages in positioning may produce unexpected results"
   - When: Some position values are pixels, others are percentages

2. **Rotation on text**
   - Message: "Text rotation (angle={angle}) is ignored as text watermarks are not supported"
   - When: angle != 0 on text layer

3. **Complex positioning**
   - Message: "Complex positioning with all of left/right/width or top/bottom/height specified may not map precisely to Imageflow"
   - When: All three horizontal or vertical values are specified

4. **Ignored attributes**
   - Message: "Attribute '{attribute}' is ignored for image watermarks in Imageflow"
   - Applies to: Text-specific attributes on image layers

## Unsupported Features

### Text Layers (Future Exercise)

Text layers are documented here for reference but will generate an error when encountered:

```xml
<!-- This will generate an error in Imageflow -->
<text name="copyright" 
      text="© #{year} Company Name" 
      font="Arial" 
      fontSize="14" />
```

**Error generated**: "Text watermarks are not supported in Imageflow. Please use image watermarks instead."

**Workaround**: Pre-render text as images and use image layers instead.

### Background Layers

Background layers (`drawAs="background"`) are not supported in Imageflow:

```xml
<!-- This will generate an error -->
<image name="bg" path="~/bg.png" drawAs="background" />
```

**Error generated**: "Background watermarks (drawAs='background') are not supported in Imageflow"

**Workaround**: Apply background effects as a separate preprocessing step.

### Unsupported RelativeTo Values

Only `image` and `canvas` are supported. These will generate errors:
- `padding`
- `border` 
- `margin`
- `imageArea`

## Usage Example

```xml
<resizer>
  <watermarks defaultImageQuery="scache=true">
    <!-- Simple image watermark -->
    <image name="logo" 
           path="~/watermarks/logo.png" 
           right="10px" 
           bottom="10px" 
           width="100px" />
    
    <!-- Percentage-based positioning -->
    <image name="centered" 
           path="~/watermarks/watermark.png" 
           left="25%" 
           top="25%" 
           right="25%" 
           bottom="25%" 
           align="center" />
    
    <!-- Group with multiple layers -->
    <group name="composite">
      <image path="~/logo1.png" left="10px" top="10px" />
      <image path="~/logo2.png" right="10px" bottom="10px" />
    </group>
    
    <!-- Legacy other images -->
    <otherimages path="~/watermarks" width="50px" height="50px" />
  </watermarks>
</resizer>
```

## Migration Notes

### From Legacy to Imageflow

1. **Text watermarks**: Must be converted to image watermarks
2. **Background layers**: Must be implemented differently
3. **Complex relativeTo**: Only 'image' and 'canvas' supported
4. **Opacity**: Extract from imageQuery and set via WatermarkOptions.Opacity
5. **Multiple watermarks**: Specified as comma-separated list in querystring

### Querystring Usage

```
image.jpg?watermark=logo
image.jpg?watermark=logo,centered
image.jpg?watermark=composite
```

## Default Values

When attributes are not specified:
- Position: Defaults to centered (align determines exact position)
- RelativeTo: "image"
- DrawAs: "overlay"
- Fill: false
- Align: MiddleCenter


## What Imageflow.Net generates as JSON (core engine ref)

We probably want to set resampling hints to mimic ImageResizer 4, which involves 
("down.filter", "mitchell"), ("f.sharpen", "15"),("down.colorspace", "srgb")

Imageflow
Watermark
io_id (required) specifies which input image to use as a watermark.
gravity determines how the image is placed within the fit_box. {x: 0, y: 0} represents top-left, {x: 50, y: 50} represents center, {x:100, y:100} represents bottom-right. Default: center
fit_mode is one of distort, within, fit, within_crop, or fit_crop. Meanings are the same as for constraint modes. Default: within
fit_box can be either image_percentage (a box represented by percentages of target image width/height) or image_margins (a box represented by pixels from the edge of the image). Default image_margins 0
min_canvas_width sets a minimum canvas width below which the watermark will be hidden.
min_canvas_height sets a minimum canvas height below which the watermark will be hidden.
opacity (0..1) How opaque to draw the image. Default 1.0
hints See resampling hints
Example with fit_box: image_percentage
This will align the watermark to 10% from the bottom and right edges of the image, scaling the watermark down if it takes more than 80% of the image space, drawing it at 80% opacity and applying 15% sharpening. It will not display on images smaller than 50x50px in either dimension.

{
  "watermark": {
    "io_id": 1,
    "gravity": { 
      "percentage" : {
        "x": 100,
        "y": 100 
      }
    },
    "fit_mode": "within",
    "fit_box": { 
      "image_percentage": {
        "x1": 10,
        "y1": 10,
        "x2": 90,
        "y2": 90
      } 
    }, 
    "min_canvas_width": 50,
    "min_canvas_height": 50,
    "opacity": 0.8,
    "hints": {
      "sharpen_percent": 15
    }
  }
}
Example with fit_box: image_margins
This will stretch/distort the watermark to fill the image except for a 5px margin.

{
  "watermark": {
    "io_id": 1,
    "gravity": { "center": null },
    "fit_mode": "distort",
    "fit_box": { 
      "image_margins": {
        "left": 5,
        "top": 5,
        "right": 5,
        "bottom": 5
      } 
    }
  }
}

Resampling Hints
Resampling hints can be specified in constraint commands, scale commands, watermarking, and for compositing. They offer control over image sharpness, resampling color space, background color, and more.

sharpen_percent (0..100) The amount of sharpening to apply during resampling
up_filter The resampling filter to use if upscaling in one or more directions
down_filter The resampling filter to use if downscaling in both directions.
scaling_colorspace Use linear for the best results, or srgb to mimick poorly-written software. srgb can destroy image highlights.
background_color The background color to apply.
resample_when One of size_differs, size_differs_or_sharpening_requested, or always.
sharpen_when One of downscaling, upscaling, size_differs, or always
{ 
  "sharpen_percent": 15,
  "down_filter":  "robidoux",
  "up_filter": "ginseng",
  "scaling_colorspace": "linear",
  "background_color": "transparent",
  "resample_when": "size_differs_or_sharpening_requested",
  "sharpen_when": "downscaling"
}
Resampling Filters
robidoux - The default and recommended downsampling filter
robidoux_sharp - A sharper version of the above
robidoux_fast - A faster, less accurate version of robidoux
ginseng - The default and suggested upsampling filter
ginseng_sharp
lanczos
lanczos_sharp
lanczos_2
lanczos_2_sharp
cubic
cubic_sharp
catmull_rom
mitchell
cubic_b_spline
hermite
jinc
triangle
linear
box
fastest
n_cubic
n_cubic_sharp