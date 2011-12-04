using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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

        public void ReplaceAllProjectReferencesWithDllReferences(string dllFolder) {

            XDocument d = XDocument.Load(Path);
            XNamespace ns = d.Root.Name.NamespaceName;
            bool didsomething = false;


            // A search which finds the ItemGroup which has Reference 
            // elements and returns the ItemGroup XElement.
            XElement referenceGroup = d.Descendants().Where(p => p.Name.LocalName == "ItemGroup"
                && p.Descendants().First<XElement>().Name.LocalName == "Reference").FirstOrDefault<XElement>();

            //Create it if it is missing
            if (referenceGroup == default(XElement)){
                referenceGroup = new XElement(ns + "ItemGroup");
                d.Root.Add(referenceGroup);
            }


            foreach (XElement e in d.Descendants(ns + "ProjectReference").ToList()) {
                string name = e.Descendants(ns + "Name").First().Value;
                

                XElement replacement = new XElement(ns + "Reference",
                    new XElement(ns + "SpecificVersion", bool.FalseString),
                    new XElement(ns + "HintPath", System.IO.Path.Combine(dllFolder,name + ".dll")));

                referenceGroup.Add(replacement);
                replacement.SetAttributeValue("Include",name);

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


        }
    }
}
