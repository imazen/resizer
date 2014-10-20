// Copyright (c) 2012 Jason Morse
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    ///   Provides a managed interface to Ghostscript API.
    /// </summary>
    /// <seealso href = "http://ghostscript.com/doc/current/API.htm" />
    public class GhostscriptEngine
    {
        private delegate int GhostscriptMessageEventHandler(IntPtr instance, IntPtr source, int length);

        private static readonly object _syncObject = new object();
        private static Version _nativeVersion;

        public static string NativeFileName
        {
            get { return GhostscriptNativeMethods.FileName; }
        }

        /// <summary>
        ///   Get the Ghostscript native library version number.
        /// </summary>
        /// <value>
        ///   Native library version number, if available, otherwise null.
        /// </value>
        public static Version NativeVersion
        {
            get
            {
                if(_nativeVersion == null)
                {
                    _nativeVersion = GetNativeVersion();
                }
                return _nativeVersion;
            }
        }

        /// <summary>
        ///   Determines if the Ghostscript engine is available.
        /// </summary>
        /// <returns>
        ///   True if the Ghostscript engine is available, otherwise false.
        /// </returns>
        /// <remarks>
        ///   If the Ghostscript engine is not available, it's typically because the native Ghostscript library cannot 
        ///   be loaded or has a version that is lower than 9.
        /// </remarks>
        public static bool IsAvailable()
        {
            Version version = NativeVersion;
            return version != null && version.Major >= 9;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Intended to be member method.")]
        public string Execute(GhostscriptSettings settings)
        {
            if(settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            // Flatten settings into Ghostscript arguments
            string[] arguments = GetArguments(settings);

            // Use callback handlers to capture stdout and stderr. 
            // NOTE: Delegates do not need to be pinned.
            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();
            GhostscriptMessageEventHandler outputHandler = (i, s, l) => HandleOutputMessage(outputBuilder, s, l);
            GhostscriptMessageEventHandler errorHandler = (i, s, l) => HandleOutputMessage(errorBuilder, s, l);
            GhostscriptMessageEventHandler inputHandler = (i, s, l) => l;

            // NOTE: Ghostscript supports only one instance per process
            int result;
            lock(_syncObject)
            {
                // Create a new instance of Ghostscript. This instance is passed to most other gsapi functions. 
                // The caller_handle will be provided to callback functions. 
                IntPtr instance;
                GhostscriptNativeMethods.NewInstance(out instance, IntPtr.Zero);

                // Set the callback functions for stdio.
                GhostscriptNativeMethods.SetMessageHandlers(instance, inputHandler, outputHandler, errorHandler);

                // Initialise the interpreter. 
                // This calls gs_main_init_with_args() in imainarg.c. See below for return codes. 
                // The arguments are the same as the "C" main function: argv[0] is ignored and the user supplied arguments are 
                // argv[1] to argv[argc-1].
                result = GhostscriptNativeMethods.InitializeWithArguments(instance, arguments.Length, arguments);

                // Exit the interpreter. 
                // This must be called on shutdown if gsapi_init_with_args() has been called, and just before gsapi_delete_instance().
                GhostscriptNativeMethods.Exit(instance);

                // Destroy an instance of Ghostscript. 
                // Before you call this, Ghostscript must have finished. 
                // If Ghostscript has been initialised, you must call gsapi_exit before gsapi_delete_instance.
                GhostscriptNativeMethods.DeleteInstance(instance);
            }

            // Check for errors. Zero and e_Quit(-101) are not errors.
            string output = outputBuilder.ToString();
            if(result != 0 && result != -101)
            {
                // Use error as message if output is empty
                string error = errorBuilder.ToString();
                if(string.IsNullOrEmpty(output))
                {
                    output = error;
                }

                GhostscriptException exception = new GhostscriptException(output);
                exception.Data["args"] = arguments;
                exception.Data["stderr"] = error;
                throw exception;
            }

            // Return the output message
            return output;
        }

        private static string[] GetArguments(GhostscriptSettings settings)
        {
            // Flatten Ghostscript engine settings into string array
            // - first argument ignore (argv[0])
            // - input path as last argument
            string[] arguments;
            List<string> argumentItems = new List<string> { string.Empty };
            foreach(string key in settings.AllKeys)
            {
                string[] values = settings.GetValues(key);
                if(values == null)
                {
                    // No value
                    argumentItems.Add(key);
                }
                else
                {
                    IEnumerable<string> items = values.Select(value => string.Concat(key, value));
                    argumentItems.AddRange(items);
                }
            }
            arguments = argumentItems.ToArray();
            return arguments;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security not exposed.")]
        private static Version GetNativeVersion()
        {
            // Get the Ghostscript version. 
            //      GPL Ghostscript
            //      Copyright Some Company
            //      815
            //      20040922            
            // Extracted as a structure but we're only interested in the 3rd field (815).
            GhostscriptNativeMethods.GhostscriptVersion ghostscriptVersion = new GhostscriptNativeMethods.GhostscriptVersion();
            try
            {
                GhostscriptNativeMethods.GetRevision(ref ghostscriptVersion, Marshal.SizeOf(ghostscriptVersion));
            }
            catch(DllNotFoundException)
            {
                return null;
            }
            catch(MissingMethodException)
            {
                return null;
            }

            // Convert version from integer such as '914' to Version object 9.14
            string versionText = ((double)ghostscriptVersion.Revision / 100).ToString(CultureInfo.InvariantCulture);
            return new Version(versionText);
        }

        public int HandleOutputMessage(StringBuilder builder, IntPtr source, int length)
        {
            string text;
            if(length > 0)
            {
                // Marshal unmanaged output to local buffer
                byte[] buffer = new byte[length];
                Marshal.Copy(source, buffer, 0, buffer.Length);

                // Determine encoding style
                System.Text.Encoding encoding = System.Text.Encoding.ASCII;
                if(length > 2)
                {
                    // Try to detect if UNICODE by looking for byte order mark and assigning appropriate encoder
                    if(buffer[0] == 0xFE && buffer[1] == 0xFF)
                    {
                        encoding = System.Text.Encoding.BigEndianUnicode;
                    }
                    else if(buffer[0] == 0xFF && buffer[1] == 0xFE)
                    {
                        encoding = System.Text.Encoding.Unicode;
                    }
                }

                // Perform decode
                text = encoding.GetString(buffer, 0, buffer.Length);
                builder.Append(text);
            }
            return length;
        }

        private static class GhostscriptNativeMethods
        {
            private static readonly bool _is64BitProcess = IntPtr.Size == 8;

            public static string FileName
            {
                get
                {
                    if(_is64BitProcess)
                    {
                        return FileName64;
                    }
                    return FileName32;
                }
            }

            public static int GetRevision(ref GhostscriptVersion revision, int length)
            {
                if(_is64BitProcess)
                {
                    return GetRevision64(ref revision, length);
                }
                return GetRevision32(ref revision, length);
            }

            public static int NewInstance(out IntPtr instance, IntPtr handle)
            {
                if(_is64BitProcess)
                {
                    return NewInstance64(out instance, handle);
                }
                return NewInstance32(out instance, handle);
            }

            public static int InitializeWithArguments(IntPtr instance, int argc, string[] argv)
            {
                if(_is64BitProcess)
                {
                    return InitializeWithArguments64(instance, argc, argv);
                }
                return InitializeWithArguments32(instance, argc, argv);
            }

            public static int Exit(IntPtr instance)
            {
                if(_is64BitProcess)
                {
                    return Exit64(instance);
                }
                return Exit32(instance);
            }

            public static void DeleteInstance(IntPtr instance)
            {
                if(_is64BitProcess)
                {
                    DeleteInstance64(instance);
                }
                else
                {
                    DeleteInstance32(instance);
                }
            }

            public static int SetMessageHandlers(IntPtr instance, GhostscriptMessageEventHandler inputHandler, GhostscriptMessageEventHandler outputHandler, GhostscriptMessageEventHandler errorHandler)
            {
                if(_is64BitProcess)
                {
                    return SetMessageHandlers64(instance, inputHandler, outputHandler, errorHandler);
                }
                return SetMessageHandlers32(instance, inputHandler, outputHandler, errorHandler);
            }

            #region 32bit

            private const string FileName32 = "gsdll32.dll";

            [DllImport(FileName32, EntryPoint = "gsapi_revision")]
            private static extern int GetRevision32(ref GhostscriptVersion revision, int length);

            [DllImport(FileName32, EntryPoint = "gsapi_new_instance")]
            private static extern int NewInstance32(out IntPtr instance, IntPtr caller_handle);

            [DllImport(FileName32, EntryPoint = "gsapi_init_with_args", CharSet = CharSet.Ansi)]
            private static extern int InitializeWithArguments32(IntPtr instance, int argc, string[] argv);

            [DllImport(FileName32, EntryPoint = "gsapi_exit")]
            private static extern int Exit32(IntPtr instance);

            [DllImport(FileName32, EntryPoint = "gsapi_delete_instance")]
            private static extern void DeleteInstance32(IntPtr instance);

            [DllImport(FileName32, EntryPoint = "gsapi_set_stdio", CharSet = CharSet.Ansi)]
            private static extern int SetMessageHandlers32(IntPtr instance, GhostscriptMessageEventHandler inputHandler, GhostscriptMessageEventHandler outputHandler, GhostscriptMessageEventHandler errorHandler);

            #endregion

            #region 64bit

            private const string FileName64 = "gsdll64.dll";

            [DllImport(FileName64, EntryPoint = "gsapi_revision")]
            private static extern int GetRevision64(ref GhostscriptVersion revision, int length);

            [DllImport(FileName64, EntryPoint = "gsapi_new_instance")]
            private static extern int NewInstance64(out IntPtr pinstance, IntPtr caller_handle);

            [DllImport(FileName64, EntryPoint = "gsapi_init_with_args", CharSet = CharSet.Ansi)]
            private static extern int InitializeWithArguments64(IntPtr instance, int argc, string[] argv);

            [DllImport(FileName64, EntryPoint = "gsapi_exit")]
            private static extern int Exit64(IntPtr instance);

            [DllImport(FileName64, EntryPoint = "gsapi_delete_instance")]
            private static extern void DeleteInstance64(IntPtr instance);

            [DllImport(FileName64, EntryPoint = "gsapi_set_stdio", CharSet = CharSet.Ansi)]
            private static extern int SetMessageHandlers64(IntPtr instance, GhostscriptMessageEventHandler inputHandler, GhostscriptMessageEventHandler outputHandler, GhostscriptMessageEventHandler errorHandler);

            #endregion

            [StructLayout(LayoutKind.Sequential)]
            public struct GhostscriptVersion
            {
                public readonly IntPtr Product;
                public readonly IntPtr Copyright;
                public readonly int Revision;
                public readonly int RevisionDate;
            }
        }
    }
}