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
}
,
{ n: "Server",
    key: "_server",
    values: ["/","http://images.imageresizing.net/"]
}
,
{ n: "Jpeg images",
    key: "_image",
    values: ["quality-original.jpg","fountain-small.jpg","tractor.jpg","red-leaf.jpg","grass.jpg","tulip-leaf.jpg","rose-leaf.jpg","image.jpg","tractor-tiny.jpg"]
}
,
{ n: "Animated Gifs",
    key: "_image",
    values: ["2_computers.gif", "clock.gif", "optical.gif"]
},
{ n: "Color depth",
    key: "colors",
    values: [2,4,8,16,32,64,128,256,"max"]
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