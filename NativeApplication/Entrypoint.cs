using System;

namespace NativeApplication
{
	public static unsafe class Entrypoint
	{
		public static int ReturnValue = 6;

		[Entrypoint]
		public static int Main()
		{
			object v = 5;
			Console.WriteLine("hello Sex World!");
			return (int)v;
		}
	}
}