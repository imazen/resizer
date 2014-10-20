grid = {};
grid.commands = [
{ n: "Convert",
    key: "format",
    values: ["jpg", "png", "gif"]
},
{ n: "Width",
    key: "width",
    values: [32, 64, 128, 192, 256]
},
{ n: "Height",
    key: "height",
    values: [32, 64, 128, 192, 256]
},
{ n: "Fit mode",
    key: "mode",
    values: ["max","pad","crop","stretch","carve"]
},
{ n: "Scale",
    key: "scale",
    values: ["both", "downscaleonly", "upscalecanvas", "upscaleonly"]
},
{ n: "Ignore ICC profile",
    key: "ignoreicc",
    values: [true,false]
},
{ n: "Anchor (when padding or auto-cropping)",
    key: "anchor",
    values: ["topleft", "topcenter", "topright", "middleleft",
    "middlecenter", "middleright", "bottomleft", "bottomcenter", "bottomright"]
},
{ n: "Source Flip",
    key: "sFlip",
    values: ["none","x","y","xy"]
},
{ n: "Flip Result",
    key: "flip",
    values: ["none", "x", "y", "xy"]
},
{ n: "Source Rotate",
    key: "sRotate",
    values: [0,90,180,270]
},
{ n: "Rotate Result",
    key: "rotate",
    values: [0,30,60, 90,120, 180, 270]
},
{ n: "Auto rotate",
    key: "autorotate",
    values: [true,false]
},
{ n: "Crop",
    key: "crop",
    values: ["0,0,0,0","0,0,256,256","0,0,128,128","10,10,-10,-10"]
},
{ n: "Speed Or Quality",
    key: "speed",
    values: [0,1,2,3,4,5]
},
{ n: "Background Color",
    key: "bgcolor",
    values: ["white","black","red","ffeedd","gray"]
},
{ n: "Padding Width",
    key: "paddingWidth",
    values: [0, 1, 2, 4,16]
},
{ n: "Padding Color",
    key: "paddingColor",
    values: ["white", "black", "red", "ffeedd", "gray"]
},
{ n: "Border Width",
    key: "borderWidth",
    values: [0, 1, 2, 4, 16]
},
{ n: "Border Color",
    key: "borderColor",
    values: ["white", "black", "red", "ffeedd", "gray"]
},
{ n: "Margin",
    key: "margin",
    values: [0, 1, 2, 4, 16]
},
{ n: "Server",
    key: "_server",
    values: ["/","http://images.imageresizing.net/"]
},
{ n: "Grid style",
    key: "_style",
    values: ["normal","blackout"]
},
{ n: "Jpeg images",
    key: "_image",
    values: ["quality-original.jpg","fountain-small.jpg","tractor.jpg","red-leaf.jpg","grass.jpg","tulip-leaf.jpg","rose-leaf.jpg","image.jpg","tractor-tiny.jpg"]
},
{ n: "Animated Gifs",
    key: "_image",
    values: ["2_computers.gif", "clock.gif", "optical.gif"]
},
{ n: "Gradient",
    key: "_image",
    values: ["gradient.png"]
},
{ n: "Gradient Angle",
    key: "angle",
    values: [0,45,90,180,270]
},
{ n: "Gradient Color 1",
    key: "color1",
    values: ["white", "black", "red", "ffeedd", "gray"]
},
{ n: "Gradient Color 2",
    key: "color2",
    values: ["white", "black", "red", "ffeedd", "gray"]
},
{ n: "Color depth",
    key: "colors",
    values: [2,4,8,16,32,64,128,256,"max"]
},
{ n: "Dithering",
    key: "dither",
    values: ["true","4pass", 20, 30, 50, 70]
},
{ n: "Jpeg Compression",
    key: "quality",
    values: [10,20,30,40,50,60,70,75,80,90,95,100]
},
{ n: "A.Sepia",
    key: "a.sepia",
    values: [true, false]
},
{ n: "S.Saturation",
    key: "s.saturation",
    values: [-5, -1, -0.9, -0.5, -0.2, -0.1, 0, 0.1, 0.2, 0.5, 0.9, 1, 5, 10]
},
{ n: "S.Contrast",
    key: "s.contrast",
    values: [-5, -1, -0.9, -0.5, -0.2, -0.1, 0, 0.1, 0.2, 0.5, 0.9, 1, 5, 10]
},
{ n: "S.Brightness",
    key: "s.brightness",
    values: [-5, -1, -0.9, -0.5, -0.2, -0.1, 0, 0.1, 0.2, 0.5, 0.9, 1, 5, 10]
},
{ n: "S.Alpha",
    key: "s.alpha",
    values: [ 0.1, 0.2, 0.5, 0.9, 1]
},
{ n: "S.Sepia",
    key: "s.sepia",
    values: [true,false]
},
{ n: "S.Grayscale",
    key: "s.grayscale",
    values: ["y","ry","bt709","flat"],
    names: ["NTSC/Y grayscale standard","R-Y Grayscale standard","BT709 (HDTV) Grayscale standard","Flat (50/50/50) Grayscale"]
},
{ n: "S.Invert",
    key: "s.invert",
    values: [true,false]
},
{ n: "A.Saturation",
    key: "a.saturation",
    values: [-5, -1, -0.9, -0.5, -0.2, -0.1, 0, 0.1, 0.2, 0.5, 0.9, 1, 5, 10]
},
{ n: "A.Contrast",
    key: "a.contrast",
    values: [-5, -1, -0.9, -0.5, -0.2, -0.1, 0, 0.1, 0.2, 0.5, 0.9, 1, 5, 10]
},
{ n: "A.Brightness",
    key: "s.brightness",
    values: [-5, -1, -0.9, -0.5, -0.2, -0.1, 0, 0.1, 0.2, 0.5, 0.9, 1, 5, 10]
},
{ n: "A.Equalize",
    key: "a.equalize",
    values: [true,false]
},
{ n: "A.TruncateWhenAdjusting",
    key: "a.truncate",
    values: [true, false]
},
{ n: "A.Blur",
    key: "a.blur",
    values: [0,1,2,3,4,5]
},
{ n: "A.Sharpen",
    key: "a.sharpen",
    values: [0, 1, 2, 3, 4, 5]
},
{ n: "A.Oil Painting",
    key: "a.oilpainting",
    values: [0, 1, 2, 3, 4, 5]
},
{ n: "A.Remove Noise",
    key: "a.removenoise",
    values: [0, 1, 2, 3, 4, 5]
},
{ n: "A.Posterize",
    key: "a.posterize",
    values: [2,4,8,32,64,128]
},
{ n: "A.Canny Edge detector",
    key: "a.canny",
    values: [true, false]
},
{ n: "A.Sobel Edge detector",
    key: "a.sobel",
    values: [true, false]
},
{ n: "A.Sobel Edge Threshold",
    key: "a.threshold",
    values: [1,2,3,4,5]
},
{ n: "A.Sobel Edge Threshold",
    key: "a.threshold",
    values: [32,64,128,256]
},
{ n: "Trim Whitespace with Threshold",
    key: "trim.threshold",
    values: [32,64,128,256]
},
{ n: "Trim Whitespace with Padding",
    key: "trim.percentpadding",
    values: [0,2,5,10]
}



];


/*Convert (JPG, PNG, GIF): format = jpg, png, gif
Width (32,64,128, 192, 256)
Height (32,64,128, 192, 256)
Fit Mode (Max, Pad, Crop, Stretch, Carve)  (Warning: both a width and height must be specified for this to take effect)
scale (downscaleonly, upscaleonly, upscalecanvas, both)



"format", "thumbnail", "maxwidth", "maxheight",
                "width", "height",
                "scale", "stretch", "crop", "page", "bgcolor",
                "rotate", "flip", "sourceFlip", "sFlip", "sRotate", "borderWidth",
                "borderColor", "paddingWidth", "paddingColor",
                "ignoreicc", "frame", "useresizingpipeline", 
                "cache", "process", "margin", "anchor","dpi","mode"*/