Visit http://imageresizing.net/ for installation instructions, upgrade notes, licensing details, and example code.

The website contains the most up-to-date installation instructions, but here's the short version

* For a Visual Studio project

1) Use Project->Add Reference to add ImageResizer.dll to the project. 
2) Change the Project's web.config file to match the settings in the included Web.Config file.

* If you have NuGet, simply go to Tools->Library Package Manager->Package Manager Console
and run "Install-Package ImageResizer.WebConfig"

* If you're installing this on an IIS website, 

1) Copy ImageResizer.dll and ImageResizer.pdb into the /bin directory of the site (create the folder if it doesn't exist.
2) Copy Web.config into the root of the website. If it already exists, you'll need to add the new settings into the old file.