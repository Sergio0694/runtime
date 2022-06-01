// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public partial class Object
    {
        // Returns a Type object which represent this object instance.
        [Intrinsic]
        public unsafe Type GetType()
        {
            MethodTable* pMethodTable = RuntimeHelpers.GetMethodTable(this);

            Type type;

            // Logging will be done by the slow path (this line just gets the writeable data without it).
            // Inlined logic for GetWriteableData_NoLogging()->GetExposedClassObjectHandle().
            nint loaderHandle = pMethodTable->m_pWriteableData->m_hExposedClassObject;

            // If the slot value does have the low bit set, then it is a simple pointer to the value
            // Otherwise, we will need a more complicated operation to get the value.
            // This first check and assignment is the inlined logic for LoaderAllocator::GetHandleValueFast.
            if (((nuint)loaderHandle & 1) != 0)
            {
                // *pValue = *((OBJECTREF *)(((UINT_PTR)handle) - 1));
                type = Unsafe.As<nint, Type>(ref *(nint*)(((nuint)loaderHandle) - 1));
            }
            else
            {
                type = InternalGetType();
            }

            GC.KeepAlive(this);

            return type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern Type InternalGetType();

        // Returns a new object instance that is a memberwise copy of this
        // object.  This is always a shallow copy of the instance. The method is protected
        // so that other object may only call this method on themselves.  It is intended to
        // support the ICloneable interface.
        [Intrinsic]
        protected unsafe object MemberwiseClone()
        {
            object clone = RuntimeHelpers.AllocateUninitializedClone(this);

            // copy contents of "this" to the clone

            nuint byteCount = RuntimeHelpers.GetRawObjectDataSize(clone);
            ref byte src = ref this.GetRawData();
            ref byte dst = ref clone.GetRawData();

            if (RuntimeHelpers.GetMethodTable(clone)->ContainsGCPointers)
                Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount);
            else
                Buffer.Memmove(ref dst, ref src, byteCount);

            return clone;
        }
    }
}
