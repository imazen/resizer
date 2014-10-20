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


    

    [AttributeUsage(AttributeTargets.Assembly)]
    public class NativeDependenciesAttribute : Attribute {

        string type;
        public NativeDependenciesAttribute() { type = string.Empty; }
        public NativeDependenciesAttribute(string txt) { type = txt; }

        public string Value { get { return type; } }
        public override string ToString() {
            return type;
        }
    }



    [AttributeUsage(AttributeTargets.Assembly)]
    public class EditionAttribute : Attribute
    {

        string type;
        public EditionAttribute() { type = string.Empty; }
        public EditionAttribute(string txt) { type = txt; }

        public string Value { get { return type; } }
        public override string ToString()
        {
            return type;
        }
    }



    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildTypeAttribute : Attribute {

        string type;
        public BuildTypeAttribute() { type = string.Empty; }
        public BuildTypeAttribute(string txt) { type = txt; }

        public string Value { get { return type; } }
        public override string ToString() {
            return type;
        }

    }


}
