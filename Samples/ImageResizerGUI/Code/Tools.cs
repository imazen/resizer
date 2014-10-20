using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ImageResizerGUI.Code
{
    class FileTools
    {
        /// <summary>
        /// Taken from http://www.dotnetperls.com/recursively-find-files
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static List<string> GetFilesRecursive(string rootPath)
        {
            // 1.
            // Store results in the file results list.
            List<string> result = new List<string>();

            // 2.
            // Store a stack of our directories.
            Stack<string> stack = new Stack<string>();

            // 3.
            // Add initial directory.
            stack.Push(rootPath);

            // 4.
            // Continue while there are directories to process
            while (stack.Count > 0)
            {
                // A.
                // Get top directory
                string dir = stack.Pop();

                try
                {
                    // B
                    // Add all files at this directory to the result List.
                    // Now filtered by Image File Types
                    result.AddRange(Directory.GetFiles(dir, "*.bmp"));
                    result.AddRange(Directory.GetFiles(dir, "*.jpg"));
                    result.AddRange(Directory.GetFiles(dir, "*.gif"));
                    result.AddRange(Directory.GetFiles(dir, "*.png"));

                    // C
                    // Add all directories at this directory.
                    foreach (string dn in Directory.GetDirectories(dir))
                    {
                        stack.Push(dn);
                    }
                }
                catch
                {
                    // D
                    // Could not open the directory
                }
            }
            return result;
        }
    }

    class ContexMenuTools
    {
        //Extension - Extension of the file (.zip, .txt etc.)
        //MenuName - Name for the menu item (Play, Open etc.)
        //MenuDescription - The actual text that will be shown
        //MenuCommand - Path to executable

        /// <summary>
        /// A simple C# function to add context menu items in Explorer
        /// </summary>
        /// <param name="Extension">Extension of the file (.zip, .txt etc.)</param>
        /// <param name="MenuName">Name for the menu item (Play, Open etc.)</param>
        /// <param name="MenuDescription">The actual text that will be shown</param>
        /// <param name="MenuCommand">Path to executable</param>
        /// <returns></returns>
        private bool AddContextMenuItem(string Extension, string MenuName, string MenuDescription, string MenuCommand)
        {
            bool ret = false;

            RegistryKey rkey = Registry.ClassesRoot.OpenSubKey(Extension);

            if (rkey != null)
            {
                string extstring = rkey.GetValue("").ToString();

                rkey.Close();

                if (extstring != null)
                {
                    if (extstring.Length > 0)
                    {
                        rkey = Registry.ClassesRoot.OpenSubKey(extstring, true);

                        if (rkey != null)
                        {
                            string strkey = "shell\\" + MenuName + "\\command";

                            RegistryKey subky = rkey.CreateSubKey(strkey);

                            if (subky != null)
                            {
                                subky.SetValue("", MenuCommand);

                                subky.Close();

                                subky = rkey.OpenSubKey("shell\\" + MenuName, true);

                                if (subky != null)
                                {
                                    subky.SetValue("", MenuDescription);

                                    subky.Close();
                                }
                                ret = true;
                            }
                            rkey.Close();
                        }
                    }
                }
            }
            return ret;
        }
    }
}
