//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Test.Tools.WicCop.InteropServices.ComTypes
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal sealed class UnmanagedPropVariant
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CY
        {
            uint Lo;
            long Hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CAX
        {
            public uint count;
            public IntPtr ptr;
        }

        [FieldOffset(0)]
        public ushort vt;

        [FieldOffset(2)]
        public ushort reserved1;
        [FieldOffset(4)]
        public ushort reserved2;
        [FieldOffset(6)]
        public ushort reserved3;

        [FieldOffset(8)]
        public sbyte sbyteValue;
        [FieldOffset(8)]
        public byte byteValue;
        [FieldOffset(8)]
        public short int16Value;
        [FieldOffset(8)]
        public ushort uint16Value;
        [FieldOffset(8)]
        public int int32Value;
        [FieldOffset(8)]
        public uint uint32Value;
        [FieldOffset(8)]
        public long int64Value;
        [FieldOffset(8)]
        public ulong uint64Value;
        [FieldOffset(8)]
        public float floatValue;
        [FieldOffset(8)]
        public double doubleValue;

        [FieldOffset(8)]
        public IntPtr pointerValue;
        [FieldOffset(8)]
        public CAX vectorValue;
    }

    public enum PropVariantMarshalType
    {
        Automatic,
        Ascii,
        Blob
    }

    public sealed class PropVariant : IEquatable<PropVariant>
    {
        internal object value;
        internal PropVariantMarshalType marshalType;

        public PropVariant()
        {
            marshalType = PropVariantMarshalType.Automatic;
            value = null;
        }

        private void EnsureTypedArray()
        {
            if ((value is Array) && value.GetType().Equals(typeof(object[])))
            {
                Array objectArray = (Array)value;
                Array typedArray = Array.CreateInstance(objectArray.GetValue(0).GetType(), objectArray.Length);
                Array.Copy(objectArray, typedArray, objectArray.Length);
                value = typedArray;
            }
        }

        public PropVariant(object value)
        {
            marshalType = PropVariantMarshalType.Automatic;
            this.value = value;

            EnsureTypedArray();

            GetUnmanagedType();
        }

        public PropVariant(object value, PropVariantMarshalType marshalType)
        {
            this.marshalType = marshalType;
            this.value = value;

            EnsureTypedArray();
            GetUnmanagedType();
        }

        public object Value
        {
            get { return value; }
        }

        public PropVariantMarshalType MarshalType
        {
            get { return marshalType; }
        }

        public VarEnum GetUnmanagedType()
        {
            if (null == value) return VarEnum.VT_EMPTY;
            else if ((PropVariantMarshalType.Blob == marshalType) && value.GetType().Equals(typeof(byte[]))) return VarEnum.VT_BLOB;
            else if ((PropVariantMarshalType.Ascii == marshalType) && value.GetType().Equals(typeof(string))) return VarEnum.VT_LPSTR;
            else if ((PropVariantMarshalType.Ascii == marshalType) && value.GetType().Equals(typeof(string[]))) return VarEnum.VT_LPSTR | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(sbyte))) return VarEnum.VT_I1;
            else if (value.GetType().Equals(typeof(sbyte[]))) return VarEnum.VT_I1 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(byte))) return VarEnum.VT_UI1;
            else if (value.GetType().Equals(typeof(byte[]))) return VarEnum.VT_UI1 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(Int16))) return VarEnum.VT_I2;
            else if (value.GetType().Equals(typeof(Int16[]))) return VarEnum.VT_I2 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(UInt16))) return VarEnum.VT_UI2;
            else if (value.GetType().Equals(typeof(UInt16[]))) return VarEnum.VT_UI2 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(Int32))) return VarEnum.VT_I4;
            else if (value.GetType().Equals(typeof(Int32[]))) return VarEnum.VT_I4 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(UInt32))) return VarEnum.VT_UI4;
            else if (value.GetType().Equals(typeof(UInt32[]))) return VarEnum.VT_UI4 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(Int64))) return VarEnum.VT_I8;
            else if (value.GetType().Equals(typeof(Int64[]))) return VarEnum.VT_I8 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(UInt64))) return VarEnum.VT_UI8;
            else if (value.GetType().Equals(typeof(UInt64[]))) return VarEnum.VT_UI8 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(float))) return VarEnum.VT_R4;
            else if (value.GetType().Equals(typeof(float[]))) return VarEnum.VT_R4 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(double))) return VarEnum.VT_R8;
            else if (value.GetType().Equals(typeof(double[]))) return VarEnum.VT_R8 | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(bool))) return VarEnum.VT_BOOL;
            else if (value.GetType().Equals(typeof(bool[]))) return VarEnum.VT_BOOL | VarEnum.VT_VECTOR;
            else if (value.GetType().Equals(typeof(string))) return VarEnum.VT_LPWSTR;
            else if (value.GetType().Equals(typeof(string[]))) return VarEnum.VT_LPWSTR | VarEnum.VT_VECTOR;
            else if (Value.GetType().Equals(typeof(DateTime))) return VarEnum.VT_FILETIME;
            else if (value.GetType().IsCOMObject) return VarEnum.VT_UNKNOWN;

            else throw new NotImplementedException();
        }

        public bool Equals(PropVariant other)
        {
            if (null == other)
            {
                return false;
            }

            if ((Value is Array) && (other.Value is Array))
            {
                Array v1 = (Array)Value;
                Array v2 = (Array)other.Value;
                if (v1.Length != v2.Length) return false;
                for (int i = 0; i < v1.Length; i++)
                {
                    if (!v1.GetValue(i).Equals(v2.GetValue(i)))
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (!(Value is Array) && !(other.Value is Array))
            {
                return (Value == other.Value) || ((null != Value) && Value.Equals(other.Value));
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PropVariant);
        }

        public static bool operator ==(PropVariant a, PropVariant b)
        {
            if (null != (a as object))
            {
                return a.Equals(b);
            }
            else if (null != (b as object))
            {
                return b.Equals(a);
            }
            else
            {
                return true;
            }
        }

        public static bool operator !=(PropVariant a, PropVariant b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            StringBuilder build = new StringBuilder();
            build.Append((GetUnmanagedType() & ~VarEnum.VT_VECTOR).ToString() + ": ");
            if (null != Value)
            {
                if (Value is Array)
                {
                    foreach (object obj in (Array)Value)
                    {
                        build.Append(obj.ToString() + " ");
                    }
                }
                else
                {
                    build.Append(Value.ToString());
                }
            }
            return build.ToString();
        }
    }

    public sealed class PropVariantMarshaler : ICustomMarshaler
    {
        [DllImport("Ole32", PreserveSig = false)]
        private extern static void PropVariantClear([In][Out]            IntPtr pvar);

        private PropVariant propVariantReference;

        internal static T To<T>(IntPtr p) where T : struct
        {
            return (T)Marshal.PtrToStructure(p, typeof(T));
        }

        internal static T[] ToArrayOf<T>(IntPtr p, int size) where T : struct
        {
            T[] res = new T[size];
            for (int i = 0; i < size; i++)
            {
                res[i] = To<T>(new IntPtr(p.ToInt64() + i * Marshal.SizeOf(typeof(T))));
            }

            return res;
        }

        public static ICustomMarshaler GetInstance(string str)
        {
            return new PropVariantMarshaler();
        }

        public void CleanUpManagedData(object obj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            PropVariantClear(pNativeData);
            Marshal.FreeCoTaskMem(pNativeData);
        }

        private IntPtr AllocatePropVariant()
        {
            int size = Marshal.SizeOf(typeof(UnmanagedPropVariant));
            IntPtr pNativeData = Marshal.AllocCoTaskMem(size);

            for (int i = 0; i < size; i++)
            {
                Marshal.WriteByte(pNativeData, i, 0);
            }
            return pNativeData;
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            if (null == obj)
            {
                return IntPtr.Zero;
            }


            PropVariantMarshalType marshalType = PropVariantMarshalType.Automatic;

            if (obj is PropVariant)
            {
                propVariantReference = (PropVariant)obj;
                obj = propVariantReference.Value;
                marshalType = propVariantReference.MarshalType;
            }

            IntPtr pNativeData = AllocatePropVariant();

            if (null == obj)
            {
            }
            else if (!(obj is Array))
            {
                if (marshalType == PropVariantMarshalType.Ascii)
                {
                    UnmanagedPropVariant upv = new UnmanagedPropVariant();
                    upv.vt = (ushort)VarEnum.VT_LPSTR;
                    upv.pointerValue = Marshal.StringToCoTaskMemAnsi((string)obj);
                    Marshal.StructureToPtr(upv, pNativeData, false);
                }
                else if (obj is DateTime)
                {
                    UnmanagedPropVariant upv = new UnmanagedPropVariant();
                    upv.vt = (ushort)VarEnum.VT_FILETIME;
                    upv.int64Value = ((DateTime)obj).ToFileTimeUtc();
                    Marshal.StructureToPtr(upv, pNativeData, false);
                }
                else if (obj is string)
                {
                    UnmanagedPropVariant upv = new UnmanagedPropVariant();
                    upv.vt = (ushort)VarEnum.VT_LPWSTR;
                    upv.pointerValue = Marshal.StringToCoTaskMemUni((string)obj);
                    Marshal.StructureToPtr(upv, pNativeData, false);
                }
                else
                {
                    Marshal.GetNativeVariantForObject(obj, pNativeData);
                }
            }
            else if ((obj.GetType().Equals(typeof(byte[]))) || (obj.GetType().Equals(typeof(sbyte[]))) ||
                (obj.GetType().Equals(typeof(Int16[]))) || (obj.GetType().Equals(typeof(UInt16[]))) ||
                (obj.GetType().Equals(typeof(Int32[]))) || (obj.GetType().Equals(typeof(UInt32[]))) ||
                (obj.GetType().Equals(typeof(Int64[]))) || (obj.GetType().Equals(typeof(UInt64[]))) ||
                (obj.GetType().Equals(typeof(float[]))) || (obj.GetType().Equals(typeof(double[]))))
            {
                int count = ((Array)obj).Length;
                int elementSize = Marshal.SizeOf(obj.GetType().GetElementType());
                IntPtr pNativeBuffer = Marshal.AllocCoTaskMem(elementSize * count);

                for (int i = 0; i < count; i++)
                {
                    IntPtr pNativeValue = Marshal.UnsafeAddrOfPinnedArrayElement((Array)obj, i);
                    for (int j = 0; j < elementSize; j++)
                    {
                        byte value = Marshal.ReadByte(pNativeValue, j);
                        Marshal.WriteByte(pNativeBuffer, j + i * elementSize, value);
                    }
                }

                UnmanagedPropVariant upv = new UnmanagedPropVariant();
                upv.vectorValue.count = (uint)count;
                upv.vectorValue.ptr = pNativeBuffer;
                if (null == propVariantReference)
                {
                    upv.vt = (ushort)(new PropVariant(obj)).GetUnmanagedType();
                }
                else
                {
                    upv.vt = (ushort)propVariantReference.GetUnmanagedType();
                }

                Marshal.StructureToPtr(upv, pNativeData, false);
            }
            else if (obj.GetType().Equals(typeof(string[])))
            {
                int count = ((Array)obj).Length;
                IntPtr pNativeBuffer = Marshal.AllocCoTaskMem(IntPtr.Size * count);

                for (int i = 0; i < count; i++)
                {
                    IntPtr strPtr = Marshal.StringToCoTaskMemUni(((string[])obj)[i]);
                    Marshal.WriteIntPtr(pNativeBuffer, IntPtr.Size * i, strPtr);
                }

                UnmanagedPropVariant upv = new UnmanagedPropVariant();
                upv.vectorValue.count = (uint)count;
                upv.vectorValue.ptr = pNativeBuffer;
                if (null == propVariantReference)
                {
                    upv.vt = (ushort)(new PropVariant(obj)).GetUnmanagedType();
                }
                else
                {
                    upv.vt = (ushort)propVariantReference.GetUnmanagedType();
                }

                Marshal.StructureToPtr(upv, pNativeData, false);
            }
            else
            {
                throw new NotImplementedException();
            }

            return pNativeData;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if ((IntPtr.Zero == pNativeData) || (null == propVariantReference))
            {
                return null;
            }

            UnmanagedPropVariant unmanagedPropVariant = new UnmanagedPropVariant();
            Marshal.PtrToStructure(pNativeData, unmanagedPropVariant);

            if ((0 == (unmanagedPropVariant.vt & (ushort)VarEnum.VT_VECTOR)) && (unmanagedPropVariant.vt != (ushort)VarEnum.VT_BLOB))
            {
                switch ((VarEnum)unmanagedPropVariant.vt)
                {
                    case VarEnum.VT_CLSID:
                        {
                            if (unmanagedPropVariant.vectorValue.ptr != IntPtr.Zero)
                            {
                                propVariantReference.value = To<Guid>(unmanagedPropVariant.vectorValue.ptr);
                            }
                            return null;
                        }
                    case VarEnum.VT_EMPTY:
                        {
                            propVariantReference.value = null;
                            propVariantReference.marshalType = PropVariantMarshalType.Automatic;
                            return null;
                        }
                    case VarEnum.VT_LPSTR:
                        {
                            propVariantReference.value = Marshal.PtrToStringAnsi(unmanagedPropVariant.pointerValue);
                            propVariantReference.marshalType = PropVariantMarshalType.Ascii;
                            return null;
                        }
                    case VarEnum.VT_LPWSTR:
                        {
                            propVariantReference.value = Marshal.PtrToStringUni(unmanagedPropVariant.pointerValue);
                            propVariantReference.marshalType = PropVariantMarshalType.Automatic;
                            return null;
                        }
                    case VarEnum.VT_FILETIME:
                        {
                            propVariantReference.value = DateTime.FromFileTimeUtc(unmanagedPropVariant.int64Value);
                            propVariantReference.marshalType = PropVariantMarshalType.Automatic;
                            return null;
                        }
                    default:
                        {
                            propVariantReference.value = Marshal.GetObjectForNativeVariant(pNativeData);
                            propVariantReference.marshalType = PropVariantMarshalType.Automatic;
                            return null;
                        }
                }
            }
            else
            {
                VarEnum elementVt = (VarEnum)(unmanagedPropVariant.vt & ~(ushort)VarEnum.VT_VECTOR);
                int count = (int)unmanagedPropVariant.vectorValue.count;
                propVariantReference.marshalType = PropVariantMarshalType.Automatic;

                switch (elementVt)
                {
                    case VarEnum.VT_BLOB:
                        propVariantReference.marshalType = PropVariantMarshalType.Blob;
                        propVariantReference.value = ToArrayOf<byte>(unmanagedPropVariant.vectorValue.ptr, count); ;
                        break;
                    case VarEnum.VT_I1:
                        propVariantReference.value = ToArrayOf<sbyte>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_UI1:
                        propVariantReference.value = ToArrayOf<byte>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_I2:
                        propVariantReference.value = ToArrayOf<short>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_UI2:
                        propVariantReference.value = ToArrayOf<ushort>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_I4:
                        propVariantReference.value = ToArrayOf<int>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_UI4:
                        propVariantReference.value = ToArrayOf<uint>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_R4:
                        propVariantReference.value = ToArrayOf<float>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_UI8:
                        propVariantReference.value = ToArrayOf<ulong>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_I8:
                        propVariantReference.value = ToArrayOf<long>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_R8:
                        propVariantReference.value = ToArrayOf<double>(unmanagedPropVariant.vectorValue.ptr, count);
                        break;
                    case VarEnum.VT_LPWSTR:
                        propVariantReference.marshalType = PropVariantMarshalType.Automatic;
                        propVariantReference.value = GetArrayOfStrings(unmanagedPropVariant.vectorValue.ptr, count, Marshal.PtrToStringUni);
                        break;
                    case VarEnum.VT_LPSTR:
                        propVariantReference.marshalType = PropVariantMarshalType.Ascii;
                        propVariantReference.value = GetArrayOfStrings(unmanagedPropVariant.vectorValue.ptr, count, Marshal.PtrToStringAnsi);
                        break;
                    default: throw new NotImplementedException();
                }

                return null;
            }
        }

        static string[] GetArrayOfStrings(IntPtr ptr, int count, Converter<IntPtr, string> converter)
        {
            return Array.ConvertAll<IntPtr, string>(ToArrayOf<IntPtr>(ptr, count), converter);
        }

        public int GetNativeDataSize()
        {
            return -1;
        }
    }
}

