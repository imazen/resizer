Tags: plugin
Edition: elite
Tagline: "Extract frames from videos by time or percentage. Includes basic blank frame avoidance. Wrapper for ffmpeg."
Aliases: /plugins/ffmpeg


# FFmpeg Plugin


*PLEASE NOTE*
* **This plugin is not forwards-compatible. Avoid usage for maximum compatibility with Imageflow and future major ImageResizer releases.**
* **Do NOT use with untrusted data. This plugin passes source files to ffmpeg.exe, which (like all software with embedded codecs), has a history of vulnerabilities.**
* **Using this plugin with malicious files could result in a remote code execution vulnerability.**
* You can view [recently reported vulnerabilities here](https://cve.mitre.org/cgi-bin/cvekey.cgi?keyword=ffmpeg) and [here](https://web.nvd.nist.gov/view/vuln/search-results?query=ffmpeg&search_type=all&cves=on)



Dynamically extract frames from videos by time or percentage. Includes basic blank frame avoidance. Wrapper for ffmpeg.

## Prerequisites

Install FFmpeg and ensure `ffmpeg.exe` and `ffprobe.exe` are on your system PATH:

* **winget:** `winget install Gyan.FFmpeg`
* **scoop:** `scoop install ffmpeg`
* **Manual download:** https://www.gyan.dev/ffmpeg/builds/

You should use the latest release of FFmpeg for optimal security.

## Installation

1. Add ImageResizer.Plugins.FFmpeg.dll to your project using Visual Studio.
2. Add `<add name="FFmpeg" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. During `Application_Start`, call  `ImageResizer.Configuration.Config.Current.Plugins.LoadPlugins()` to ensure that ffmpeg is detected before the application starts accepting requests.

## Use

`http://example.com/videos/thisvideo.m4v?ffmpeg.seconds=45.3`

* `ffmpeg.seconds=45.3` - Will grab the frame 45.3 seconds into the video file. Fastest way to grab an image.
* `ffmpeg.percent=50.1` - Will grab the frame 50.1 percent through the video file (slower, as the videos length must be queried)
* `ffmpeg.skipblankframes=true` - Even slower - if the acquired frame is blank, another frame 5 seconds later will be chosen, and so on, up to 4 times.

## Version history

### v4.3 (current)

* **BREAKING:** CDN auto-download of `ffmpeg.exe`/`ffprobe.exe` binaries has been removed. You must install FFmpeg manually (see Prerequisites above).
* The system PATH is now searched for `ffmpeg.exe` and `ffprobe.exe`.
* Improved error messages when FFmpeg is not found (suggests winget, scoop, or gyan.dev).
* Process deadlock fix: stdout/stderr are now read asynchronously.
* Plugin now implements `IIssueProvider` and emits a diagnostic error when FFmpeg is missing.
* All .NET Framework projects now target .NET 4.7.2 (previously 4.5/4.5.2).

### v4.2.8 and prior

* FFmpeg binaries were automatically downloaded from a CDN via `downloadNativeDependencies="true"`.
* Targeted .NET 4.5/4.5.2.
* stdout/stderr were read synchronously, which could cause process deadlocks.
