Aliases: /docs/configuration-all
Flags: hidden

# Configuration Reference

This is a full reference showing how to use each setting. You should *never* copy this into your site - it will break everything. Only change the settings you need to, and only specify the settings you change.


	<?xml version="1.0" encoding="utf-8" ?>
	<configuration>
		<configSections>
			<section name="resizer" type="ImageResizer.ResizerSection,ImageResizer"  requirePermission="false"  />
			<section name="nlog" type="NLog.Config.ConfigSectionHandler, NLog"/>
			
		</configSections>

		<resizer>
			<!-- Unless you (a) use Integrated mode, or (b) map all requests to ASP.NET,
					 you'll need to add .ashx to your image URLs: image.jpg.ashx?width=200&height=20
					 vppUsage defaults to 'Fallback', which means VirtualPathProviders are used if the
					 physical file doesn't exist.
					 Optional - these are the default settings -->
		 <pipeline fakeExtensions=".ashx" vppUsage="Always|Fallback|None"/>

			<!-- minutes specifies how far in the future to set the Expires: header. The Expires header
					 tells the browser to not even *check* for a newer version until the Expires header expires.
					 Defaults to 0 - header not sent. 1440 is 24 hours, a good value.
					 See http://www.imageresizing.net/plugins/clientcache for details.-->
		 <clientcache minutes="0|1440" />

			<!-- When onlyAllowPresets="true", all other querystring pairs will be stripped from the URL.
			     Naturally, this will break the RemoteReader plugin if you're using the signed URLs.
			     onlyAllowPresets does not apply to the managed API, only to the URL API
			     See http://www.imageresizing.net/plugins/presets for details.-->
			<presets onlyAllowPresets="true|false">
				<preset name="thumb-defs" defaults="width=100;height=100" />
				<preset name="thumb" settings="width=100;height=100" />
				<preset name="thumb-width" defaults="height=100" settings="width=100" />
				<!-- The height can be overridden above, but not the width -->
			</presets>

			<!-- Overrides the 'custom errors' setting. Enables the /resizer.debug page for the specified clients.
					 Defaults to the same behavior specified in customErrors, so it won't expose data.
					 See http://www.imageresizing.net/plugins/diagnostics for details. -->
			<diagnostics enableFor="Localhost|AllHosts|None" />

			<!-- The image404 plugin (when installed) lets you specify a 404 fallback in the querystring
					 If the image doesn't exists. You can also reference 'presets'.
					 Ex. image.jpg?404=myPreset and image.jpg?404=404-whale.png would result in the same behavior
					 See http://www.imageresizing.net/plugins/image404 for details. -->
			<image404 baseDir="~/Images/404Images/" myPreset="~/Images/404Images/404-whale.png" />


			<!-- The DefaultSettings plugin allows you to specify the default settings to use when certain
			     settings are omitted. Currently supports ScaleMode defaults.
			     See http://www.imageresizing.net/plugins/defaultsettings for details. -->
			<defaultsettings explicitSizeScaleMode="DownscaleOnly" maxSizeScaleMode="DownscaleOnly" />

			<!-- The RemoteReader plugin allows the ImageResizer to resize and display images that are
			     located at any URL. Kind of like a resizing relay. Make sure the key is kept safe,
			     and is the same across all servers in the web farm (if you're using one). This key
			     can contain any xml-safe characters, and should be as long as possible. URLs generated
			     with one key will not work with another.
			     See http://www.imageresizing.net/plugins/remotereader for details. -->
			<remotereader signingKey="put a long and very secure key here"></remotereader>

			<!-- The CloudFront plugin makes ImageResizer work nicely with CDNs that strip off query
			     strings by default, such as Amazon CloudFront and Azure CDN. Change
			     d3urjqacv88oxz.cloudfront.net to match the distribution name you created in the AWS console.
			     See http://www.imageresizing.net/plugins/cloudfront for details. -->
			<cloudfront redirectThrough="http://d3urjqacv88oxz.cloudfront.net" redirectPermanent="false" />.

			<!-- see http://www.imageresizing.net/plugins/sizelimiting for detailed docs -->
			<sizelimits imageWidth="0" imageHeight="0" totalWidth="3200" totalHeight="3200"
				          totalBehavior="throwexception" />

			<!-- See http://www.imageresizing.net/plugins/diskcache for details. Avoid changing these settings -->
			<diskCache dir="~/imagecache" autoClean="false" hashModifiedDate="true" enabled="true"
				         subfolders="32" cacheAccessTimeout="15000" />

			<cleanupStrategy startupDelay="00:05"
											 minDelay="00:00:20"
											 maxDelay="00:05"
											 optimalWorkSegmentLength="00:00:04"
											 targetItemsPerFolder="400"
											 maximumItemsPerFolder="1000"
											 avoidRemovalIfCreatedWithin="24:00"
											 avoidRemovalIfUsedWithin="4.00:00"
											 prohibitRemovalIfUsedWithin="00:05"
											 prohibitRemovalIfCreatedWithin="00:10" />
			<!-- The Watermark plugin is fully XML configurable, and supports named watermark
			     configurations, multiple image and text layers, layer groups, and background
			     (as well as overlay) layers. The positioning system is per-layer, permits
			     flexible pixel and percent layout, anchoring, and container selection.
			     See http://www.imageresizing.net/plugins/watermark for details. -->
			<watermarks>
				<otherimages path="~/watermarks" right="20" bottom="20" width="20" height="20" />
				<image name="test1" path="~/watermarks/Sun_64.png"
					     imageQuery="filter=alpha(0.5)&amp;rotate=45" top="0" left="0" />
				<text name="test2" text="Hello #{name}!" vertical="true" align="topright" />
				<image name="gradientbg" path="~/gradient.png" drawAs="background"
					     imageQuery="color1=black&amp;color2=white&amp;angle=45"
					     top="0" right="0" left="0" bottom="0" />
				<group name="storyBG">
					<image path="~/watermarks/Sun_64.png" align="topleft" width="100%" height="100%"
						relativeTo="canvas" drawAs="Background"></image >
				</group>
			</watermarks>

			<plugins>
				<!-- These are installed by default. We're only removing and re-adding them to show an example.
				     It's pointless -->
				<remove name="DefaultEncoder" />
				<remove name="NoCache" />
				<remove name="ClientCache" />
				<remove name="Diagnostics />
				<add name="DefaultEncoder" />
				<add name="NoCache" />
				<add name="ClientCache" />
				<add name="Diagnostics />


				<!-- Unless otherwise noted, the remaining plugins are not included in ImageResizer.dll - they
				     have their own DLLs -->

				<!----------------------------------->
				<!-- Essential Edition starts here -->
				<!----------------------------------->

				<!-- Adds the /resize(w,h,f)/ folder syntax -->
				<!-- You must remove the <add name="FolderResizeSyntax" /> if present -->
				<!-- http://www.imageresizing.net/plugins/folderresizesyntax -->
				<add name="FolderResizeSyntax" />

				<!-- Adds the PdfRender -->
				<!-- If you set downloadNativeDependencies="false" or you're running < V3.2, place gsdll32.dll and  -->
				<!-- gsdll64.dll in the /bin directory -->
				<!-- http://www.imageresizing.net/plugins/pdfrenderer -->
				<add name="PdfRenderer" downloadNativeDependencies="true" />

				<!-- Add IEPngFix -->
				<!-- http://www.imageresizing.net/plugins/iepngfix -->
				<!-- If redirect="true" (the default), the requests from IE will be HTTP redirected to new URLs. -->
				<!-- If false, the GIF will be silently served instead of the PNG, without any redirection. -->
				<!-- When catchAll=false, the behavior is opt-in. You must add &iefix=true to enable the  -->
				<!-- browser detection and redirection behavior for the URL. -->
				<!-- When catchAll=true, the behavior is opt-out. You must add &iefix=false to disable the browser  -->
				<!-- detection and redirection behavior for the URL. -->
				<add name="IEPngFix" redirect="true|false" catchAll="true|false" />

				<!-- Add SideLimiting plugin -->
				<!-- This plugin is installed by default on ASP.NET sites (not for WinForms, Console, or WPF apps) -->
				<!-- See Remove SizeLimiting Below -->
				<!-- http://www.imageresizing.net/plugins/sizelimiting -->
				<add name="SizeLimiting" />

				<!-- Remove SizeLimiting -->
				<!-- http://www.imageresizing.net/plugins/sizelimiting -->
				<!-- In rare cases, it may make sense to completely remove the plugin. -->
				<!-- <remove name="SizeLimiting" /> -->

				<!-- Add SpeedOrQuality (v3.1+) -->
				<!-- http://www.imageresizing.net/plugins/speedorquality -->
				<add name="SpeedOrQuality" />

				<!-- Add Drop Shadow -->
				<!-- http://www.imageresizing.net/plugins/dropshadow -->
				<add name="DropShadow" />

				<!-- Add Presets plugin (v3.1+) -->
				<!-- http://www.imageresizing.net/plugins/presets -->
				<add name="Presets" />

				<!-- Add Logging plugin (v3.1+) -->
				<!-- Add ImageResizer.Plugins.Logging.dll to your project. NLog.dll is also needed, but doesn't -->
				<!-- have to be referenced directly - it should be automatically copied if you are using Visual Studio.  -->
				<!-- If not, copy it to the /bin folder as well -->
				<!-- http://www.imageresizing.net/plugins/logging -->
				<add name="Logging" />

				<!-- Add ImageHandlerSyntax plugin -->
				<!-- Adds support for the syntaxes used by 4 image resizing handlers. This plugin allows  -->
				<!-- painless, gradual migration from them by supporting their URL syntax. -->
				<!-- http://www.imageresizing.net/plugins/imagehandlersyntax -->
				<add name="ImageHandlerSyntax" />

				<!-- Add Image404 plugin -->
				<!-- This plugin is *not* installed by default, but is included in ImageResizer.Dll -->
				<!-- http://www.imageresizing.net/plugins/image404 -->
				<add name="Image404" />

				<!-- Custom Overlay plugin -->
				<!-- This is an example plugin. It is useful as a starting point, but is not subject to the same  -->
				<!-- standards of maintenance and backwards-compatibility that normal plugins are. -->
				<!-- http://www.imageresizing.net/plugins/customoverlay -->
				<add name="CustomOverlay" provider="MyNamespace.MyOverlayProviderClass, MyAssembly"
					   arg1="value1" arg2="value2.." ignoreMissingFiles="false" />

				<!-- Install Gradient plugin -->
				<!-- Generates gradients on the fly. Very useful for rapid prototyping and design - but safe -->
				<!-- for production use! This plugin is *not* installed by default, but is included in ImageResizer.Dll -->
				<!-- http://www.imageresizing/plugins/gradient -->
				<add name="Gradient" />

				<!-- DefaultSettings plugin (V3.1+) -->
				<!-- Allows you to specify the default settings to use when certain settings are omitted. -->
				<!-- Currently supports ScaleMode defaults. -->
				<!-- http://www.imageresizing.net/plugins/defaultsettings -->
				<add name="DefaultSettings" />

				<!-- Works like an IIS virtual folder, but without IIS.  -->
				<!-- This plugins is *not* installed by default, but is included in ImageResizer.Dll -->
				<!-- http://www.imageresizing.net/plugins/virtualfolder -->
				<add name="VirtualFolder" virtualPath="~/" physicalPath="..//Images" />
				<add name="VirtualFolder" virtualPath="~/watermarks" physicalPath="..//Watermarks" />

				<!-- Add AutoRotate plugin (v3.1+) -->
				<!-- Automatically rotates images based on the EXIF Orientation flag embedded by the camera. -->
				<!-- http://www.imageresizing.net/plugins/autorotate -->
				<add name="AutoRotate" />


				<!------------------------------------->
				<!-- Performance Edition starts here -->
				<!------------------------------------->

				<!-- Add AzureReader2 plugin -->
				<!-- Allows images located in an Azure Blobstore to be read, processed, resized, and served. -->
				<!-- Requests for unmodified images get redirected to the blobstore itself. -->
				<!-- AzureReader2 supports the Azure SDK V2.0 and requires .NET 4.0 instead of .NET 3.5. -->
				<!-- http://www.imageresizing.net/plugins/azurereader2 -->
				<add name="AzureReader2"
					   connectionString="DefaultEndpointsProtocol=http;AccountName=myAccountName;AccountKey=myAccountKey"
					   endpoint="http://<account>.blob.core.windows.net/" />

				<!-- Add RemoteReader plugin -->
				<!-- The RemoteReader plugin allows the ImageResizer to resize and display images that are -->
				<!-- located at any URL. Kind of like a resizing relay. -->
				<!-- http://www.imageresizing.net/plugins/remotereader -->
				<add name="RemoteReader" />

				<!-- Add CloudFront plugin -->
				<!-- Makes the ImageResizer work nicely with CDNs that strip off query strings by default, -->
				<!-- such as Amazon CloudFront and Azure CDN. -->
				<!-- http://www.imageresizing.net/plugins/cloudfront -->
				<add name="CloudFront" />

				<!-- Add S3Reader plugin -->
				<!-- Allows images located on Amazon S3 to be processed and resized as if they were located -->
				<!-- locally on the disk. Also serves files located on S3 - not restricted to images -->
				<!-- (unless vpp="false") is used. -->
				<!-- http://www.imageresizing.net/plugins/s3reader -->
				<add name="S3Reader"
					   vpp="true"
					   buckets="my-bucket-1,my-bucket-2,my-bucket-3"
					   prefix="~/s3/"
						 checkForModifiedFiles="false"
						 useSsl="false"
						 accessKeyId=""
						 secretAccessKey=""
						 useSubdomains="false" />

				<!-- Add SqlReader plugin -->
				<!-- Allows you to access binary blobs in a SQL database using a URL. Accepts integer, GUID, and -->
				<!-- string identifiers for images. -->
				<!-- http://www.imageresizing.net/plugins/sqlreader -->
	   		<add name="SqlReader"
	   			prefix="~/databaseimages/"
	   			connectionString="database"
	   			idType="UniqueIdentifier"
	   			blobQuery="SELECT Content FROM Images WHERE ImageID=@id"
	   			modifiedQuery="Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id"
	   			existsQuery="Select COUNT(ImageID) From Images WHERE ImageID=@id"
	   			requireImageExtension="false"
	   			cacheUnmodifiedFiles="true"
	   			extensionPartOfId="false"
	   			checkForModifiedFiles="true"
	   			vpp="true"
	   			untrustedData="false" />

				<!-- Add DiskCache plugin -->
				<!-- When disabling DiskCache on a site that hosts protected images, it is best to use  -->
				<!-- <diskcache enabled="false" /> instead of removing <add name="DiskCache" />.  -->
				<!-- http://www.imageresizing.net/plugins/diskcache -->
				<!-- <add name="DiskCache" /> -->
				<diskcache enabled="false" />
				<!-- Plugins below are included with DiskCache -->
				<!-- SourceMemCache (beta) -->
				<!-- Caches up to 10MB of original image files in memory. Files not accessed for more than -->
				<!-- 10 minutes are removed from the cache. -->
				<!-- Improves performance for real-time image editing (such as via StudioJS or jCrop). -->
				<add name="SourceMemCache" />
				<!-- Output MemCache (alpha) -->
				<!-- Caches up to 10MB of output image files in memory. Files not accessed for more than -->
				<!-- 10 minutes are removed from the cache. Useful for few-time-use image processing, such as a -->
				<!-- live camera feed. -->
				<add name="MemCache" />
				<!-- SourceDiskCache (beta) -->
				<!-- Like DiskCache, but for source files. Not advisable if your source image collection is -->
				<!-- larger than available local storage. -->
				<add name="SourceDiskCache" />

				<!-- Add AzureReader plugin -->
				<!-- NOTE: See AzureReader2 if you're using the Azure SDK 2.0. -->
				<!-- Allows images located in an Azure Blobstore to be read, processed, resized, and served. -->
				<!-- Requests for unmodified images get redirected to the blobstore itself. -->
				<!-- http://www.imageresizing.net/plugins/azurereader -->
				<add name="AzureReader" connectionString="ConnectionKeyName"
					   endpoint="http://<account>.blob.core.windows.net/" />

				<!-- Add AnimatedGifs plugin -->
				<!-- Adds support for resizing animated gifs. Once added, animated gifs will be resized while -->
				<!-- maintaining all animated frames. By default, .NET only saves the first frame of the GIF image. -->
				<!-- http://www.imageresizing.net/plugins/animatedgifs -->
				<add name="AnimatedGifs" />

				<!-- Add PrettyGifs plugin -->
				<!-- Replaces .NET's disgusting default GIF encoding algorithm with Octree quantization and -->
				<!-- dithering, and allows 8-bit PNG creation. Compatible with all plugins. -->
				<!-- http://www.imageresizing.net/plugins/prettygifs -->
				<add name="PrettyGifs" />


				<!---------------------------------->
				<!-- Creative Edition starts here -->
				<!---------------------------------->

				<!-- WIC Plugins (V3.1+) -->
				<!-- ImageResizer.Plugins.Wic.dll contains 3 plugins: WicImageDecoder, WicImageEncoder, and -->
				<!-- WicImageBuilder. These plugins require Windows 7 or Windows Server 2008 R2 for best -->
				<!-- performance and correct behavior. Vista or Windows Server 2008 with the Platform Update applied should also -->
				work.
				<!-- http://www.imageresizing.net/plugins/wic -->
				<add name="WicDecoder" />
				<add name="WicEncoder" />
				<add name="WicBuilder" />

				<!-- New Watermark plugin (v3.1+) -->
				<!-- The new watermark plugin is fully XML configurable, and supports named watermark -->
				<!-- configurations, multiple image and text layers, layer groups, and background (as well as overlay) -->
				<!-- layers. The positioning system is per-layer, permits flexible pixel and percent layout, anchoring, -->
				<!-- and container selection. Actually, you add Watermark from C# code - configuration is rather -->
				<!-- comprehensive, and it doesn't have an xml representation yet. -->
				<!-- http://www.imageresizing.net/plugins/watermark -->
				<add name="Watermark" />

				<!-- SeamCarving plugin -->
				<!-- Provides content-aware image resizing and 5 different algorithms. -->
				<!-- http://www.imageresizing.net/plugins/seamcarving -->
				<add name="SeamCarving" />

				<!-- FreeImage Plugins -->
				<!-- ImageResizer.Plugins.FreeImage.dll contains several plugins, all based around the abilities -->
				<!-- offered by the C/C++ FreeImage library. These plugins are alpha-level. -->
				<!-- http://www.imageresizing.net/plugins/freeimage -->
				<!-- FreeImageDecoder plugin -->
				<!-- Introduces support for RAW & HDR image formats, such as CRW/CR2, NEF, RAF, DNG, MOS, KDC, -->
				<!-- DCR, etc. Also introduced support for XBM, XPM, TARGA, SGI, Sun RAS, PSD, PICT, PNG, PFM, PBM, -->
				<!-- PGM, PPM, PCX, MNG, Kodak PhotoCD, KOALA, JPEG-2000, JIF, JNG, IFF, ICO, Raw Fax G3, EXR, DDS, -->
				<!-- and Dr. Halo CUT files. -->
				<add name="FreeImageDecoder" />
				<!-- FreeImageEncoder plugin -->
				<!-- FreeImageEncoder can encode jpegs 2-3x as fast as GDI can, and offers more encoding options. -->
				<add name="FreeImageEncoder" />
				<!-- FreeImageBuilder plugin -->
				<!-- Provides an alternate resizing pipeline that never touches GDI. Only supports -->
				<!-- width/maxwidth/height/maxheight/scale/marginWidth/paddingWidth/fi.scale settings. -->
				<add name="FreeImageBuilder" />
				<!-- FreeImageResizer plugin -->
				<!-- Adds support for FreeImage resizing algorithms, which include CatmullRom, Lanczos3,
				bspline, box, bicubic, and bilinear filters. -->
				<add name="FreeImageResizer" />

				<!-- AdvancedFilters plugin -->
				<!-- Apply advanced effects to your images. Requires Full Trust. -->
				<!-- The plugin currently applies effects to the image along with any background color, padding, -->
				<!-- or drop shadow that may be present. Future versions may simply apply the effect to the image, -->
				<!-- not the surrounding area. Note: does not affect borders or watermarks. -->
				<!-- http://www.imageresizing.net/plugins/advancedfilters -->
				<add name="AdvancedFilters" />

				<!-- SimpleFilters plugin -->
				<!-- This plugin provides grayscale, sepia, brightness, saturation, contrast, inversion, -->
				<!-- and alpha filtering options. It also includes beta support for rounded corners. -->
				<!-- http://www.imageresizing.net/plugins/simplefilters -->
				<add name="SimpleFilters" />


				<!-- WhitespaceTrimmer plugin -->
				<!-- Trims whitespace (even smooth gradients) from around images automatically using edge -->
				<!-- detection filters. Requires Full Trust, uses unmanaged code. -->
				<!-- http://www.imageresizing.net/plugins/whitespacetrimmer -->
				<add name="WhitespaceTrimmer" />


				<!------------------------------->
				<!-- Elite Edition starts here -->
				<!------------------------------->

				<!-- CropAround plugin -->
				<!-- Provided a set of focus rectangles, will crop the image while preserving the specified areas. -->
				<!-- Coordinates must be in the source image coordinates. -->
				<!-- Requires mode=crop and width and height to be specified to activate. -->
				<!-- http://www.imageresizing.net/plugins/croparound -->
				<add name="CropAround" />

				<!-- Faces plugin -->
				<!-- You can find a sample project for this plugin in \Samples\ImageStudio within the full download -->
				<!-- Human face detection plugin. Provides automatic face detection, as well as the CropAround -->
				<!-- plugin, which can even be combined in a single request (using &c.focus=faces) to provide -->
				<!-- face-focused/face-preserving cropping. -->
				<!-- OpenCV is required for face detection. Requires V3.2 or higher. -->
				<!-- http://www.imageresizing.net/plugins/faces -->
				<add name="Faces" downloadNativeDependencies="true" />

				<!-- WebP plugins -->
				<!-- With slimmage.js, you can use WebP for supporting browsers - without breaking the others. -->
				<!-- Slimmage makes responsive images easy to implement - both client and true image size is -->
				<!-- controlled with css max-width properties. -->
				<!-- http://www.imageresizing.net/plugins/webp -->
				<!-- WebPDecoder -->
				<!-- Simply reference a .webp file as you would a .jpg -->
				<add name="WebPEncoder" />
				<!-- WebPEncoder -->
				<!-- Add &format=webp to any URL to encode the result in webp format instead of jpg/png -->
				<add name="WebPDecoder" />

				<!-- FFmpeg Plugin -->
				<!-- Dynamically extract frames from videos by time or percentage. Includes basic blank -->
				<!-- frame avoidance. Based on ffmpeg. -->
				<!-- http://www.imageresizing.net/plugins/ffmpeg -->
				<add name="FFmpeg" downloadNativeDependencies="true" />

				<!-- PsdReader plugin -->
				<!-- Adds support for .PSD source files. No configuration required. -->
				<!-- http://www.imageresizing.net/plugins/psdreader -->
				<add name="PsdReader" />

				<!-- MongoReader plugin -->
				<!-- Allows files stored on MongoDB GridFS to be resized and processed as if they were local. -->
				<!-- Requires .NET 3.5. -->
				<!-- http://www.imageresizing.net/plugins/mongoreader -->
				<add name="MongoReader" connectionString="mongodb://user:password@servername/database" />

				<!-- PsdComposer plugin (New in V3.1) -->
				<!-- Allows you to edit PSD files (hide/show layers, change text layer contents, apply -->
				<!-- certain effects), and render them to jpeg, gif, or png dynamically. Works as an -->
				<!-- IVirtualImageProvider, so you can post-process the composed result with any of the other -->
				<!-- plugins or commands -->
				<!-- http://www.imageresizing.net/plugins/psdcomposer -->
				<add name="PsdComposer" />

				<!-- RedEye plugin -->
				<!-- You can find a sample project for this plugin in \Samples\ImageStudio within the full download -->
				<!-- Provides automatic and manual red-eye detection and correction. For automatic face and eye -->
				<!-- detection, OpenCV is required. Requires V3.2 or higher. -->
				<!-- http://www.imageresizing.net/plugins/redeye -->
				<add name="RedEye" downloadNativeDependencies="true" />

			</plugins>
		</resizer>
		<!-- configuration section for NLog and configure logging rules & targets -->
			<!-- http://www.imageresizing.net/plugins/logging -->
			<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
				xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

				<targets  async="true" >
					<target name="resizer" xsi:type="File" fileName="${basedir}/Logs/Resizer.txt" />
					<target name="diskcache" xsi:type="File" fileName="${basedir}/Logs/Diskcache.txt"
						      layout="${processid} ${pad:padCharacter= :padding=3:inner=${threadid}} ${time} ${message}"  />
				</targets>

				<rules>
					<logger name="ImageResizer.Plugins.DiskCache" minlevel="Trace" writeTo="diskcache" final="true"  />
					<logger name="*" minlevel="Debug" writeTo="resizer" />
				</rules>
			</nlog>
	  <system.web>
	 		<httpModules>
	 			<!-- This is for IIS5, IIS6, and IIS7 Classic, and Cassini/VS Web Server-->
	 			<add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
	 		</httpModules>
	 	</system.web>
	  <system.webServer>
	 		<validation validateIntegratedModeConfiguration="false"/>
	 		<modules>
	 			<!-- This is for IIS7+ Integrated mode -->
	 			<add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
	 		</modules>
			<!-- Add WebP Plugin
					 With slimmage.js, you can use WebP for supporting browsers - without
					 breaking the others. Slimmage makes responsive images easy to
					 implement - both client and true image size is controlled
					 with css max-width properties.
					 See http://www.imageresizing.net/plugins/webp for details. -->
	 		<!-- To correct HTTP Error 404.3 - Not Found Error -->
			<staticContent>
				<mimeMap fileExtension=".webp" mimeType="image/webp" />
			</staticContent>
		</system.webServer>
	</configuration>


