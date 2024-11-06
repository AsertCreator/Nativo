using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Nativo
{
	public static class Program
	{
		public static StringBuilder SourceOutput = new();
		public static StringBuilder HeaderOutput = new();
		public static List<MethodDefinition> StaticInitializers = [];
		public static MethodDefinition Entrypoint;

		public static int Main(string[] args)
		{
			if (args.Length != 1) 
			{
				Console.WriteLine("nativo: expected assembly file path");
				return 1;
			}
			if (!File.Exists(args[0]))
			{
				Console.WriteLine("nativo: no such file");
				return 1;
			}

			var assm = ModuleDefinition.ReadModule(args[0]);
			var types = assm.Types
				.Where(type => type.Name != "Void" && !type.Name.Contains("Attribute") && type.Name != "ValueType")
				.Skip(1)
				.ToArray();

			Console.WriteLine($"[+] processing roughly {types.Length} types...");
			Stopwatch spw = new();
			spw.Start();

			EmitSetup();

			for (int i = 0; i < types.Length; i++)
			{
				var type = types[i];

				if (type.BaseType == null || type.BaseType.Name != "ValueType")
					HeaderOutput.AppendLine("class " + Utilities.GetWorkName(type) + ";");
				else
					HeaderOutput.AppendLine("struct " + Utilities.GetWorkName(type) + ";");
			}

			for (int i = 0; i < types.Length; i++)
			{
				var type = types[i];
				if (type.Name == "Object")
				{
					EmitTypeDeclaration(type);
					break; // first we MUST define object
				}
			}
			for (int i = 0; i < types.Length; i++)
			{
				var type = types[i];
				if (type.Name == "ValueType")
				{
					EmitTypeDeclaration(type);
					break; // and that
				}
			}
			for (int i = 0; i < types.Length; i++)
			{
				var type = types[i];
				if (type.Name == "Enum")
				{
					EmitTypeDeclaration(type);
					break; // and that...
				}
			}
			for (int i = 0; i < types.Length; i++)
			{
				var type = types[i];
				if (type.Name != "Object" && type.Name != "ValueType" && type.Name != "Enum")
					EmitTypeDeclaration(type);
			}

			var tasks = types.Where(x => x.HasMethods).Select(EmitTypeDefinition).ToArray();

			Task.WaitAll(tasks);

			for (int i = 0; i < tasks.Length; i++)
				SourceOutput.Append(tasks[i].Result);

			EmitBootstrap();

			spw.Stop();

			File.WriteAllText(Path.ChangeExtension(args[0], "cpp"), SourceOutput.ToString());
			File.WriteAllText(Path.ChangeExtension(args[0], "hpp"), HeaderOutput.ToString());

			Console.WriteLine("[+] compilation took " + spw.Elapsed.TotalMilliseconds + " ms!");
			Console.WriteLine("[+] written " + SourceOutput.Length + " source characters!");
			Console.WriteLine("[+] written " + HeaderOutput.Length + " header characters!");
			return 0;
		}
		public static void EmitSetup()
		{
			HeaderOutput.AppendLine("#include <stdint.h>");
			HeaderOutput.AppendLine("#include <stdio.h>");
			HeaderOutput.AppendLine("#include <malloc.h>");
			HeaderOutput.AppendLine();
			HeaderOutput.AppendLine("void* createObject(int type);\n");

			SourceOutput.AppendLine("#include \"" + Path.GetFileName(Path.ChangeExtension(Environment.GetCommandLineArgs()[1], "hpp")) + "\"\n");
		}
		public static void EmitTypeDeclaration(TypeDefinition type)
		{
			var flds = type.Fields;
			var meths = type.Methods;

			if (type.BaseType == null || type.BaseType.Name != "ValueType")
				HeaderOutput.AppendLine("class " + Utilities.GetWorkName(type) +
					(type.BaseType != null ? (" : public " + Utilities.GetWorkName(type.BaseType.Resolve())) : "") +
					" {\npublic:");
			else
				HeaderOutput.AppendLine("struct " + Utilities.GetWorkName(type) + " {");

			for (int i = 0; i < flds.Count; i++)
			{
				var field = flds[i];
				if (field.Name.StartsWith('<'))
					field.Name = field.Name.Substring(1, field.Name.IndexOf('>') - 1);
				HeaderOutput.AppendLine(
					$"    {(field.IsStatic ? "static " : "")}{Utilities.GetMemberTypeName(field.FieldType)} {field.Name};");
			}

			for (int i = 0; i < meths.Count; i++)
			{
				var method = meths[i];
				if (method.CustomAttributes.ToList().Find(x => x.AttributeType.Name == "EntrypointAttribute") != null)
					Entrypoint = method;
				var ri = method.CustomAttributes.ToList().Find(x => x.AttributeType.Name == "RuntimeImportAttribute");
				if (ri != null)
				{
					int ritype = (int)ri.ConstructorArguments[0].Value;
					int riodnl = (int)ri.ConstructorArguments[1].Value;

					if (ritype == 1)
					{
						switch (riodnl)
						{
							case 0:
								HeaderOutput.AppendLine("    static void " + method.Name + "(System_String* str);");
								break;
							case 1:
								HeaderOutput.AppendLine("    static void " + method.Name + "(System_String* str);");
								break;
							case 2:
								HeaderOutput.AppendLine("    static void* " + method.Name + "(int size);");
								break;
							case 3:
								HeaderOutput.AppendLine("    static void* " + method.Name + "(void* addr, int size);");
								break;
							case 4:
								HeaderOutput.AppendLine("    static void " + method.Name + "(void* addr);");
								break;
							case 5:
								HeaderOutput.AppendLine("    static void* " + method.Name + "(System_Object* obj);");
								break;
							case 100:
								HeaderOutput.AppendLine("    static void " + method.Name + "(System_Object* obj);");
								break;
							case 200:
								HeaderOutput.AppendLine("    static System_String* " + method.Name + "(System_String* a, System_String* b);");
								break;
							case 201:
								HeaderOutput.AppendLine("    static System_String* " + method.Name + "(System_String* a, System_String* b, System_String* c);");
								break;
						}
					}
				}
				else if (method.Name == ".ctor")
				{
					HeaderOutput.AppendLine("    " + Utilities.GetWorkName(type) + "(" + GetArguments(method) + ");");
				}
				else if (method.Name == ".cctor")
				{
					StaticInitializers.Add(method);
					method.Name = "__static_construct";
					HeaderOutput.AppendLine("    static void __static_construct();");
				}
				else
				{
					HeaderOutput.AppendLine(
						$"    {(method.IsStatic ? "static " : "")}{Utilities.GetMemberTypeName(method.ReturnType)} " +
						$"{method.Name}(" + GetArguments(method) + ");");
				}
			}

			HeaderOutput.AppendLine("};");
		}
		public static Task<StringBuilder> EmitTypeDefinition(TypeDefinition type)
		{
			var task = new Task<StringBuilder>(() =>
			{
				var flds = type.Fields;
				var meths = type.Methods;
				var source = new StringBuilder();

				for (int i = 0; i < flds.Count; i++)
				{
					var field = flds[i];
					if (field.IsStatic)
						source.AppendLine(Utilities.GetMemberTypeName(field.FieldType) + " " +
							Utilities.GetStaticReference(field.DeclaringType, field.Name) + ';');
				}

				for (int i = 0; i < meths.Count; i++)
				{
					var method = meths[i];
					if (method.CustomAttributes.ToList().Find(x => x.AttributeType.Name == "EntrypointAttribute") != null)
						Entrypoint = method;
					var ri = method.CustomAttributes.ToList().Find(x => x.AttributeType.Name == "RuntimeImportAttribute");
					if (ri != null)
					{
						int ritype = (int)ri.ConstructorArguments[0].Value;
						int riodnl = (int)ri.ConstructorArguments[1].Value;

						if (ritype == 1)
						{
							switch (riodnl)
							{
								case 0:
									source.AppendLine("void " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(System_String* str) {");
									source.AppendLine("    printf(str->data);");
									source.AppendLine("    printf(\"\\n\");");
									source.AppendLine("}");
									break;
								case 1:
									source.AppendLine("void " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(System_String* str) {");
									source.AppendLine("    printf(str->data);");
									source.AppendLine("}");
									break;
								case 2:
									source.AppendLine("void* " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(int size) {");
									source.AppendLine("    return malloc(size);");
									source.AppendLine("}");
									break;
								case 3:
									source.AppendLine("void* " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(void* addr, int size) {");
									source.AppendLine("    return realloc(addr, size);");
									source.AppendLine("}");
									break;
								case 4:
									source.AppendLine("void " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(void* addr) {");
									source.AppendLine("    free(addr);");
									source.AppendLine("}");
									break;
								case 5:
									source.AppendLine("void* " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(System_Object* obj) {");
									source.AppendLine("    return (void*)obj;");
									source.AppendLine("}");
									break;
								case 100:
									source.AppendLine("void " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(System_Object* obj) {");
									source.AppendLine("    obj->m_ObjectTable = new System_ObjectTable();");
									source.AppendLine("    obj->m_ObjectTable->AssociatedObject = 0;");
									source.AppendLine("    obj->m_ObjectTable->GCReferences = 0;");
									source.AppendLine("    obj->m_ObjectTable->MethodTable = 0;");
									source.AppendLine("}");
									break;
								case 200:
									source.AppendLine("System_String* " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(System_String* a, System_String* b) {");
									source.AppendLine("    return new System_String((char*)(void*)\"\");");
									source.AppendLine("}");
									break;
								case 201:
									source.AppendLine("System_String* " + Utilities.GetWorkName(method.DeclaringType) + "::" + method.Name + "(System_String* a, System_String* b, System_String* c) {");
									source.AppendLine("    return new System_String((char*)(void*)\"\");");
									source.AppendLine("}");
									break;
							}
						}
					}
					else if (method.Name == ".ctor")
					{
						source.AppendLine(Utilities.GetWorkName(type) + "::" + Utilities.GetWorkName(type) + "(" + GetArguments(method) + ") {");
						if (type.Name != "Enum") // fix enum shenanigans
							new Emitter(method).EmitBody(source);
						source.AppendLine("}");
					}
					else if (method.Name == ".cctor")
					{
						StaticInitializers.Add(method);
						method.Name = "__static_construct";
						source.AppendLine("void " + Utilities.GetWorkName(type) + "::" + "__static_construct() {");
						new Emitter(method).EmitBody(source);
						source.AppendLine("}");
					}
					else
					{
						source.AppendLine(Utilities.GetMemberTypeName(method.ReturnType) + " " + Utilities.GetWorkName(method.DeclaringType) +
							"::" + method.Name + "(" + GetArguments(method) + ") {");
						new Emitter(method).EmitBody(source);
						source.AppendLine("}");
					}
				}

				return source;
			});
			task.Start();
			return task;
		}
		public static string GetArguments(MethodDefinition method)
		{
			return string.Join(", ", method.Parameters.Select(x => $"{Utilities.GetMemberTypeName(x.ParameterType)} {x.Name}"));
		}
		public static void EmitBootstrap()
		{
			SourceOutput.AppendLine("int main() {");
			for (int i = 0; i < StaticInitializers.Count; i++)
			{
				var si = StaticInitializers[i];
				SourceOutput.AppendLine("    " + Utilities.GetStaticReference(si.DeclaringType, si.Name) + "();");
			}
			SourceOutput.AppendLine("    return " + Utilities.GetStaticReference(Entrypoint.DeclaringType, Entrypoint.Name) + "();");
			SourceOutput.AppendLine("}");
		}
	}
}
