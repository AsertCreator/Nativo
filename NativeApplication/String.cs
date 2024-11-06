namespace System
{
	public unsafe sealed class String
	{
		private int length;
		private char* data;

		public int Length => length;

		public String(char* str)
		{
			data = str;
			length = 0;
		}
		public String(char* str, int len)
		{
			data = str;
			length = len;
		}
		public String(char ch, int len)
		{
			data = (char*)Unsafe.Malloc(len + 1);
			length = len;
			for (int i = 0; i < len; i++)
				data[i] = ch;
			data[len] = '\0';
		}

		[RuntimeImport(RuntimeImportType.InternalRuntime, 200)]
		public static extern string Concat(string a, string b);

		[RuntimeImport(RuntimeImportType.InternalRuntime, 201)]
		public static extern string Concat(string a, string b, string c);
	}
}
