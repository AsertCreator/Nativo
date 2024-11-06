namespace System
{
	public unsafe class Array
	{
		private int length;
		private void* data;
		private int elsize;

		public int Length => length;

		public Array(int length, void* data, int elsize)
		{
			this.length = length;
			this.data = data;
			this.elsize = elsize;
		}
		public static Array NewArray(int length, int elsize)
		{
			return new Array(length, Unsafe.Malloc(length * elsize), elsize);
		}
		public object GetValue(int i) => *(object*)((byte*)data + elsize * i);
		public void SetValue(int i, object val) => *(object*)((byte*)data + elsize * i) = val;
	}
}
