using Mono.Cecil;
using System;

namespace Nativo
{
	public static class Utilities
	{
		public static string GetThisReference(string name) => "this->" + name;
		public static string GetStaticReference(TypeDefinition type, string name) => GetWorkName(type) + "::" + name;
		public static string GetWorkName(TypeDefinition type) => type.FullName.Replace('.', '_').Replace('`', '_') + (type.IsPointer ? "*" : "");
		public static string GetMemberTypeName(TypeReference type)
		{
			var ogt = type;
			if (type.IsPointer) type = type.GetElementType().Resolve();
			switch (type.Name)
			{
				case "Char":
					return "char" + (ogt.IsPointer ? "*" : "");
				case "SByte":
					return "int8_t" + (ogt.IsPointer ? "*" : "");
				case "Int16":
					return "short" + (ogt.IsPointer ? "*" : "");
				case "Int32":
					return "int" + (ogt.IsPointer ? "*" : "");
				case "Int64":
					return "int64_t" + (ogt.IsPointer ? "*" : "");
				case "IntPtr":
					return "intptr_t" + (ogt.IsPointer ? "*" : "");
				case "UIntPtr":
					return "uintptr_t" + (ogt.IsPointer ? "*" : "");
				case "Byte":
					return "uint8_t" + (ogt.IsPointer ? "*" : "");
				case "UInt16":
					return "uint16_t" + (ogt.IsPointer ? "*" : "");
				case "UInt32":
					return "uint32_t" + (ogt.IsPointer ? "*" : "");
				case "UInt64":
					return "uint64_t" + (ogt.IsPointer ? "*" : "");
				case "Boolean":
					return "bool" + (ogt.IsPointer ? "*" : "");
				case "Single":
					return "float" + (ogt.IsPointer ? "*" : "");
				case "Double":
					return "double" + (ogt.IsPointer ? "*" : "");
				case "Void":
					return ogt.IsPointer ? "uint8_t*" : "void"; // yes
				default:
					return GetWorkName(ogt.Resolve()) + '*';
			}
		}
	}
}
