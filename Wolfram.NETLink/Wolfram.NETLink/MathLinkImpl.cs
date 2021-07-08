using System;
using System.Globalization;

namespace Wolfram.NETLink
{
	public abstract class MathLinkImpl : IMathLink
	{
		private long timeoutMillis;

		private long startConnectTime;

		protected object yieldFunctionLock = new object();

		protected ComplexClassHandler complexHandler = new ComplexClassHandler();

		protected string[] headsHolder = new string[32];

		public virtual Type ComplexType
		{
			get
			{
				return complexHandler.ComplexType;
			}
			set
			{
				complexHandler.ComplexType = value;
			}
		}

		public abstract int Error
		{
			get;
		}

		public abstract string ErrorMessage
		{
			get;
		}

		public abstract bool Ready
		{
			get;
		}

		public abstract string Name
		{
			get;
		}

		public abstract event YieldFunction Yield;

		public abstract event MessageHandler MessageArrived;

		public string GetStringCRLF()
		{
			return Utils.ConvertCRLF(GetString());
		}

		public void Put(bool b)
		{
			PutSymbol(b ? "True" : "False");
		}

		public bool GetBoolean()
		{
			return GetSymbol() == "True";
		}

		public void Put(long i)
		{
			Put((decimal)i);
		}

		public decimal GetDecimal()
		{
			return Utils.DecimalFromString(GetString());
		}

		public void Put(decimal d)
		{
			if (decimal.Truncate(d) == d)
			{
				PutNext(ExpressionType.Integer);
			}
			else
			{
				PutNext(ExpressionType.Real);
			}
			string text = d.ToString(NumberFormatInfo.InvariantInfo);
			byte[] array = new byte[text.Length];
			for (int i = 0; i < text.Length; i++)
			{
				array[i] = (byte)text[i];
			}
			PutSize(array.Length);
			PutData(array);
		}

		public bool[] GetBooleanArray()
		{
			return (bool[])GetArray(typeof(bool), 1);
		}

		public byte[] GetByteArray()
		{
			return (byte[])GetArray(typeof(byte), 1);
		}

		public char[] GetCharArray()
		{
			return (char[])GetArray(typeof(char), 1);
		}

		public short[] GetInt16Array()
		{
			return (short[])GetArray(typeof(short), 1);
		}

		public int[] GetInt32Array()
		{
			return (int[])GetArray(typeof(int), 1);
		}

		public long[] GetInt64Array()
		{
			return (long[])GetArray(typeof(long), 1);
		}

		public float[] GetSingleArray()
		{
			return (float[])GetArray(typeof(float), 1);
		}

		public double[] GetDoubleArray()
		{
			return (double[])GetArray(typeof(double), 1);
		}

		public decimal[] GetDecimalArray()
		{
			return (decimal[])GetArray(typeof(decimal), 1);
		}

		public string[] GetStringArray()
		{
			return (string[])GetArray(typeof(string), 1);
		}

		public object[] GetComplexArray()
		{
			if (ComplexType == null)
			{
				throw new MathLinkException(1010);
			}
			return (object[])GetArray(ComplexType, 1);
		}

		public virtual string GetFunction(out int argCount)
		{
			ExpressionType expressionType = GetExpressionType();
			if (expressionType != ExpressionType.Function || expressionType != ExpressionType.Complex)
			{
				throw new MathLinkException(1014);
			}
			argCount = GetArgCount();
			return GetSymbol();
		}

		public void PutFunction(string f, int argCount)
		{
			PutNext(ExpressionType.Function);
			PutArgCount(argCount);
			PutSymbol(f);
		}

		public void PutFunctionAndArgs(string f, params object[] args)
		{
			PutFunction(f, args.Length);
			foreach (object obj in args)
			{
				Put(obj);
			}
		}

		public int CheckFunction(string f)
		{
			ILinkMark mark = CreateMark();
			try
			{
				int argCount;
				string function = GetFunction(out argCount);
				if (function != f)
				{
					SeekMark(mark);
					throw new MathLinkException(1015);
				}
				return argCount;
			}
			finally
			{
				DestroyMark(mark);
			}
		}

