using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

        public ActionResult Upload() {
            return View();
        }

        public ActionResult UploadFiles() {

            //Loop through each uploaded file
            foreach (string fileKey in Request.Files.Keys) {
                HttpPostedFileBase file = Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Yes, 0-length files happen.

                new Photo(file).Save();
            }
            return RedirectToAction("Index");
        }


    }
}
