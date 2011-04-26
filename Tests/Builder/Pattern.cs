using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageResizer.ReleaseBuilder {
    public class Pattern:Regex {
        public Pattern(string pattern):base(pattern.Replace("/","\\\\").Replace(".","\\.").Replace("*","."), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline) {

        }


    }
}