		public void CheckFunctionWithArgCount(string f, int argCount)
		{
			ILinkMark mark = CreateMark();
			try
			{
				int argCount2;
				string function = GetFunction(out argCount2);
				if (function != f || argCount2 != argCount)
				{
					SeekMark(mark);
					throw new MathLinkException(1015);
				}
			}
			finally
			{
				DestroyMark(mark);
			}
		}

		public Expr GetExpr()
		{
			return Expr.CreateFromLink(this);
		}

		public Expr PeekExpr()
		{
			ILinkMark mark = CreateMark();
			try
			{
				return Expr.CreateFromLink(this);
			}
			finally
			{
				SeekMark(mark);
				DestroyMark(mark);
			}
		}

		public void Connect(long timeoutMillis)
		{
			this.timeoutMillis = timeoutMillis;
			startConnectTime = Environment.TickCount;
			YieldFunction value = connectTimeoutYielder;
			Yield += value;
			try
			{
				Connect();
			}
			finally
			{
				Yield -= value;
			}
		}

		private bool connectTimeoutYielder()
		{
			return Environment.TickCount - startConnectTime > timeoutMillis;
		}

		public virtual object GetObject()
		{
			switch (GetExpressionType())
			{
			case ExpressionType.Integer:
				return GetInteger();
			case ExpressionType.Real:
				return GetDouble();
			case ExpressionType.String:
				return GetString();
			case ExpressionType.Object:
				return getObj();
			case ExpressionType.Boolean:
				return GetBoolean();
			case ExpressionType.Complex:
				return GetComplex();
			case ExpressionType.Symbol:
			{
				ILinkMark mark = CreateMark();
				try
				{
					string symbol = GetSymbol();
					if (symbol == "Null")
					{
						return null;
					}
					SeekMark(mark);
					throw new MathLinkException(1016);
				}
				finally
				{
					DestroyMark(mark);
				}
			}
			case ExpressionType.Function:
				return Utils.readArbitraryArray(this, typeof(Array));
			default:
				return null;
			}
		}

		public virtual object GetComplex()
		{
			return complexHandler.GetComplex(this);
		}

		public virtual Array GetArray(Type leafType, int depth)
		{
			string[] heads;
			return GetArray(leafType, depth, out heads);
		}

