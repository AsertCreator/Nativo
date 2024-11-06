using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nativo
{
	public class Emitter(MethodDefinition method)
	{
		public void EmitBody(StringBuilder ou)
		{
			var insts = method.Body.Instructions;
			var locals = method.Body.Variables;

			for (int i = 0; i < locals.Count; i++)
			{
				var local = locals[i];
				ou.AppendLine("    " + Utilities.GetMemberTypeName(local.VariableType) + " var_" + local.Index + ";");
			}

			Stack stack = [];
			Dictionary<int, string> labels = [];

			string tabbed = new string(' ', 4);

			for (int i = 0; i < insts.Count; i++)
			{
				var inst = insts[i];
				var operand = inst.Operand as Instruction;

				if (operand != null)
				{
					if (operand.Previous != inst)
						labels[insts.IndexOf(operand)] = "IL_" + operand.Offset;
				}
			}
			for (int i = 0; i < insts.Count; i++)
			{
				var inst = insts[i];

				if (labels.TryGetValue(i, out string label))
					ou.AppendLine(tabbed + label + ":");

				switch (inst.OpCode.Code)
				{
					case Code.Ldc_I4_0:
						stack.Push(0);
						break;
					case Code.Ldc_I4_1:
						stack.Push(1);
						break;
					case Code.Ldc_I4_2:
						stack.Push(2);
						break;
					case Code.Ldc_I4_3:
						stack.Push(3);
						break;
					case Code.Ldc_I4_4:
						stack.Push(4);
						break;
					case Code.Ldc_I4_5:
						stack.Push(5);
						break;
					case Code.Ldc_I4_6:
						stack.Push(6);
						break;
					case Code.Ldc_I4_7:
						stack.Push(7);
						break;
					case Code.Ldc_I4_8:
						stack.Push(8);
						break;
					case Code.Ldc_I4:
						stack.Push(inst.Operand);
						break;
					case Code.Ldc_I4_S:
						stack.Push((int)(sbyte)inst.Operand);
						break;
					case Code.Ldsfld:
						{
							var target = inst.Operand as FieldDefinition;
							stack.Push(Utilities.GetStaticReference(target.DeclaringType, target.Name));
							break;
						}
					case Code.Ldfld:
						{
							var obj = stack.Pop();
							var target = inst.Operand as FieldDefinition;
							stack.Push("((" + Utilities.GetMemberTypeName(target.FieldType) + ")" + obj + "->" + target.Name + ")");
							break;
						}
					case Code.Ldloc_0:
						stack.Push("var_0");
						break;
					case Code.Ldloc_1:
						stack.Push("var_1");
						break;
					case Code.Ldloc_2:
						stack.Push("var_2");
						break;
					case Code.Ldloc_3:
						stack.Push("var_3");
						break;
					case Code.Ldloc:
						stack.Push("var_" + (inst.Operand as VariableDefinition).Index);
						break;
					case Code.Ldloc_S:
						stack.Push("var_" + (inst.Operand as VariableDefinition).Index);
						break;
					default:
						if (method.IsStatic)
						{
							if (inst.OpCode.Code == Code.Ldarg_0) stack.Push(method.Parameters[0].Name);
							else if (inst.OpCode.Code == Code.Ldarg_1) stack.Push(method.Parameters[1].Name);
							else if (inst.OpCode.Code == Code.Ldarg_2) stack.Push(method.Parameters[2].Name);
							else if (inst.OpCode.Code == Code.Ldarg_3) stack.Push(method.Parameters[3].Name);
						}
						else
						{
							if (inst.OpCode.Code == Code.Ldarg_0) stack.Push("this");
							else if (inst.OpCode.Code == Code.Ldarg_1) stack.Push(method.Parameters[0].Name);
							else if (inst.OpCode.Code == Code.Ldarg_2) stack.Push(method.Parameters[1].Name);
							else if (inst.OpCode.Code == Code.Ldarg_3) stack.Push(method.Parameters[2].Name);
						}
						break;
				}

				switch (inst.OpCode.Code)
				{
					case Code.Ldind_Ref:
						stack.Push("(*(System_Object**)(" + stack.Pop() + "))");
						break;
					case Code.Stind_Ref:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(System_Object**)(" + obj + ") = " + "(System_Object*)" + val + ";");
							break;
						}
					case Code.Stind_I1:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(int8_t*)(" + obj + ") = " + "(int8_t)" + val + ";");
							break;
						}
					case Code.Stind_I2:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(int16_t*)(" + obj + ") = " + "(int16_t)" + val + ";");
							break;
						}
					case Code.Stind_I4:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(int32_t*)(" + obj + ") = " + "(int32_t)" + val + ";");
							break;
						}
					case Code.Stind_I8:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(int64_t*)(" + obj + ") = " + "(int64_t)" + val + ";");
							break;
						}
					case Code.Stind_R4:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(float*)(" + obj + ") = " + "(float)" + val + ";");
							break;
						}
					case Code.Stind_R8:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(double*)(" + obj + ") = " + "(double)" + val + ";");
							break;
						}
					case Code.Ldind_U1:
						stack.Push("*(uint8_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_U2:
						stack.Push("*(uint16_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_U4:
						stack.Push("*(uint32_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_I1:
						stack.Push("*(int8_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_I2:
						stack.Push("*(int16_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_I4:
						stack.Push("*(int32_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_I8:
						stack.Push("*(int64_t*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_R4:
						stack.Push("*(float*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_R8:
						stack.Push("*(double*)(" + stack.Pop() + ")");
						break;
					case Code.Ldind_I:
						stack.Push("*(void**)(" + stack.Pop() + ")");
						break;
					case Code.Stind_I:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							ou.AppendLine(tabbed + "*(void**)(" + obj + ") = " + "(void*)" + val + ";");
							break;
                        }
                    case Code.Ldarg:
                        stack.Push((inst.Operand as ParameterDefinition).Name);
                        break;
                    case Code.Ldarg_S:
						stack.Push((inst.Operand as ParameterDefinition).Name);
						break;
					case Code.Ldarga_S:
						stack.Push('&' + (inst.Operand as ParameterDefinition).Name);
						break;
                    case Code.Ldarga:
                        stack.Push('&' + (inst.Operand as ParameterDefinition).Name);
                        break;
                    case Code.Add:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " + " + a0 + ')');
							break;
						}
					case Code.Sub:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " - " + a0 + ')');
							break;
						}
					case Code.Mul:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " * " + a0 + ')');
							break;
						}
					case Code.Div:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " / " + a0 + ')');
							break;
						}
					case Code.Ceq:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " == " + a0 + ')');
							break;
						}
					case Code.Cgt:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " > " + a0 + ')');
							break;
						}
					case Code.Clt:
						{
							var a0 = stack.Pop();
							var a1 = stack.Pop();
							stack.Push("(" + a1 + " < " + a0 + ')');
							break;
						}
					case Code.Stfld:
						{
							var val = stack.Pop();
							var obj = stack.Pop();
							var field = inst.Operand as FieldDefinition;
							ou.AppendLine(tabbed + obj + "->" + field.Name + " = (" + Utilities.GetMemberTypeName(field.FieldType) + ')' + val + ";");
							break;
						}
					case Code.Box:
						{
							var val = stack.Pop();
							var type = inst.Operand as TypeDefinition;
							ou.AppendLine(tabbed + "auto box_" + inst.Offset + " = " + val + ";");
							stack.Push("System_Object::Box(0, (uint8_t*)&box_" + inst.Offset + ", sizeof(" + Utilities.GetMemberTypeName(type) + "))");
							break;
						}
					case Code.Unbox_Any:
						{
							var val = stack.Pop();
							var type = inst.Operand as TypeDefinition;
							ou.AppendLine(tabbed + Utilities.GetMemberTypeName(type) + " unbox_" + inst.Offset + ";");
							ou.AppendLine(tabbed + "System_Object::Unbox((System_Object*)" + val + ", (uint8_t*)&unbox_" + inst.Offset + ", sizeof(" + Utilities.GetMemberTypeName(type) + "));");
							stack.Push("unbox_" + inst.Offset);
							break;
						}
					case Code.Ldnull:
						stack.Push(0);
						break;
					case Code.Dup:
						stack.Push(stack.Peek());
						break;
					case Code.Ldstr:
						{
							string str = (string)inst.Operand;
							stack.Push("new System_String((char*)(void*)\"" + str + "\", " + str.Length + ")");
							break;
						}

					case Code.Call:
					case Code.Callvirt:
						{
							var meth = inst.Operand as MethodDefinition;
							if (meth.IsStatic)
							{
								var args = new object[meth.Parameters.Count];
								for (int j = 0; j < meth.Parameters.Count; j++)
								{
									args[meth.Parameters.Count - j - 1] = stack.Pop();
								}
								if (inst.Next.OpCode.Code != Code.Nop) // EXTREMELY. EXTREMELY NAIVE
								{
									stack.Push(Utilities.GetStaticReference(meth.DeclaringType, meth.Name) + "(" + string.Join(", ", args) + ")");
								}
								else
								{
									ou.AppendLine(tabbed + Utilities.GetStaticReference(meth.DeclaringType, meth.Name) + "(" + string.Join(", ", args) + ");");
								}
							}
							else
							{
								var arglist = "";
								for (int j = 0; j < meth.Parameters.Count; j++)
								{
									arglist += "(" + Utilities.GetMemberTypeName(meth.Parameters[j].ParameterType) + ")(" + stack.Pop() + ")";
									if (j != meth.Parameters.Count - 1)
										arglist += ", ";
								}

								var obj = stack.Pop();
								if (inst.Next.OpCode.Code != Code.Nop)
								{
									if (meth.Name != ".ctor") // c++ is smart enough
										stack.Push(obj + "->" + meth.Name + "(" + arglist + ")");
								}
								else
								{
									if (meth.Name != ".ctor") // c++ is smart enough
										ou.AppendLine(tabbed + obj + "->" + meth.Name + "(" + arglist + ");");
								}
							}

							break;
						}

					case Code.Newobj:
						{
							var meth = inst.Operand as MethodDefinition;
							var type = meth.DeclaringType;

							var arglist = "";
							for (int j = 0; j < meth.Parameters.Count; j++)
							{
								arglist += "(" + Utilities.GetMemberTypeName(meth.Parameters[j].ParameterType) + ")(" + stack.Pop() + ")";
								if (j != meth.Parameters.Count - 1)
									arglist += ", ";
							}

							if (inst.Next.OpCode.Code != Code.Nop)
							{
								stack.Push("new " + Utilities.GetWorkName(type) + "(" + arglist + ")");
							}
							else
							{
								ou.AppendLine(tabbed + "new " + Utilities.GetWorkName(type) + "(" + arglist + ");");
							}

							break;
						}

					case Code.Newarr:
						{
							var type = inst.Operand as TypeDefinition;
							var arrsize = stack.Pop();

							stack.Push("System_Array::NewArray(" + arrsize + ", sizeof(" + Utilities.GetMemberTypeName(type) + "))");
							break;
						}

					case Code.Sizeof:
						{
							var type = inst.Operand as TypeDefinition;

							stack.Push("sizeof(" + Utilities.GetWorkName(type) + ")");
							break;
						}

					case Code.Ret:
						if (stack.Count > 0 && !method.IsConstructor)
							ou.AppendLine(tabbed + "return " + stack.Pop() + ';');
						else
							ou.AppendLine(tabbed + "return;");
						return;
					case Code.Brfalse_S:
						{
							var targ = (Instruction)inst.Operand;
							ou.AppendLine(tabbed + "if (!" + stack.Pop() + ") {");
							ou.AppendLine(tabbed + "	goto IL_" + targ.Offset + ';');
							ou.AppendLine(tabbed + "}");
							break;
						}

					case Code.Brtrue_S:
						{
							var targ = (Instruction)inst.Operand;
							ou.AppendLine(tabbed + "if (" + stack.Pop() + ") {");
							ou.AppendLine(tabbed + "	goto IL_" + targ.Offset + ';');
							ou.AppendLine(tabbed + "}");
							break;
						}

					case Code.Br_S:
						{
							var targ = (Instruction)inst.Operand;
							if (insts.IndexOf(targ) == i + 1) continue;
							ou.AppendLine(tabbed + "goto IL_" + targ.Offset + ';');
							break;
						}

					case Code.Stloc_0:
						ou.AppendLine(tabbed + "var_0 = (" +
											Utilities.GetMemberTypeName(locals[0].VariableType) + ')' + stack.Pop() + ';');
						break;
					case Code.Stloc_1:
						ou.AppendLine(tabbed + "var_1 = (" +
												Utilities.GetMemberTypeName(locals[1].VariableType) + ')' + stack.Pop() + ';');
						break;
					case Code.Stloc_2:
						ou.AppendLine(tabbed + "var_2 = (" +
												Utilities.GetMemberTypeName(locals[2].VariableType) + ')' + stack.Pop() + ';');
						break;
					case Code.Stloc_3:
						ou.AppendLine(tabbed + "var_3 = (" +
												Utilities.GetMemberTypeName(locals[3].VariableType) + ')' + stack.Pop() + ';');
						break;
					case Code.Stloc_S:
						ou.AppendLine(tabbed + "var_" + (inst.Operand as VariableDefinition).Index + " = (" +
												Utilities.GetMemberTypeName((inst.Operand as VariableDefinition).VariableType) + ')' + stack.Pop() + ';');
						break;
					case Code.Stloc:
						ou.AppendLine(tabbed + "var_" + (inst.Operand as VariableDefinition).Index + " = (" +
												Utilities.GetMemberTypeName((inst.Operand as VariableDefinition).VariableType) + ')' + stack.Pop() + ';');
						break;
					case Code.Stsfld:
						{
							FieldDefinition field = inst.Operand as FieldDefinition;
							ou.AppendLine(tabbed + Utilities.GetStaticReference(field.DeclaringType, field.Name) + " = " + stack.Pop() + ";");
							break;
						}
				}
			}
		}
	}
}
