using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace COMInstaller {
    public class RegCleaner {

        public RegCleaner(Regex rNamespace, Regex rAssembly, Regex rCodebase) {


        }

        public void ScanCLSIDs() {
            RegistryKey clsid = Registry.ClassesRoot.OpenSubKey("CLSID");
            string[] clsids = clsid.GetSubKeyNames();
        }

        /*
         * 

[HKEY_CLASSES_ROOT\ImageResizer.Util.StreamUtils]
@="ImageResizer.Util.StreamUtils"

[HKEY_CLASSES_ROOT\ImageResizer.Util.StreamUtils\CLSID]
@="{40E7FC61-BB05-3337-8BC2-6232DB29AFA3}"

[HKEY_CLASSES_ROOT\CLSID\{40E7FC61-BB05-3337-8BC2-6232DB29AFA3}]
@="ImageResizer.Util.StreamUtils"

[HKEY_CLASSES_ROOT\CLSID\{40E7FC61-BB05-3337-8BC2-6232DB29AFA3}\InprocServer32]
@="mscoree.dll"
"ThreadingModel"="Both"
"Class"="ImageResizer.Util.StreamUtils"
"Assembly"="ImageResizer, Version=3.0.12.42337, Culture=neutral, PublicKeyToken=null"
"RuntimeVersion"="v2.0.50727"
"CodeBase"="file:///C:/Users/Administrator/Documents/resizer/dlls/release/ImageResizer.dll"

[HKEY_CLASSES_ROOT\CLSID\{40E7FC61-BB05-3337-8BC2-6232DB29AFA3}\InprocServer32\3.0.12.42337]
"Class"="ImageResizer.Util.StreamUtils"
"Assembly"="ImageResizer, Version=3.0.12.42337, Culture=neutral, PublicKeyToken=null"
"RuntimeVersion"="v2.0.50727"
"CodeBase"="file:///C:/Users/Administrator/Documents/resizer/dlls/release/ImageResizer.dll"

[HKEY_CLASSES_ROOT\CLSID\{40E7FC61-BB05-3337-8BC2-6232DB29AFA3}\ProgId]
@="ImageResizer.Util.StreamUtils"

[HKEY_CLASSES_ROOT\CLSID\{40E7FC61-BB05-3337-8BC2-6232DB29AFA3}\Implemented Categories\{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}]

[HKEY_CLASSES_ROOT\Record\{4D88F4C4-359F-34DE-AD6C-3CFDDED35B20}\3.0.12.42337]
"Class"="ImageResizer.ServerCacheMode"
"Assembly"="ImageResizer, Version=3.0.12.42337, Culture=neutral, PublicKeyToken=null"
"RuntimeVersion"="v2.0.50727"
"CodeBase"="file:///C:/Users/Administrator/Documents/resizer/dlls/release/ImageResizer.dll"

[HKEY_CLASSES_ROOT\Record\{4299C2AA-1FD3-3836-9B13-1B8C1D21B47F}\3.0.12.42337]
"Class"="ImageResizer.ProcessWhen"
"Assembly"="ImageResizer, Version=3.0.12.42337, Culture=neutral, PublicKeyToken=null"
"RuntimeVersion"="v2.0.50727"
"CodeBase"="file:///C:/Users/Administrator/Documents/resizer/dlls/release/ImageResizer.dll"

[HKEY_CLASSES_ROOT\Record\{80079DCC-6670-3A2C-B4F6-4175DD7D9E28}\3.0.12.42337]
"Class"="ImageResizer.ScaleMode"
"Assembly"="ImageResizer, Version=3.0.12.42337, Culture=neutral, PublicKeyToken=null"
"RuntimeVersion"="v2.0.50727"
"CodeBase"="file:///C:/Users/Administrator/Documents/resizer/dlls/release/ImageResizer.dll"

         */

    }
}