		public virtual Array GetArray(Type leafType, int depth, out string[] heads)
		{
			TypeCode typeCode = Type.GetTypeCode(leafType);
			Array array = null;
			int num = Utils.determineIncomingArrayDepth(this);
			if (!((!leafType.IsArray && leafType != typeof(object) && leafType != typeof(Expr)) ? (num == depth) : (num >= depth)))
			{
				throw new MathLinkException(1002);
			}
			if (depth == 1 && !leafType.IsEnum)
			{
				int argCount;
				string function = GetFunction(out argCount);
				heads = new string[1];
				heads[0] = function;
				switch (typeCode)
				{
				case TypeCode.Byte:
				{
					byte[] array10 = new byte[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array10[i] = (byte)GetInteger();
					}
					array = array10;
					break;
				}
				case TypeCode.SByte:
				{
					sbyte[] array4 = new sbyte[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array4[i] = (sbyte)GetInteger();
					}
					array = array4;
					break;
				}
				case TypeCode.Char:
				{
					char[] array12 = new char[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array12[i] = (char)GetInteger();
					}
					array = array12;
					break;
				}
				case TypeCode.Int16:
				{
					short[] array16 = new short[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array16[i] = (short)GetInteger();
					}
					array = array16;
					break;
				}
				case TypeCode.UInt16:
				{
					ushort[] array7 = new ushort[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array7[i] = (ushort)GetInteger();
					}
					array = array7;
					break;
				}
				case TypeCode.Int32:
				{
					int[] array15 = new int[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array15[i] = GetInteger();
					}
					array = array15;
					break;
				}
				case TypeCode.UInt32:
				{
					uint[] array9 = new uint[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array9[i] = (uint)GetDecimal();
					}
					array = array9;
					break;
				}
				case TypeCode.Int64:
				{
					long[] array6 = new long[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array6[i] = (long)GetDecimal();
					}
					array = array6;
					break;
				}
				case TypeCode.UInt64:
				{
					ulong[] array13 = new ulong[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array13[i] = (ulong)GetDecimal();
					}
					array = array13;
					break;
				}
				case TypeCode.Single:
				{
					float[] array8 = new float[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array8[i] = (float)GetDouble();
					}
					array = array8;
					break;
				}
				case TypeCode.Double:
				{
					double[] array5 = new double[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array5[i] = GetDouble();
					}
					array = array5;
					break;
				}
				case TypeCode.Decimal:
				{
					decimal[] array3 = new decimal[argCount];
					for (int i = 0; i < argCount; i++)
					{
						ref decimal reference = ref array3[i];
						reference = GetDecimal();
					}
					array = array3;
					break;
				}
				case TypeCode.Boolean:
				{
					bool[] array14 = new bool[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array14[i] = GetBoolean();
					}
					array = array14;
					break;
				}
				case TypeCode.String:
				{
					string[] array11 = new string[argCount];
					for (int i = 0; i < argCount; i++)
					{
						array11[i] = GetString();
					}
					array = array11;
					break;
				}
				default:
					if (leafType == typeof(Expr))
					{
						Expr[] array2 = new Expr[argCount];
						for (int i = 0; i < argCount; i++)
						{
							array2[i] = GetExpr();
						}
						array = array2;
					}
					else if (leafType == ComplexType)
					{
						array = Array.CreateInstance(leafType, argCount);
						for (int i = 0; i < argCount; i++)
						{
							array.SetValue(GetComplex(), i);
						}
					}
					else
					{
						array = Array.CreateInstance(leafType, argCount);
						for (int i = 0; i < argCount; i++)
						{
							array.SetValue(GetObject(), i);
						}
					}
					break;
				}
			}
			else
			{
				int[] array17 = new int[depth];
				for (int j = 0; j < depth; j++)
				{
					int argCount2;
					string function2 = GetFunction(out argCount2);
					array17[j] = argCount2;
					headsHolder[j] = function2;
				}
				int[] indices = new int[depth];
				array = Array.CreateInstance(leafType, array17);
				bool isEnum = leafType.IsEnum;
				if (array.Length != 0)
				{
					do
					{
						array.SetValue(readAs(typeCode, leafType, isEnum), indices);
						discardInnerHeads(indices, array17);
					}
					while (Utils.nextIndex(indices, array17));
				}
				heads = new string[depth];
				Array.Copy(headsHolder, heads, depth);
			}
			return array;
		}

		public virtual void Put(object obj)
		{
			if (obj == null)
			{
				PutSymbol("Null");
				return;
			}
			Type type = obj.GetType();
			if (obj is int)
			{
				Put((int)obj);
			}
			else if (obj is double)
			{
				Put((double)obj);
			}
			else if (obj is string)
			{
				putString((string)obj);
			}
			else if (obj is byte)
			{
				Put((byte)obj);
			}
			else if (obj is sbyte)
			{
				Put((sbyte)obj);
			}
			else if (obj is char)
			{
				Put((char)obj);
			}
			else if (obj is short)
			{
				Put((short)obj);
			}
			else if (obj is ushort)
			{
				Put((ushort)obj);
			}
			else if (obj is uint)
			{
				Put((decimal)(uint)obj);
			}
			else if (obj is long)
			{
				Put((decimal)(long)obj);
			}
			else if (obj is ulong)
			{
				Put((decimal)(ulong)obj);
			}
			else if (obj is bool)
			{
				Put((bool)obj);
			}
			else if (obj is float)
			{
				Put((float)obj);
			}
			else if (obj is decimal)
			{
				Put((decimal)obj);
			}
			else if (obj is Expr)
			{
				((Expr)obj).Put(this);
			}
			else if (type.IsArray || type == typeof(Array))
			{
				putArray((Array)obj, null);
			}
			else if (ComplexType != null && ComplexType == type)
			{
				putComplex(obj);
			}
			else
			{
				putRef(obj);
			}
		}

