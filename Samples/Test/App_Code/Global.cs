using System;
using System.Collections.Generic;
using System.Web;
using System.Reflection;
using System.Web.Hosting;
using System.IO;

/// <summary>
/// Summary description for Global
/// </summary>
public class Global:HttpApplication
{

    public Global() { }
	 static Global()
	{
		//
		// TODO: Add constructor logic here
		//
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        
	}


    public override void Init() {
        base.Init();

        
        //// Code that runs on application startup
        //ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
        //w.align = System.Drawing.ContentAlignment.BottomLeft;
        //w.hideIfTooSmall = true;
        //w.keepAspectRatio = true;
        //w.valuesPercentages = false;
        //w.watermarkDir = "~/watermarks/"; //Where the watermark plugin looks for the image specifed in the querystring ?watermark=file.png
        //w.bottomRightPadding = new System.Drawing.SizeF(20, 20);
        //w.topLeftPadding = new System.Drawing.SizeF(20, 20);
        //w.watermarkSize = new System.Drawing.SizeF(30, 30); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
        ////Install the plugin
        //w.Install(ImageResizer.Configuration.Config.Current);
        //new ImageResizer.Plugins.Basic.VirtualFolder("~/", "..\\Images").Install(ImageResizer.Configuration.Config.Current);

       
    }

    

    static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
        //This handler is called only when the common language runtime tries to bind to the assembly and fails.
        
        string dllsDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(HostingEnvironment.ApplicationPhysicalPath.TrimEnd('\\', '/'))), "dlls");

        string name = args.Name;
        //Strip version, culture, and public key token info if present.
        if (name.IndexOf(',') > -1) name = name.Substring(0, name.IndexOf(','));
        name = name.Trim() + ".dll";//Trim whitespace, add ".dll"
     
        //First look in debug, then release, then trial.
        string[] paths = new string[] { "debug", "release", "trial" };

        foreach (string s in paths) {
            string path = Path.Combine(Path.Combine(dllsDir, s), name);
            if (File.Exists(path)) {
                return Assembly.LoadFrom(path);
            }
        }
        return null;
                                       
        ////Retrieve the list of referenced assemblies in an array of AssemblyName.
        //Assembly MyAssembly, objExecutingAssemblies;
        //string strTempAssmbPath = "";

        //objExecutingAssemblies = Assembly.GetExecutingAssembly();
        //AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

        ////Loop through the array of referenced assembly names.
        //foreach (AssemblyName strAssmbName in arrReferencedAssmbNames) {
        //    //Check for the assembly names that have raised the "AssemblyResolve" event.
        //    if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(","))) {
        //        //Build the path of the assembly from where it has to be loaded.				
        //        strTempAssmbPath = "C:\\Myassemblies\\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
        //        break;
        //    }

        //}


        ////Load the assembly from the specified path. 					
        //MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

        ////Return the loaded assembly.
        //return MyAssembly;
    }




}