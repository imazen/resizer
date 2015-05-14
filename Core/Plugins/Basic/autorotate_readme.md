Tags: plugin
Bundle: free
Edition: free
Tagline: Use the EXIF rotation data from the camera to auto-rotate your images.
Aliases: /plugins/autorotate

# AutoRotate plugin (deprecated, built-in)

Automatically rotates images based on the EXIF Orientation flag embedded by the camera. 

ImageResizer v4 has subsumed AutoRotate into the core; it does not need to be added. You can enabled autorotation on all processed images via Web.config:


            <pipeline defaultCommands="autorotate.default=true" /> ]


Or, indiviudally using `&autorotate=true`