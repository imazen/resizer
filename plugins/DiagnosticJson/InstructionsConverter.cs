// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ImageResizer.Plugins.DiagnosticJson
{
    public class InstructionsConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanWrite { get { return true; } }
        public override bool CanRead { get { return false; } }

        public override bool CanConvert(Type objectType)
        {
            // We only support ResizeSettings/NameValueCollection at present.
            // Support for the Instructions class may be added later.
            return (objectType == typeof(ResizeSettings) ||
                    objectType == typeof(NameValueCollection));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // We only support ResizeSettings/NameValueCollection at present.
            // Support for the Instructions class may be added later.
            this.WriteNameValueCollection(writer, (NameValueCollection)value, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private void WriteNameValueCollection(JsonWriter writer, NameValueCollection nvc, JsonSerializer serializer)
        {
            if (nvc.HasKeys())
            {
                writer.WriteStartObject();

                foreach (string key in nvc.Keys)
                {
                    // ResizeSettings seems to end up with a null=>"text" entry.
                    // We never want that to show in the output!
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    writer.WritePropertyName(key);
                    serializer.Serialize(writer, nvc[key]);
                }

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
