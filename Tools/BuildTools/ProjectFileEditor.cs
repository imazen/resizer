using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;

namespace BuildTools {
    public class ProjectFileEditor {

        public ProjectFileEditor(string path) {
            this.Path = path;
        }

        private string _Path = null;

        public string Path {
            get { return _Path; }
            set { _Path = value; }
        }

        public ProjectFileEditor RemoveStrongNameRefs() {
            XDocument d = XDocument.Load(Path);
            XNamespace ns = d.Root.Name.NamespaceName;
            bool didsomething = false;

            var iAttr =  "Include";

            foreach(XElement r in d.Descendants().Where(p => p.Name.LocalName == "Reference").ToList()){
                if (r.Attribute(iAttr) == null) continue;
                var str = r.Attribute(iAttr).Value;
                var ix = str.IndexOf(',');
                if (ix < 0) continue;
                var newval = str.Substring(0,ix);
                r.Attribute(iAttr).SetValue(newval);
                didsomething = true;
            }
            if (didsomething) d.Save(Path);
            return this;
        }


        public ProjectFileEditor ReplaceAllProjectReferencesWithDllReferences(string defaultDllFolder = null) {

            XDocument d = XDocument.Load(Path);
            XNamespace ns = d.Root.Name.NamespaceName;
            bool didsomething = false;


            //Find the Reference ItemGroup
            XElement referenceGroup = d.Descendants().Where(p => p.Name.LocalName == "ItemGroup"
                && p.Descendants().First<XElement>().Name.LocalName == "Reference").FirstOrDefault<XElement>();

            //Create it if it is missing
            if (referenceGroup == default(XElement)){
                referenceGroup = new XElement(ns + "ItemGroup");
                d.Root.Add(referenceGroup);
            }

            //Find all the project references, convert them to dll references.
            foreach (XElement e in d.Descendants(ns + "ProjectReference").ToList()) {
                string name = e.Descendants(ns + "Name").First().Value; //Project name
                string include = e.Attribute("Include").Value; //Relative path to project

                //Fallback hint - assume project name and assembly name match, assume build dir is the default
                string hintPath = System.IO.Path.Combine(defaultDllFolder, name + ".dll"); 
                //Get an absolute path to the reference project so we can parse it
                string referencedProject = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path),include));
                //Parse the referenced project, get the release build folder and the assembly name
                string assemblyName = null;
                if (!System.IO.File.Exists(referencedProject))
                {
                    continue;
                }
                string buildPath = findBuildPath(referencedProject,"Release", out assemblyName);

                //If successful, build a relative path (relative to the parent project) that points to the dll destination.
                if (buildPath != null) hintPath = collapsePath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(include), buildPath));
                else Debug.WriteLine("Failed to locate or parse referenced project " + referencedProject);

                XElement replacement = new XElement(ns + "Reference",
                    new XElement(ns + "SpecificVersion", bool.FalseString),
                    new XElement(ns + "HintPath", hintPath));

                string fullDllPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path),hintPath));
                if (!System.IO.File.Exists(fullDllPath)) {
                    Debug.WriteLine("Project " + System.IO.Path.GetFileNameWithoutExtension(Path) + "  references " + name + ", but the dll was not found at " + hintPath + ".");

                }

                referenceGroup.Add(replacement);
                //Use assembly name instead of project name if available.
                replacement.SetAttributeValue("Include", assemblyName != null ? assemblyName : name); 

                //Remove the project reference.
                e.Remove();
                didsomething = true;
            }
            if (didsomething) d.Save(Path);
            
            /*
             *     <ProjectReference Include="..\..\Plugins\FreeImage\ImageResizer.Plugins.FreeImage.csproj">
                      <Project>{8E863AFE-B4CF-46AA-8382-0C3547C0EDE9}</Project>
                      <Name>ImageResizer.Plugins.FreeImage</Name>
                    </ProjectReference>

                    <Reference Include="ImageResizer.Plugins.Watermark">
                      <HintPath>..\..\dlls\release\ImageResizer.Plugins.Watermark.dll</HintPath>
                    </Reference>*/

            return this;
        }

        protected string collapsePath(string path) {
            string oldPath = path;
            do {
                oldPath = path;
                path = collapseOneLevel(oldPath);
            } while (oldPath != path);
            return path;
        }

        protected string collapseOneLevel(string path) {
            int up = path.Length -1;
            do{
                up = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar,up);
                if (up < 0) return path;
                int prevSlash = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar, up - 1);
                if (prevSlash < 0) return path;
                string segment = path.Substring(prevSlash + 1,up-prevSlash -1);
                if (segment.Equals("..", StringComparison.OrdinalIgnoreCase)) {
                    //We can't combine \..\..\, just keep looking closer to the beginning of the string. We already adjusted 'up'
                }else if (segment.Equals(".", StringComparison.OrdinalIgnoreCase)){
                    return path.Substring(0,prevSlash) + path.Substring(up); //Just remove \.\ sections
                }else{
                    return path.Substring(0, prevSlash) + path.Substring(up + 3); //If it's not \.\ or \..\, remove both it and the following \..\
                }
            }while(up > 0);
            return path;
        }

        private string findBuildPath(string csproj, string configuration, out string assemblyNameString) {
            XDocument d = XDocument.Load(csproj);
            assemblyNameString = null;

            XElement assemblyName = d.Descendants().Where(p => p.Name.LocalName =="AssemblyName" && p.Parent.Attribute("Condition") == null).FirstOrDefault();
            if (assemblyName == default(XElement)) return null;

            assemblyNameString = assemblyName.Value;

            XElement group = d.Descendants().Where(p => p.Name.LocalName == "PropertyGroup" && p.Attribute("Condition") != null && p.Attribute("Condition").Value.IndexOf("'" + configuration + "|",StringComparison.Ordinal) >= 0).FirstOrDefault();
            if (group == default(XElement)) return null;

            string outputPath = group.Descendants().Where(p => p.Name.LocalName == "OutputPath").FirstOrDefault().Value.TrimEnd('\\') + '\\' + assemblyNameString + ".dll";

            return outputPath;
        }
    }
}