		public void Put(Array a, string[] heads)
		{
			if (a == null)
			{
				PutSymbol("Null");
			}
			else
			{
				putArray(a, heads);
			}
		}

		protected void putArrayPiecemeal(Array a, string[] heads, int headIndex)
		{
			if (a == null)
			{
				PutSymbol("Null");
				return;
			}
			a.GetType();
			int rank = a.Rank;
			string f = ((heads != null && heads.Length > headIndex) ? heads[headIndex] : "List");
			int length = a.GetLength(0);
			int lowerBound = a.GetLowerBound(0);
			PutFunction(f, length);
			Type elementType = a.GetType().GetElementType();
			bool isArray = elementType.IsArray;
			switch (rank)
			{
			case 1:
			{
				for (int num3 = 0; num3 < length; num3++)
				{
					if (isArray)
					{
						putArrayPiecemeal((Array)a.GetValue(lowerBound + num3), heads, headIndex + 1);
					}
					else
					{
						Put(a.GetValue(lowerBound + num3));
					}
				}
				break;
			}
			case 2:
			{
				int length2 = a.GetLength(1);
				int lowerBound2 = a.GetLowerBound(1);
				string f2 = ((heads != null && heads.Length > headIndex + 1) ? heads[headIndex + 1] : "List");
				for (int num4 = 0; num4 < length; num4++)
				{
					PutFunction(f2, length2);
					for (int num5 = 0; num5 < length2; num5++)
					{
						if (isArray)
						{
							putArrayPiecemeal((Array)a.GetValue(lowerBound + num4, lowerBound2 + num5), heads, headIndex + 2);
						}
						else
						{
							Put(a.GetValue(lowerBound + num4, lowerBound2 + num5));
						}
					}
				}
				break;
			}
			case 3:
			case 4:
			case 5:
			{
				int[] array = new int[rank];
				int[] array2 = new int[rank];
				for (int i = 0; i < rank; i++)
				{
					array[i] = a.GetLength(i);
					array2[i] = a.GetLowerBound(i);
				}
				int[] array3 = new int[rank];
				f = ((heads != null && heads.Length > headIndex + 1) ? heads[headIndex + 1] : "List");
				for (int j = 0; j < array[0]; j++)
				{
					array3[0] = array2[0] + j;
					PutFunction(f, array[1]);
					f = ((heads != null && heads.Length > headIndex + 2) ? heads[headIndex + 2] : "List");
					for (int k = 0; k < array[1]; k++)
					{
						array3[1] = array2[1] + k;
						PutFunction(f, array[2]);
						if (rank == 3)
						{
							for (int l = 0; l < array[2]; l++)
							{
								array3[2] = array2[2] + l;
								if (isArray)
								{
									putArrayPiecemeal((Array)a.GetValue(array3), heads, headIndex + 3);
								}
								else
								{
									Put(a.GetValue(array3));
								}
							}
							continue;
						}
						f = ((heads != null && heads.Length > headIndex + 3) ? heads[headIndex + 3] : "List");
						for (int m = 0; m < array[2]; m++)
						{
							array3[2] = array2[2] + m;
							PutFunction(f, array[3]);
							if (rank == 4)
							{
								for (int n = 0; n < array[3]; n++)
								{
									array3[3] = array2[3] + n;
									if (isArray)
									{
										putArrayPiecemeal((Array)a.GetValue(array3), heads, headIndex + 4);
									}
									else
									{
										Put(a.GetValue(array3));
									}
								}
								continue;
							}
							f = ((heads != null && heads.Length > headIndex + 4) ? heads[headIndex + 4] : "List");
							for (int num = 0; num < array[3]; num++)
							{
								array3[3] = array2[3] + num;
								PutFunction(f, array[4]);
								for (int num2 = 0; num2 < array[4]; num2++)
								{
									array3[4] = array2[4] + num2;
									if (isArray)
									{
										putArrayPiecemeal((Array)a.GetValue(array3), heads, headIndex + 5);
									}
									else
									{
										Put(a.GetValue(array3));
									}
								}
							}
						}
					}
				}
				break;
			}
			default:
				throw new ArgumentException("Cannot send an array deeper than 5 dimensions using .NET/Link.");
			}
		}

