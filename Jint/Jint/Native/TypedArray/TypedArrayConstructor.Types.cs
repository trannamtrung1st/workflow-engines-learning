using Jint.Runtime;

namespace Jint.Native.TypedArray
{
    public sealed class Int8ArrayConstructor : TypedArrayConstructor
    {
        internal Int8ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Int8)
        {
        }

        public JsTypedArray Construct(sbyte[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Uint8ArrayConstructor : TypedArrayConstructor
    {
        internal Uint8ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Uint8)
        {
        }

        public JsTypedArray Construct(byte[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Uint8ClampedArrayConstructor : TypedArrayConstructor
    {
        internal Uint8ClampedArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Uint8C)
        {
        }

        public JsTypedArray Construct(byte[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Int16ArrayConstructor : TypedArrayConstructor
    {
        internal Int16ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Int16)
        {
        }

        public JsTypedArray Construct(short[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Uint16ArrayConstructor : TypedArrayConstructor
    {
        internal Uint16ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Uint16)
        {
        }

        public JsTypedArray Construct(ushort[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Int32ArrayConstructor : TypedArrayConstructor
    {
        internal Int32ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Int32)
        {
        }

        public JsTypedArray Construct(int[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Uint32ArrayConstructor : TypedArrayConstructor
    {
        internal Uint32ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Uint32)
        {
        }

        public JsTypedArray Construct(uint[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Float16ArrayConstructor : TypedArrayConstructor
    {
        internal Float16ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Float16)
        {
        }

#if SUPPORTS_HALF
        public JsTypedArray Construct(Half[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            for (var i = 0; i < values.Length; ++i)
            {
                array.DoIntegerIndexedElementSet(i, values[i]);
            }
            return array;
        }
#endif
    }

    public sealed class Float32ArrayConstructor : TypedArrayConstructor
    {
        internal Float32ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Float32)
        {
        }

        public JsTypedArray Construct(float[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class Float64ArrayConstructor : TypedArrayConstructor
    {
        internal Float64ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.Float64)
        {
        }

        public JsTypedArray Construct(double[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class BigInt64ArrayConstructor : TypedArrayConstructor
    {
        internal BigInt64ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.BigInt64)
        {
        }

        public JsTypedArray Construct(long[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }

    public sealed class BigUint64ArrayConstructor : TypedArrayConstructor
    {
        internal BigUint64ArrayConstructor(
            Engine engine,
            Realm realm,
            IntrinsicTypedArrayConstructor functionPrototype,
            IntrinsicTypedArrayPrototype objectPrototype) : base(engine, realm, functionPrototype, objectPrototype, TypedArrayElementType.BigUint64)
        {
        }

        public JsTypedArray Construct(ulong[] values)
        {
            var array = (JsTypedArray) base.Construct([values.Length], this);
            FillTypedArrayInstance(array, values);
            return array;
        }
    }
}
