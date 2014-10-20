using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageResizer.Core.Tests.SiteMocks {
    public class WebConfigBuilder {
        public WebConfigBuilder() {
        }
        public WebConfigBuilder(string resizerConfigContents) {
            this.resizerContents = resizerConfigContents;
        }

        public string webConfigTop = "<?xml version='1.0' encoding='utf-8' ?><configuration><configSections>" +
            "<section name='resizer' restartOnExternalChanges='true' requirePermission='false' type='ImageResizer.ResizerSection,ImageResizer' />" +
            "</configSections>";

        public string resizerTop =
            "<resizer xmlns='http://imageresizing.net/resizer.xsd'  xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:noNamespaceSchemaLocation='resizer.xsd'>\n\n";

        public string resizerContents = "";

        public string resizerBottom = "\n</resizer>\n";

        public string compilationTop = "<system.web><compilation debug='true'>";
        public string webConfigBottom =
            "</compilation><httpModules> <add name='ImageResizingModule' type='ImageResizer.InterceptModule'/>\n" +
            "</httpModules></system.web>\n" +
            "<system.webServer><validation validateIntegratedModeConfiguration='false'/>\n" +
            "<modules> <add name='ImageResizingModule' type='ImageResizer.InterceptModule'/>\n" +
            "</modules></system.webServer></configuration>\n";

        public virtual string Build() {
            return webConfigTop + resizerTop + resizerContents + resizerBottom + compilationTop + webConfigBottom;
        }
    }
}
