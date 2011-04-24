/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.SqlReader {

    public delegate void AuthorizeEvent(String id);

    /// <summary>
    /// Holds settings used to create a SqlReader
    /// </summary>
    public class SqlReaderSettings {

        public SqlReaderSettings() {
        }

        /// <summary>
        /// Called before accessing any row in the database. The row ID is passed as a string argument.
        /// If the current user should not access the row, throw an HttpException(403, "message")
        /// </summary>
        public event AuthorizeEvent BeforeAccess;

        /// <summary>
        /// Fires the BeforeAccess event
        /// </summary>
        /// <param name="id"></param>
        public void FireBeforeAccess(string id) {
            if (BeforeAccess != null) FireBeforeAccess(id);
        }

        private string pathPrefix = "~/databaseimages/";
        /// <summary>
        /// Defines a virtual path where database images can be accessed. Defaults to "~/databaseimages"
        /// Ex image URL: localhost/databaseimages/4953.jpg
        /// </summary>
        public string PathPrefix {
            get { return pathPrefix; }
            set { pathPrefix = value; }
        }

        private string connectionString = null;
        /// <summary>
        /// The database connection string. Defaults to null.
        /// </summary>
        public string ConnectionString {
            get { return connectionString; }
            set { connectionString = value; }
        }

        private string imageBlobQuery =
            "SELECT Content FROM Images WHERE ImageID=@id";
        /// <summary>
        /// The query that returns the binary image data based on the ID. Defaults to "SELECT Content FROM Images WHERE ImageID=@id"
        /// </summary>
        public string ImageBlobQuery {
            get { return imageBlobQuery; }
            set { imageBlobQuery = value; }
        }

        private string modifiedDateQuery =
            "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
        /// <summary>
        /// The query that returns the modified and created date of the image.  Defaults to "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id".
        /// Of all the dates returned by the query, the first non-empty date is used.
        /// </summary>
        public string ModifiedDateQuery {
            get { return modifiedDateQuery; }
            set { modifiedDateQuery = value; }
        }

        private string imageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";
        /// <summary>
        /// The query that returns whether an image exists or not. Defaults to "Select COUNT(ImageID) From Images WHERE ImageID=@id"
        /// </summary>
        public string ImageExistsQuery {
            get { return imageExistsQuery; }
            set { imageExistsQuery = value; }
        }

        private System.Data.SqlDbType imageIdType = System.Data.SqlDbType.Int;
        /// <summary>
        /// Specifies the type of ID used for images. Int and GUID are the only valid values.
        /// </summary>
        public System.Data.SqlDbType ImageIdType {
            get { return imageIdType; }
            set {
                if (value != System.Data.SqlDbType.Int && value != System.Data.SqlDbType.TinyInt || value != System.Data.SqlDbType.SmallInt ||
                value != System.Data.SqlDbType.BigInt || value != System.Data.SqlDbType.UniqueIdentifier)
                    throw new ArgumentOutOfRangeException("Int, TinyInt, SmallInt, BigInt, and UniqueIdentifier are the only valid values for ImageIdType");

                imageIdType = value; 
            }
        }


    }
}
