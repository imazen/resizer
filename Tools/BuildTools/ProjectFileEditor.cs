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
            
            bool didsomething = false;
            //if (d.Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}ProjectReference").Count() < 1) return;

            foreach (XElement e in d.Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}ProjectReference").ToList()) {
                string name = e.Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}Name").First().Value;

                
                var replacement = new XElement("Reference");
                replacement.SetAttributeValue("Include",name);
                var path = new XElement("HintPath");
                path.SetValue(System.IO.Path.Combine(dllFolder,name + ".dll"));
                replacement.Add(path);
                e.ReplaceWith(replacement);
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
