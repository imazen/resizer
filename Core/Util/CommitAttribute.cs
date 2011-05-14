using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Util {

    [AttributeUsage(AttributeTargets.Assembly)]
    public class CommitAttribute : Attribute {
          
       string guid;
       public CommitAttribute() { guid = string.Empty; }
       public CommitAttribute(string txt) { guid = txt; }

       public string Value { get { return guid; } }
       public override string ToString() {
           return guid;
       }
        
    }

}
