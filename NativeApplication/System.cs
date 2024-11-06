namespace System
{
	public unsafe class Object
	{
#pragma warning disable 169
		// The layout of object is a contract with the compiler.
		private ObjectTable* m_ObjectTable;
#pragma warning restore 169

		public Object() { SetupObjectTable(this); }

		[RuntimeImport(RuntimeImportType.InternalRuntime, 100)]
		private extern static void SetupObjectTable(object obj);

		public bool Equals(object obj)
		{
			return this == obj;
		}
		public string ToString() => GetType().FullName;
		public static bool Equals(object a, object b)
		{
			return a == b;
		}
		public static bool ReferenceEquals(object a, object b)
		{
			return GetObjectAddress(a) == GetObjectAddress(b);
		}
		public Type GetType()
		{
			if (m_ObjectTable->MethodTable->CachedType == null)
			{
				Type type = new(m_ObjectTable->MethodTable);
				m_ObjectTable->MethodTable->CachedType = type;
				return type;
			}
			return m_ObjectTable->MethodTable->CachedType;
		}
		public static object Box(MethodTable* mt, void* data, int len)
		{
			int totallen = sizeof(nint) + len;
			byte* ret = (byte*)Unsafe.Malloc(totallen);
			ObjectTable* ot = (ObjectTable*)Unsafe.Malloc(sizeof(ObjectTable));
			ot->AssociatedObject = null;
			ot->GCReferences = 0;
			ot->MethodTable = mt;

			for (int i = 0;	i < len; i++)
				ret[i + sizeof(nint)] = ((byte*)data)[i];

			ObjectTable** otp = (ObjectTable**)ret;
			*otp = ot;

			return AsObject(ret);
		}
		public static void Unbox(object obj, void* data, int len)
		{
			byte* bt = (byte*)GetObjectAddress(obj);
			for (int i = 0; i < len; i++)
				((byte*)data)[i] = bt[i + sizeof(nint)];
		}
		[RuntimeImport(RuntimeImportType.InternalRuntime, 5)]
		public static extern void* GetObjectAddress(object obj);
		public static unsafe object AsObject(void* ptr) => *(object*)(&ptr);
	}
	public unsafe class Type
	{
		public string Name => typeTable->TypeName;
		public string Namespace => typeTable->TypeNamespace;
		public string FullName => Namespace + "." + Name;
		private MethodTable* typeTable;

		internal Type(MethodTable* mt) => typeTable = mt;
	}
	public unsafe struct ObjectTable
	{
		public MethodTable* MethodTable;
		public object AssociatedObject;
		public int GCReferences;
	}
	public unsafe struct MethodTable
	{
		public string TypeName;
		public string TypeNamespace;
		public IntPtr* MethodList;
		public int MethodCount;
		public Type CachedType;
	}

	public abstract class Delegate { }
	public abstract class MulticastDelegate : Delegate { }

	public struct RuntimeTypeHandle { }
	public struct RuntimeMethodHandle { }
	public struct RuntimeFieldHandle { }

	public class Attribute { }

	public enum AttributeTargets { }
	public enum RuntimeImportType 
	{ 
		PInvoke, InternalRuntime
	}

	public static class Console
	{
		[RuntimeImport(RuntimeImportType.InternalRuntime, 0)]
		public extern static void WriteLine(string text);
		[RuntimeImport(RuntimeImportType.InternalRuntime, 1)]
		public extern static void Write(string text);
	}
	public static unsafe class Unsafe
	{
		[RuntimeImport(RuntimeImportType.InternalRuntime, 2)]
		public extern static void* Malloc(int size);
		[RuntimeImport(RuntimeImportType.InternalRuntime, 3)]
		public extern static void* Realloc(void* addr, int size);
		[RuntimeImport(RuntimeImportType.InternalRuntime, 4)]
		public extern static void Free(void* addr);
	}
	public sealed class EntrypointAttribute : Attribute { }
	public sealed class RuntimeImportAttribute : Attribute
	{
		public RuntimeImportAttribute(RuntimeImportType validOn, int ordinal) { }
	}
	public sealed class AttributeUsageAttribute : Attribute
	{
		public AttributeUsageAttribute(AttributeTargets validOn) { }
		public bool AllowMultiple { get; set; }
		public bool Inherited { get; set; }
	}
}