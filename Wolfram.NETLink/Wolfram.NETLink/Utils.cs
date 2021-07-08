using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Wolfram.NETLink
{
	public abstract class Utils
	{
		private static bool isWin;

		private static bool isMac;

		private static bool isMono;

		private static bool is64Bit;

		private static char[] junkChars;

		public static bool IsWindows => isWin;

		public static bool Is64Bit => is64Bit;

		public static bool IsMac => isMac;

		public static bool IsMono => isMono;

		static Utils()
		{
			junkChars = new char[2]
			{
				' ',
				'\0'
			};
			string text = Environment.OSVersion.ToString();
			isWin = text.IndexOf("Windows") != -1;
			is64Bit = IntPtr.Size == 8;
			if (isWin)
			{
				isMac = false;
			}
			else
			{
				ProcessStartInfo startInfo = new ProcessStartInfo("uname", "-s")
				{
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				string strA = string.Empty;
				using (Process process = Process.Start(startInfo))
				{
					process.Start();
					strA = process.StandardOutput.ReadLine();
					process.WaitForExit();
				}
				isMac = string.Compare(strA, "darwin", ignoreCase: true) == 0;
			}
			isMono = false;
			try
			{
				Type.GetType("Mono.Math.BigInteger", throwOnError: true);
				isMono = true;
			}
			catch (Exception)
			{
			}
		}

		public static string ConvertCRLF(string s)
		{
			if (s == null)
			{
				return s;
			}
			int num = s.IndexOf('\n');
			if (num != -1)
			{
				return s.Replace("\n", "\r\n");
			}
			return s;
		}

		public static decimal DecimalFromString(string s)
		{
			if (s[0] == '.')
			{
				s = "0" + s;
			}
			int num = s.IndexOfAny(junkChars);
			if (num != -1)
			{
				s = s.Substring(0, num);
			}
			int num2 = s.IndexOf('e');
			int num3 = s.IndexOf('`');
			int num4 = ((num2 != -1) ? (-1) : s.IndexOf('*'));
			if (num2 != -1)
			{
				if (num3 != -1)
				{
					s = s.Substring(0, num3);
				}
				if (char.IsDigit(s[num2 + 1]))
				{
					s = s.Insert(num2 + 1, "+");
				}
			}
			else if (num4 != -1)
			{
				int length = ((num3 != -1) ? num3 : num4);
				s = s.Substring(0, length) + "e" + (char.IsDigit(s[num4 + 2]) ? "+" : "") + s.Substring(num4 + 2);
			}
			else if (num3 != -1)
			{
				s = s.Substring(0, num3);
			}
			return decimal.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
		}

		public static void WriteEvalToStringExpression(IMathLink ml, object obj, int pageWidth, string format)
		{
			ml.PutFunction("EvaluatePacket", 1);
			ml.PutFunction("ToString", 3);
			if (obj is string)
			{
				ml.PutFunction("ToExpression", 1);
			}
			ml.Put(obj);
			ml.PutFunction("Rule", 2);
			ml.PutSymbol("FormatType");
			ml.PutSymbol(format);
			ml.PutFunction("Rule", 2);
			ml.PutSymbol("PageWidth");
			if (pageWidth > 0)
			{
				ml.Put(pageWidth);
			}
			else
			{
				ml.PutSymbol("Infinity");
			}
			ml.EndPacket();
		}

		public static void WriteEvalToTypesetExpression(IMathLink ml, object obj, int pageWidth, string graphicsFmt, bool useStdForm)
		{
			ml.PutFunction("EvaluatePacket", 1);
			int argCount = 2 + ((!useStdForm) ? 1 : 0) + ((pageWidth > 0) ? 1 : 0);
			ml.PutFunction("EvaluateToTypeset", argCount);
			ml.Put(obj);
			if (!useStdForm)
			{
				ml.PutSymbol("TraditionalForm");
			}
			if (pageWidth > 0)
			{
				ml.Put(pageWidth);
			}
			ml.Put(graphicsFmt);
			ml.EndPacket();
		}

		public static void WriteEvalToImageExpression(IMathLink ml, object obj, int width, int height, string graphicsFmt, int dpi, bool useFE)
		{
			ml.PutFunction("EvaluatePacket", 1);
			int argCount = 2 + (useFE ? 1 : 0) + ((dpi > 0) ? 1 : 0) + ((width > 0 || height > 0) ? 1 : 0);
			ml.PutFunction("EvaluateToImage", argCount);
			ml.Put(obj);
			if (useFE)
			{
				ml.Put(b: true);
			}
			ml.Put(graphicsFmt);
			if (dpi > 0)
			{
				ml.PutFunction("Rule", 2);
				ml.PutSymbol("ImageResolution");
				ml.Put(dpi);
			}
			if (width > 0 || height > 0)
			{
				ml.PutFunction("Rule", 2);
				ml.PutSymbol("ImageSize");
				ml.PutFunction("List", 2);
				if (width > 0)
				{
					ml.Put(width);
				}
				else
				{
					ml.PutSymbol("Automatic");
				}
				if (height > 0)
				{
					ml.Put(height);
				}
				else
				{
					ml.PutSymbol("Automatic");
				}
			}
			ml.EndPacket();
		}

		internal static bool memberNamesMatch(string actualMemberName, string nameFromMma)
		{
			if (nameFromMma.Length != actualMemberName.Length)
			{
				return false;
			}
			for (int i = 0; i < actualMemberName.Length; i++)
			{
				char c = nameFromMma[i];
				char c2 = actualMemberName[i];
				if (c != c2 && (c != 'U' || c2 != '_'))
				{
					return false;
				}
			}
			return true;
		}

		internal static bool nextIndex(int[] indices, int[] lengths)
		{
			int num = lengths.Length;
			while (num-- > 0)
			{
				if (indices[num] < lengths[num] - 1)
				{
					indices[num]++;
					break;
				}
				if (num == 0)
				{
					return false;
				}
				indices[num] = 0;
			}
			return true;
		}

		internal static object readArgAs(IKernelLink ml, int argType, Type t)
		{
			if (t.IsByRef)
			{
				t = t.GetElementType();
			}
			if (t == typeof(Expr))
			{
				return ml.GetExpr();
			}
			switch (argType)
			{
			case 5:
			case 7:
			{
				object @object = ml.GetObject();
				if (object.ReferenceEquals(@object, Missing.Value))
				{
					return @object;
				}
				if ((IsTrulyPrimitive(t) || t == typeof(decimal)) && @object.GetType() != t)
				{
					return Convert.ChangeType(@object, t);
				}
				return @object;
			}
			case 6:
				ml.GetSymbol();
				return Missing.Value;
			case 1:
				if (t.IsEnum)
				{
					Type underlyingType = Enum.GetUnderlyingType(t);
					object obj = readArgAs(ml, argType, underlyingType);
					if (!Enum.IsDefined(t, obj) && t.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Length == 0)
					{
						throw new MathLinkException(1011, string.Concat("Enum value ", obj, " out of range for type ", t.FullName, "."));
					}
					return Enum.ToObject(t, obj);
				}
				break;
			}
			if ((argType == 1 || argType == 2 || argType == 12) && t == ml.ComplexType)
			{
				return ml.GetComplex();
			}
			switch (Type.GetTypeCode(t))
			{
			case TypeCode.Object:
				switch (argType)
				{
				case 1:
					return ml.GetInteger();
				case 2:
					return ml.GetDouble();
				case 3:
					return ml.GetString();
				case 5:
					ml.GetSymbol();
					return null;
				case 4:
					return ml.GetBoolean();
				case 12:
					return ml.GetComplex();
				case 13:
					throw new ArgumentException();
				default:
					if (t == typeof(Array) || t == typeof(object))
					{
						return readArbitraryArray(ml, typeof(Array));
					}
					if (t.IsArray)
					{
						Type elementType = t.GetElementType();
						if (elementType.IsArray)
						{
							return readArbitraryArray(ml, t);
						}
						return ml.GetArray(elementType, t.GetArrayRank());
					}
					if (t.IsPointer && argType == 8)
					{
						Type elementType2 = t.GetElementType();
						return readArgAs(ml, 8, TypeLoader.GetType(elementType2.FullName + "[]", throwOnError: true));
					}
					throw new ArgumentException();
				}
			case TypeCode.Byte:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (byte)ml.GetInteger();
			case TypeCode.SByte:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (sbyte)ml.GetInteger();
			case TypeCode.Char:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (char)ml.GetInteger();
			case TypeCode.Int16:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (short)ml.GetInteger();
			case TypeCode.UInt16:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (ushort)ml.GetInteger();
			case TypeCode.Int32:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return ml.GetInteger();
			case TypeCode.UInt32:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (uint)ml.GetDecimal();
			case TypeCode.Int64:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (long)ml.GetDecimal();
			case TypeCode.UInt64:
				if (argType != 1)
				{
					throw new ArgumentException();
				}
				return (ulong)ml.GetDecimal();
			case TypeCode.Single:
				if (argType != 2 && argType != 1)
				{
					throw new ArgumentException();
				}
				return (float)ml.GetDouble();
			case TypeCode.Double:
				if (argType != 2 && argType != 1)
				{
					throw new ArgumentException();
				}
				return ml.GetDouble();
			case TypeCode.Decimal:
				if (argType != 2 && argType != 1)
				{
					throw new ArgumentException();
				}
				return ml.GetDecimal();
			case TypeCode.Boolean:
				if (argType != 4)
				{
					throw new ArgumentException();
				}
				return ml.GetBoolean();
			case TypeCode.String:
				if (argType != 3)
				{
					throw new ArgumentException();
				}
				return ml.GetString();
			case TypeCode.DateTime:
				return (DateTime)ml.GetObject();
			case TypeCode.DBNull:
				ml.GetSymbol();
				return DBNull.Value;
			default:
				throw new ArgumentException();
			}
		}

		internal static Array readArbitraryArray(IMathLink ml, Type t)
		{
			if (t == typeof(Array))
			{
				int num = determineIncomingArrayDepth(ml);
				ILinkMark mark = ml.CreateMark();
				ILoopbackLink loopbackLink = null;
				try
				{
					switch (num)
					{
					case 1:
					{
						if (ml.CheckFunction("List") == 0)
						{
							throw new MathLinkException(1006);
						}
						ExpressionType nextExpressionType = ml.GetNextExpressionType();
						ml.SeekMark(mark);
						Type leafType = netTypeFromExpressionType(nextExpressionType, ml);
						if (leafType == typeof(Expr))
						{
							if (nextExpressionType == ExpressionType.Complex)
							{
								throw new MathLinkException(1010);
							}
							throw new MathLinkException(1005);
						}
						return ml.GetArray(leafType, 1);
					}
					case 2:
					{
						loopbackLink = MathLinkFactory.CreateLoopbackLink();
						int num2 = ml.CheckFunction("List");
						bool flag = false;
						bool flag2 = false;
						ExpressionType nextExpressionType = ExpressionType.Integer;
						int num3 = ml.CheckFunction("List");
						if (num3 > 0)
						{
							nextExpressionType = ml.GetNextExpressionType();
							flag2 = true;
						}
						for (int j = 0; j < num3; j++)
						{
							loopbackLink.TransferExpression(ml);
						}
						for (int k = 1; k < num2; k++)
						{
							switch (ml.GetNextExpressionType())
							{
							case ExpressionType.Object:
								flag = true;
								if (ml.GetObject() == null)
								{
									continue;
								}
								throw new MathLinkException(1003);
							case ExpressionType.Function:
							{
								int num4 = ml.CheckFunction("List");
								if (!flag2 && num4 > 0)
								{
									nextExpressionType = ml.GetNextExpressionType();
									flag2 = true;
								}
								if (num4 == num3)
								{
									for (int l = 0; l < num4; l++)
									{
										loopbackLink.TransferExpression(ml);
									}
									continue;
								}
								break;
							}
							default:
								throw new MathLinkException(1002);
							}
							flag = true;
							break;
						}
						if (!flag2)
						{
							throw new MathLinkException(1006);
						}
						Type leafType = netTypeFromExpressionType(nextExpressionType, ml);
						ml.SeekMark(mark);
						if (flag)
						{
							ml.CheckFunction("List");
							Array array = Array.CreateInstance(Array.CreateInstance(leafType, 0).GetType(), num2);
							for (int m = 0; m < num2; m++)
							{
								ExpressionType nextExpressionType2 = ml.GetNextExpressionType();
								if (nextExpressionType2 == ExpressionType.Function)
								{
									array.SetValue(ml.GetArray(leafType, 1), m);
									continue;
								}
								string symbol = ml.GetSymbol();
								if (symbol != "Null")
								{
									throw new MathLinkException(1003);
								}
								array.SetValue(null, m);
							}
							return array;
						}
						return ml.GetArray(leafType, 2);
					}
					default:
					{
						for (int i = 0; i < num; i++)
						{
							ml.CheckFunction("List");
						}
						ExpressionType nextExpressionType = ml.GetNextExpressionType();
						Type leafType = netTypeFromExpressionType(nextExpressionType, ml);
						ml.SeekMark(mark);
						return ml.GetArray(leafType, num);
					}
					}
				}
				finally
				{
					loopbackLink?.Close();
					ml.DestroyMark(mark);
				}
			}
			int arrayRank = t.GetArrayRank();
			Type elementType = t.GetElementType();
			if (elementType.IsArray)
			{
				if (arrayRank > 1)
				{
					throw new MathLinkException(1007);
				}
				int num2 = ml.CheckFunction("List");
				Array array2 = Array.CreateInstance(elementType, num2);
				for (int n = 0; n < num2; n++)
				{
					ExpressionType nextExpressionType3 = ml.GetNextExpressionType();
					if (nextExpressionType3 == ExpressionType.Function)
					{
						array2.SetValue(readArbitraryArray(ml, elementType), n);
						continue;
					}
					string symbol2 = ml.GetSymbol();
					if (symbol2 != "Null")
					{
						throw new MathLinkException(1003);
					}
					array2.SetValue(null, n);
				}
				return array2;
			}
			if (elementType == typeof(Array))
			{
				throw new MathLinkException(1008);
			}
			return ml.GetArray(elementType, arrayRank);
		}

		internal static void discardNext(IKernelLink ml)
		{
			switch (ml.GetNextExpressionType())
			{
			case ExpressionType.Integer:
				ml.GetInteger();
				break;
			case ExpressionType.Real:
				ml.GetDouble();
				break;
			case ExpressionType.Symbol:
			case ExpressionType.Boolean:
				ml.GetSymbol();
				break;
			case ExpressionType.String:
				ml.GetString();
				break;
			case ExpressionType.Object:
				ml.GetObject();
				break;
			case ExpressionType.Complex:
			case ExpressionType.Function:
			{
				IMathLink mathLink = MathLinkFactory.CreateLoopbackLink();
				try
				{
					mathLink.TransferExpression(ml);
				}
				finally
				{
					mathLink.Close();
				}
				break;
			}
			}
		}

		internal static int determineIncomingArrayDepth(IMathLink ml)
		{
			int num = 0;
			ILinkMark mark = ml.CreateMark();
			try
			{
				ml.GetFunction(out var argCount);
				num = 1;
				while (true)
				{
					if (argCount > 0)
					{
						ExpressionType nextExpressionType = ml.GetNextExpressionType();
						if (nextExpressionType == ExpressionType.Function)
						{
							ml.GetFunction(out argCount);
							num++;
							continue;
						}
						break;
					}
					return num;
				}
				return num;
			}
			catch (MathLinkException)
			{
				ml.ClearError();
				return num;
			}
			finally
			{
				ml.SeekMark(mark);
				ml.DestroyMark(mark);
			}
		}

		internal static bool IsTrulyPrimitive(Type t)
		{
			if (t.IsPrimitive)
			{
				return t != typeof(IntPtr);
			}
			return false;
		}

		internal static string addSystemNamespace(string typeName)
		{
			if (typeName.IndexOf('.') != -1)
			{
				return typeName;
			}
			return "System." + typeName;
		}

		internal static bool IsOutOnlyParam(ParameterInfo pi)
		{
			if (pi.IsOut)
			{
				return !pi.IsIn;
			}
			return false;
		}

		private static Type netTypeFromExpressionType(ExpressionType exprType, IMathLink ml)
		{
			switch (exprType)
			{
			case ExpressionType.Integer:
				return typeof(int);
			case ExpressionType.Real:
				return typeof(double);
			case ExpressionType.String:
				return typeof(string);
			case ExpressionType.Boolean:
				return typeof(bool);
			case ExpressionType.Symbol:
			case ExpressionType.Function:
				return typeof(Expr);
			case ExpressionType.Object:
				return typeof(object);
			case ExpressionType.Complex:
				if (ml.ComplexType == null)
				{
					return typeof(Expr);
				}
				return ml.ComplexType;
			default:
				return null;
			}
		}
	}
}
