
using System;
using System.Runtime.InteropServices;

namespace ImageResizer.Plugins.Wic.InteropServices
{
    public static partial class NativeMethods
    {
        [Flags]
        public enum GenericAccessRights : uint
        {
            GENERIC_READ    = 0x80000000,
            GENERIC_WRITE   = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL     = 0x10000000,

            GENERIC_READ_WRITE = GENERIC_READ | GENERIC_WRITE
        }
    }
}

