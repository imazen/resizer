Aliases: /docs/install/web-config


# Editing Web.Config

To tell IIS to use the ImageResizer, we must edit the Web.config file. 

First, copy&paste a backup of the file in case you run into problems.

Right click on "Web.config", choose 'Open with', and pick Notepad.

For each &lt;element> in the following XML, look for the corresponding element in the existing Web.config file. If the element already exists, make sure the elements inside it exist also. If the element doesn't exist, you can copy and paste element and any child elements. 



	<?xml version="1.0" encoding="utf-8" ?>
	<configuration>
		<configSections>
			<section name="resizer" type="ImageResizer.ResizerSection,ImageResizer" requirePermission="false" />
		</configSections>

		<resizer>
			<!-- Unless you (a) use Integrated mode, or (b) map all requests to ASP.NET, 
			     you'll need to add .ashx to your image URLs: image.jpg.ashx?width=200&height=20 -->
			<pipeline fakeExtensions=".ashx" defaultCommands="autorotate.default=true"/>

			<plugins>
				<add name="MvcRoutingShim" />
				<!-- <add name="DiskCache" /> -->
				<!-- <add name="PrettyGifs" /> -->
			</plugins>	
		</resizer>

		<system.web>
			<httpModules>
				<!-- This is for IIS7/8 Classic Mode and Cassini-->
				<add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
			</httpModules>
		</system.web>

		<system.webServer>
			<validation validateIntegratedModeConfiguration="false"/>
			<modules>
				<!-- This is for IIS7+ Integrated mode -->
				<add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
			</modules>
		</system.webServer>
	</configuration>