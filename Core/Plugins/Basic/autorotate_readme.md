Tags: plugin
Bundle: free
Edition: free
Tagline: Use the Exif rotation data from the camera to auto-rotate your images.
Aliases: /plugins/autorotate

# Rotate images by Exif data

Automatically rotate images based on the Exif Orientation flag embedded by the camera.

## Enable autorotation for all images by default via Web.config

            <pipeline defaultCommands="autorotate.default=true" />


The default is "false"

## Via URL 

`&autorotate=false` or `&autorotate=true` will override the default. 


### Historical note

ImageResizer v4 has subsumed AutoRotate into the core; it is no longer a plugin. 