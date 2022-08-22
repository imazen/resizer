// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;

namespace ImageResizer.Util
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class CommitAttribute : Attribute
    {
        private string guid;

        public CommitAttribute()
        {
            guid = string.Empty;
        }

        public CommitAttribute(string txt)
        {
            guid = txt;
        }

        public string Value => guid;

        public override string ToString()
        {
            return guid;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateAttribute : Attribute
    {
        private string str;

        public BuildDateAttribute()
        {
            str = string.Empty;
        }

        public BuildDateAttribute(string txt)
        {
            str = txt;
        }

        public string Value => str;

        public DateTimeOffset? ValueDate
        {
            get
            {
                DateTimeOffset v;
                if (DateTimeOffset.TryParse(str, null, DateTimeStyles.RoundtripKind, out v))
                    return v;
                else
                    return null;
            }
        }

        public override string ToString()
        {
            return str;
        }
    }


    [AttributeUsage(AttributeTargets.Assembly)]
    public class NativeDependenciesAttribute : Attribute
    {
        private string type;

        public NativeDependenciesAttribute()
        {
            type = string.Empty;
        }

        public NativeDependenciesAttribute(string txt)
        {
            type = txt;
        }

        public string Value => type;

        public override string ToString()
        {
            return type;
        }
    }


    [AttributeUsage(AttributeTargets.Assembly)]
    public class EditionAttribute : Attribute
    {
        private string type;

        public EditionAttribute()
        {
            type = string.Empty;
        }

        public EditionAttribute(string txt)
        {
            type = txt;
        }

        public string Value => type;

        public override string ToString()
        {
            return type;
        }
    }


    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildTypeAttribute : Attribute
    {
        private string type;

        public BuildTypeAttribute()
        {
            type = string.Empty;
        }

        public BuildTypeAttribute(string txt)
        {
            type = txt;
        }

        public string Value => type;

        public override string ToString()
        {
            return type;
        }
    }
}