		private object readAs(TypeCode leafTypeCode, Type leafType, bool isEnum)
		{
			if (isEnum)
			{
				int integer = GetInteger();
				if (!Enum.IsDefined(leafType, integer))
				{
					throw new MathLinkException(1011, "Enum value " + integer + " out of range for type " + leafType.FullName + ".");
				}
				return Enum.ToObject(leafType, integer);
			}
			switch (leafTypeCode)
			{
			case TypeCode.Int32:
				return GetInteger();
			case TypeCode.Double:
				return GetDouble();
			case TypeCode.Char:
				return (char)GetInteger();
			case TypeCode.String:
				return GetString();
			case TypeCode.Boolean:
				return GetBoolean();
			case TypeCode.Byte:
				return (byte)GetInteger();
			case TypeCode.SByte:
				return (sbyte)GetInteger();
			case TypeCode.Int16:
				return (short)GetInteger();
			case TypeCode.UInt16:
				return (ushort)GetInteger();
			case TypeCode.UInt32:
				return (uint)GetInteger();
			case TypeCode.Int64:
				return (long)GetDecimal();
			case TypeCode.UInt64:
				return (ulong)GetDecimal();
			case TypeCode.Decimal:
				return GetDecimal();
			case TypeCode.Single:
				return (float)GetDouble();
			default:
				if (leafType == typeof(Expr))
				{
					return GetExpr();
				}
				if (leafType == ComplexType)
				{
					return GetComplex();
				}
				return GetObject();
			}
		}

		private void discardInnerHeads(int[] indices, int[] dims)
		{
			int num = 0;
			int num2 = indices.Length - 1;
			while (num2 >= 0 && indices[num2] == dims[num2] - 1)
			{
				num++;
				num2--;
			}
			if (num >= indices.Length)
			{
				return;
			}
			while (num > 0)
			{
				GetFunction(out var argCount);
				if (argCount != dims[dims.Length - num])
				{
					throw new MathLinkException(1004);
				}
				num--;
			}
		}

		protected abstract void putString(string s);

		protected abstract void putArray(Array a, string[] heads);

		protected abstract void putComplex(object obj);

		protected virtual object getObj()
		{
			throw new InvalidOperationException("Object references can only be read by an IKernelLink instance, not an IMathLink.");
		}

		protected virtual void putRef(object obj)
		{
			putString(obj.ToString());
		}

		public abstract void Close();

		public abstract void Connect();

		public abstract void NewPacket();

		public abstract PacketType NextPacket();

		public abstract void EndPacket();

		public abstract bool ClearError();

		public abstract void Flush();

		public abstract ExpressionType GetNextExpressionType();

		public abstract ExpressionType GetExpressionType();

		public abstract void PutNext(ExpressionType type);

		public abstract int GetArgCount();

		public abstract void PutArgCount(int argCount);

		public abstract void PutSize(int n);

		public abstract void PutData(byte[] data);

		public abstract byte[] GetData(int numRequested);

		public abstract int BytesToGet();

		public abstract int BytesToPut();

		public abstract string GetString();

		public abstract string GetSymbol();

		public abstract void PutSymbol(string s);

		public abstract byte[] GetByteString(int missing);

		public abstract int GetInteger();

		public abstract void Put(int i);

		public abstract double GetDouble();

		public abstract void Put(double d);

		public abstract void TransferExpression(IMathLink source);

		public abstract void TransferToEndOfLoopbackLink(ILoopbackLink source);

		public abstract void PutMessage(MathLinkMessage msg);

		public abstract ILinkMark CreateMark();

		public abstract void SeekMark(ILinkMark mark);

		public abstract void DestroyMark(ILinkMark mark);

		public abstract void DeviceInformation(int selector, IntPtr buf, ref int len);
	}
}
