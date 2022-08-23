// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;

namespace ImageResizer.Util
{
    [AttributeUsage(AttributeTargets.Assembly)]
    [Obsolete("Use Imazen.Common.Licesning.CommitAttribute instead, this will not be recognized")]
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

    [Obsolete("Use Imazen.Common.Licesning.BuildDateAttribute instead, this will not be recognized")]
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
    [Obsolete("Native dependencies are no longer supported due to misuse and startup delays")]
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
    [Obsolete("Use Imazen.Common.Licesning.EditionAttribute instead, this will not be recognized")]
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
    [Obsolete("No longer used.")]
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