using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Web;
using System.Web.Caching;
using System.Drawing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.CustomOverlay {
    /// <summary>
    /// Understands &amp;designid=504&amp;mastid=8540&amp;colorid=9633 syntax. Caches database lookups so that repeat requests are fast. Only supports 1 overlay per image. 
    /// Does
    /// </summary>
    public class CachedOverlayProvider:IOverlayProvider, IQuerystringPlugin {
        /// <summary>
        /// If true, null values (missing rows) will be cached. 
        /// This makes requests for non-existent designs fast, but also means it could take 1 second (or whatever the sqlcachedependency polling value is) for them to appear after they are created.
        /// </summary>
        public bool CacheNullValues { get; set; }
        /// <summary>
        /// The query to get all required data
        /// </summary>
        public string SqlQuery { get; set; }
        /// <summary>
        /// The names of the tables to watch - any changes to them will invalidate the cached data.
        /// </summary>
        public string[] WatchTables { get; set; }
        /// <summary>
        /// The name of the sqlCacheDependency entry in web.config
        /// </summary>
        public string SqlDependencyName { get; set; }

        /// <summary>
        /// The name of the connection string in web.config
        /// </summary>
        public string ConnectionStringName { get; set; }

        /// <summary>
        /// The directory containing the overlay files, in virtual form (I.e. ~/images/overlays or /app/images/overlays)
        /// </summary>
        public string OverlayBasePath { get; set; }

         
        /// <summary>
        /// If true, caching of database results will be enabled.
        /// </summary>
        public bool CacheDb { get; set; }

        public CachedOverlayProvider(NameValueCollection args):this(args["connectionStringName"],args["sqlDependencyName"],args["overlayBasePath"]) {
            CacheDb = Utils.getBool(args, "cachedb", true);
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "designid", "mastid", "colorid","orgid","logousageid" };
        }

        public CachedOverlayProvider(string connectionStringName, string sqlDependencyName, string overlayBasePath) {
            this.ConnectionStringName = connectionStringName;
            this.SqlDependencyName = sqlDependencyName;
            this.CacheNullValues = true;
            this.OverlayBasePath = overlayBasePath;

            WatchTables = new string[] { "LogoDesignMap", "LogoDesign", "LogoUsage", "Organization", "LogoImage2", "Color" };

//            SqlQuery = @"SELECT
//                            Organization.NickName, 
//                            LogoDesign.Description,
//                            Color.IsDarkColor,
//                            LogoUsage.Description,
            //                            X1, Y1, X2, Y2, X4, Y4, X3, Y3, 
//
//                            FROM LogoDesignMap 
//                            INNER JOIN LogoDesign 
//		                            ON (LogoDesignMap.LogoDesignID = LogoDesign.LogoDesignID)
//                            INNER JOIN LogoUsage
//		                            ON (LogoDesignMap.LogoUsageID = LogoUsage.LogoUsageID)
//                            INNER JOIN Organization 
//		                            ON (LogoDesignMap.OrgID = Organization.OrgID)
//                            INNER JOIN LogoImage2
//		                            ON (LogoImage2.LogoPosition = LogoUsage.Description AND
//		                            LogoImage2.MastID = @masterid AND LogoImage2.ColorID = @colorid)
//                            INNER JOIN Color
//		                            ON (Color.ColorID = @colorid)
//                            WHERE LogoDesignMap.ID = @designmapid AND LogoImage2.IsLegacy = 0";

            SqlQuery = @"SELECT
                            NickName, 
                            DesignDesc,
                            Color.IsDarkColor,
                            UsageDesc,
                            X1, Y1, X2, Y2, X4, Y4, X3, Y3, 
                            LogoAlignment

                            FROM LogoDesignView 
                            INNER JOIN LogoImage2
                                    ON (LogoDesignView.LogoUsageId = LogoImage2.LogoUsageID AND LogoDesignView.MastId = LogoImage2.MastID AND
                                    LogoImage2.MastID = @masterid AND LogoImage2.ColorID = @colorid)
                            INNER JOIN Color
                                    ON (Color.ColorID = @colorid)
                            WHERE LogoDesignID = @designid AND LogoDesignView.OrgID=@orgid and LogoImage2.LogoUsageID=@logousageid AND LogoImage2.IsLegacy = 0 ";

            

            cachedDataKey = this.GetType().ToString() + new Random().Next().ToString(); //establish a unique cache key for the ASP.NET cache
        }
        private SqlConnection GetConnection() {
            var s = System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (s == null) throw new ArgumentException("The specified connection string " + ConnectionStringName + " does not exist in Web.config");
            return new SqlConnection(s.ConnectionString);
        }

        //private Overlay GetOverlayFromDb(int designMapId, int masterId, int colorId) {
        private Overlay GetOverlayFromDb(int designId, int masterId, int colorId, int orgId, int logoUsageId)
        {


            using (var conn = GetConnection()) {
                conn.Open();
                var c = new SqlCommand(SqlQuery, conn);
                c.Parameters.AddWithValue("designid", designId);
                c.Parameters.AddWithValue("masterid", masterId);
                c.Parameters.AddWithValue("colorid", colorId);
                c.Parameters.AddWithValue("orgid", orgId);
                c.Parameters.AddWithValue("logousageid", logoUsageId);

                using (var r = c.ExecuteReader(System.Data.CommandBehavior.SingleRow)) {
                    if (!r.HasRows) return null; //No results?
                    r.Read();

                    /*NickName	Description	IsDarkColor	Description X1	Y1	X2	Y2	X3	Y3	X4	Y4
                    CampusCloz	Alumni 1	1	Full Chest 309	289	705	289	309	384	705	384	*/
                    /*
                    So working through this example which has real values from the NEW sample db I provided:
                    · designid=3233 locates for us LogoDesignMap.ID=3233 which represents a 'Alumni 1' logo design for Org=Campus Cloz in the Full Chest position.
                    · mastid=9464, colorid=1577, and Logo Position=Full Chest are used to locate LogoImage2.ID=63392. Now we know the source product image (1020_99_z.jpg), coordinates for the logo, and the logo position (this answers your question).
                    · colorid=1577 tells us the color is a dark color (Color.IsDarkColor=1). We need to know if color is light or dark because we put white logos on dark colors only, and dark logos on light colors only.
                     * Now we know enough to build the logo filename. This was described in earlier email, but I'll repeat: 
                     * Our logo image file names will use a strict naming convention i.e. [Organization.NickName]_[Logo Design Name]_[Light or Dark]_[Logo Position].png, 
                     * i.e. CampusCloz_athletics9_light_leftsleeve.png. So, the logo image name should be: CampusCloz_athletics1_dark_FullChest.png. Note we collapse spaces in these file names, i.e. 'Campus Cloz' becomes 'CampusCloz', 'Athletics 1' -> 'Athletics1, 'Full Chest' -> FullChest

                     * */


                    //Build overlay path
                    Overlay o = new Overlay();
                    StringBuilder p = new StringBuilder();
                    //p.AppendFormat("{4}/{0}_{1}_{2}_{3}.png", r.GetString(0), r.GetString(1), r.GetBoolean(2) ? "dark" : "light", r.GetString(3), this.OverlayBasePath.TrimEnd('/'));
                    p.AppendFormat("{4}/{0}/LogoImages/{0}_{1}_{2}_{3}.png", r.GetString(0), r.GetString(1), r.GetBoolean(2) ? "dark" : "light", r.GetString(3), this.OverlayBasePath.TrimEnd('/'));
                    p.Replace(" ", "");
                    o.OverlayPath = p.ToString();

                    //Parse logo position and use it to fill in magic values
                    string logoPosition = r.GetString(3).Replace(" ", "");
                    LogoPosition type = (LogoPosition)Enum.Parse(typeof(LogoPosition), logoPosition, true);
                    this.ApplyLogoPositionMagicValues(o, type);


                    o.Align = (ContentAlignment)r.GetInt32(12);     //LogoAlignment
                    



                    //Store coordinates
                    o.Poly = new PointF[4];
                    for (int i = 0; i < 4; i++) {
                        int x = r.GetInt32(4 + (i * 2));
                        int y = r.GetInt32(5 + (i * 2));
                        o.Poly[i] = new PointF(x, y);
                    }

                    return o;
                }
            }

        }

        private object cachedDataSync = new object();

        private string cachedDataKey = null;

        /// <summary>
        /// Returns an enumeration containing a single Overlay instance for the given request.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<Overlay> GetOverlays(string virtualPath, NameValueCollection query) {
            string sDesign = query["designid"];
            string sMaster = query["mastid"];
            string sColor = query["colorid"];
            string sOrgId = query["orgid"];
            string sLogoUsageId = query["logousageid"];
            if (string.IsNullOrEmpty(sDesign) || string.IsNullOrEmpty(sMaster) || string.IsNullOrEmpty(sColor) || string.IsNullOrEmpty(sOrgId) || string.IsNullOrEmpty(sLogoUsageId)) return null; //Don't process this image, it's not ours

            int designId = 0;
            int masterId = 0;
            int colorId = 0;
            int orgId = 0;
            int logoUsageId = 0;

            if (!int.TryParse(sDesign, out designId) || !(int.TryParse(sMaster, out masterId)) || !int.TryParse(sColor, out colorId) || !int.TryParse(sOrgId, out orgId) || !int.TryParse(sLogoUsageId, out logoUsageId)) return null; //Invalid numbers, fail silently.

            string cacheKey = sDesign + "_" + sMaster + "_" + sColor + "_" + sOrgId + "_" + sLogoUsageId;
            var cachedData = HttpRuntime.Cache[cachedDataKey] as Dictionary<string,Overlay>;
            //Load from cache if present
            if (CacheDb && cachedData != null) lock (cachedDataSync) {
                    Overlay temp;
                    if (cachedData.TryGetValue(cacheKey, out temp) && (temp != null || CacheNullValues)) {
                        return temp == null ? null :  new Overlay[] { temp };
                    }
                }


            //Cache mis. Let's do our SQL
            Overlay o = GetOverlayFromDb(designId, masterId, colorId, orgId, logoUsageId);
            if (!CacheNullValues && o == null) return null; //Item doesn't exist

            if (CacheDb) {
                //Save in cache
                lock (cachedDataSync) {
                    //Perform lookup again, don't want to overwrite it
                    var cachedData2 = HttpRuntime.Cache[cachedDataKey] as Dictionary<string, Overlay>;
                    //Ensure the dictionary exists
                    if (cachedData2 == null) {
                        cachedData2 = new Dictionary<string, Overlay>();
                        HttpRuntime.Cache.Add(cachedDataKey, cachedData2, GetDependencies(), Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
                    }
                    cachedData2[cacheKey] = o;
                }
            }
            return (o == null) ? null : new Overlay[]{o};
        }



        private CacheDependency GetDependencies() {
            if (string.IsNullOrEmpty(SqlDependencyName)) return null;
            var a = new AggregateCacheDependency();
            foreach (string t in WatchTables) {
                a.Add(new SqlCacheDependency(SqlDependencyName, t));
            }

            return a;
        }


        /// <summary>
        /// Applies the scaling values based on the logo position
        /// </summary>
        /// <param name="o"></param>
        /// <param name="lp"></param>
        public void ApplyLogoPositionMagicValues(Overlay o, LogoPosition lp){

            //Apply arbitrary pixel scaling factors
            switch (lp) {
                case LogoPosition.CenterChest:
                    //6.5" max logo width 
                    o.PolyWidthInLogoPixels = 218;
                    break;
                case LogoPosition.FullChest:
                    //12" max logo width 
                    o.PolyWidthInLogoPixels = 402;
                    break;
                case LogoPosition.LeftChest:
                    //5.88" max logo width 
                    o.PolyWidthInLogoPixels = 197;
                    break;
                case LogoPosition.LeftThigh:
                    //4.5" max logo width 
                    o.PolyWidthInLogoPixels = 151;
                    break;
                case LogoPosition.VerticalLeg:
                case LogoPosition.LeftSleeve:
                    //3" max logo width - width of logo is left to right with logo hanging vertically 
                    //o.PolyWidthInLogoPixels = 101; - In fact, the previous algorithm NEVER applied this value, so specifying it will cause an undesired result.
                    o.PolyHeightInLogoPixels = 469; //14" logo 
                    break;
                case LogoPosition.General:
                    //3.5" 
                    o.PolyWidthInLogoPixels = 118;
                    break;
            }

        }

        public enum LogoPosition {
            FullChest, //1
            CenterChest, //3
            LeftChest, //2
            LeftThigh, //6
            VerticalLeg, //12
            LeftSleeve, //4
            General, //11
            RightSleeve, //5
            Butt, //7
            CenterChestOnBack,  //14
            FullChestOnBack,    //15
            NeckOnBack, //16
            Unknown
        }


        
    }
}
