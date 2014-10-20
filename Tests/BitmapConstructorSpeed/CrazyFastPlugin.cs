using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.IO;
using ImageResizer.Util;
using System.Drawing;
using System.Reflection;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.CrazyFast {
    public class CrazyFastPlugin:BuilderExtension, IPlugin {
        public CrazyFastPlugin() {
        }

        public override System.Drawing.Bitmap DecodeStream(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            bool useICM = true;
            if (settings != null && "true".Equals(settings["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;
            Bitmap b;
            //NDJ - May 24, 2011 - Copying stream into memory so the original can be closed safely.
            MemoryStream ms = s.CopyToMemoryStream();
            //b = new Bitmap(ms, useICM); 
            b = FastBitmapLoader(ms, useICM);
            b.Tag = new BitmapTag(optionalPath, ms); //May 25, 2011: Storing a ref to the MemorySteam so it won't accidentally be garbage collected.
            return b;

        }


        public static Bitmap FastBitmapLoader(Stream s, bool useIcm) {
            IntPtr zero = IntPtr.Zero;
            int num = 0;
            if (useIcm) {
                Assembly drawing = Assembly.GetAssembly(typeof(Bitmap));
                MethodInfo mi =
                    drawing.GetType("System.Drawing.SafeNativeMethods+Gdip").GetMethod("GdipCreateBitmapFromStreamICM", BindingFlags.Static | BindingFlags.NonPublic);

                object gpstream = drawing.GetType("System.Drawing.Internal.GPStream").GetConstructors
            (BindingFlags.Instance | BindingFlags.NonPublic)[0].Invoke(new object[] { s });

                object[] args = new object[] { gpstream, zero };
                num = (int)mi.Invoke(null, args);
                zero = (IntPtr)args[1];
            } else {
                //num = SafeNativeMethods.Gdip.GdipCreateBitmapFromStream(new GPStream(stream), out zero);
            }
            if (num != 0) {
                //throw SafeNativeMethods.Gdip.StatusException(num);
            }

            MethodInfo fromGDI = typeof(Bitmap).GetMethod("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static);


            return fromGDI.Invoke(null, new object[] { zero }) as Bitmap;
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
