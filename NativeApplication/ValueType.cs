namespace System
{
	public struct Void { }
	public struct Boolean { private bool val; }
	public struct Char { private char val; }
	public struct SByte { private sbyte val; }
	public struct Byte { private byte val; }
	public struct Int16 { private short val; }
	public struct UInt16 { private ushort val; }
	public struct Int32 { private int val; }
	public struct UInt32 { private uint val; }
	public struct Int64 { private long val; }
	public struct UInt64 { private ulong val; }
	public unsafe struct IntPtr { public void* val; }
	public struct Single { private float val; }
	public struct Double { private double val; }

	public abstract class ValueType { }
	public abstract class Enum : ValueType { }

	public struct Nullable<T> where T : struct { }
}
