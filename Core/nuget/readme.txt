Upgrading from a previous version of ImageResizer? You may need to correct your Web.config file.

If you see 2 duplicate elements like the following, remove the shorter one.

	<section name="resizer" type="ImageResizer.ResizerSection"/>
	<section name="resizer" type="ImageResizer.ResizerSection" requirePermission="false"/>


You can find the release changelog at http://imageresizing.net/releases