// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer.Configuration;
using ImageResizer.Plugins.MongoReader;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using System.IO;
using ImageResizer;

namespace MongoReaderSample {
    public partial class Upload : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {

            var bucket = Config.Current.Plugins.Get<MongoReaderPlugin>().GridFSBucket;

            //Loop through each uploaded file
            foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
                HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Skip unused file controls.

                //Resize to a memory stream, max 2000x2000 jpeg
                MemoryStream temp = new MemoryStream(4096);
                new ImageJob(file.InputStream,temp,new ResizeSettings("width=2000;height=2000;mode=max;format=jpg")).Build();
                //Reset the stream
                temp.Seek(0, SeekOrigin.Begin);

                var options = new GridFSUploadOptions {
                    Metadata = new BsonDocument("contentType", file.ContentType)
                };

                var id = bucket.UploadFromStream(Path.GetFileName(file.FileName), temp, options);

                lit.Text += "<img src=\"" + ResolveUrl("~/gridfs/id/") + id + ".jpg?width=100&amp;height=100\" />";
            }

        }
    }
}