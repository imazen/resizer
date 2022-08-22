// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Web.Mvc;

namespace MvcSample.Controllers
{
    public class PhotosController : Controller
    {
        //
        // GET: /Photos/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Upload()
        {
            return View();
        }

        public ActionResult UploadFiles()
        {
            //Loop through each uploaded file
            foreach (string fileKey in Request.Files.Keys)
            {
                var file = Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Yes, 0-length files happen.

                new Photo(file).Save();
            }

            return RedirectToAction("Index");
        }
    }
}