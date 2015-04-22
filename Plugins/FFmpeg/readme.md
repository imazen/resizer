Tags: plugin
Edition: elite
Tagline: "Extract frames from videos by time or percentage. Includes basic blank frame avoidance. Based on ffmpeg."
Aliases: /plugins/ffmpeg


# FFmpeg Plugin


Dynamically extract frames from videos by time or percentage. Includes basic blank frame avoidance. Based on ffmpeg.

## Installation

1. Add ImageResizer.Plugins.FFmpeg.dll to your project using Visual Studio. 
2. Add `<add name="FFmpeg" downloadNativeDependencies="true" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. During `Application_Start`, call  `ImageResizer.Configuration.Config.Current.Plugins.LoadPlugins()` to ensure that ffmpeg is downloaded before the application starts accepting requests.

## Use

`http://example.com/videos/thisvideo.m4v?ffmpeg.seconds=45.3`

* `ffmpeg.seconds=45.3` - Will grab the frame 45.3 seconds into the video file. Fastest way to grab an image.
* `ffmpeg.percent=50.1` - Will grab the frame 50.1 percent through the video file (slower, as the videos length must be queried)
* `ffmpeg.skipblankframes=true` - Even slower - if the acquired frame is blank, another frame 5 seconds later will be chosen, and so on, up to 4 times. 
