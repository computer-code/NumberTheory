using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Wolfram.NETLink.Internal;

namespace Wolfram.NETLink
{
	[Serializable]
	public sealed class Expr : IDisposable, ISerializable
	{
		private const int INTEGER = 1;

		private const int REAL = 2;

		private const int STRING = 3;

		private const int SYMBOL = 4;

		private const int UNKNOWN = 0;

		private const int FIRST_COMPOSITE = 100;

		private const int FUNCTION = 100;

		private const int FIRST_ARRAY_TYPE = 200;

		private const int INTARRAY1 = 200;

		private const int REALARRAY1 = 201;

		private const int INTARRAY2 = 202;

		private const int REALARRAY2 = 203;

		public static readonly Expr SYM_SYMBOL = new Expr(ExpressionType.Symbol, "Symbol");

		public static readonly Expr SYM_INTEGER = new Expr(ExpressionType.Symbol, "Integer");

		public static readonly Expr SYM_REAL = new Expr(ExpressionType.Symbol, "Real");

		public static readonly Expr SYM_STRING = new Expr(ExpressionType.Symbol, "String");

		public static readonly Expr SYM_LIST = new Expr(ExpressionType.Symbol, "List");

		public static readonly Expr SYM_TRUE = new Expr(ExpressionType.Symbol, "True");

		public static readonly Expr SYM_FALSE = new Expr(ExpressionType.Symbol, "False");

		public static readonly Expr INT_ONE = new Expr(1);

		public static readonly Expr INT_ZERO = new Expr(0);

		public static readonly Expr INT_MINUSONE = new Expr(-1);

		private int type;

		private Expr head;

		private Expr[] args;

		private object val;

		[NonSerialized]
		private ILoopbackLink link;

		private volatile int cachedHashCode;

		public Expr this[int part] => Part(part);

		public Expr Head
		{
			get
			{
				prepareFromLoopback();
				if (type >= 200)
				{
					return SYM_LIST;
				}
				return head;
			}
		}

		public Expr[] Args => (Expr[])nonCopyingArgs().Clone();

		public int Length
		{
			get
			{
				prepareFromLoopback();
				if (type >= 200)
				{
					return ((Array)val).GetLength(0);
				}
				if (args == null)
				{
					return 0;
				}
				return args.Length;
			}
		}

		public int[] Dimensions
		{
			get
			{
				prepareFromLoopback();
				int[] array = null;
				if (type < 100)
				{
					array = new int[0];
				}
				else
				{
					switch (type)
					{
					case 200:
					case 201:
						array = new int[1]
						{
							((Array)val).GetLength(0)
						};
						break;
					case 202:
					case 203:
						array = new int[2]
						{
							((Array)val).GetLength(0),
							((Array)val).GetLength(1)
						};
						break;
					case 100:
					{
						if (args.Length == 0)
						{
							array = new int[1]
							{
								0
							};
							break;
						}
						int[] dimensions = args[0].Dimensions;
						int[] array2 = new int[dimensions.Length + 1];
						array2[0] = args.Length;
						Array.Copy(dimensions, 0, array2, 1, dimensions.Length);
						int num = 1 + dimensions.Length;
						for (int i = 1; i < args.Length; i++)
						{
							if (num == 1)
							{
								break;
							}
							int[] dimensions2 = args[i].Dimensions;
							num = Math.Min(num, 1 + dimensions2.Length);
							for (int j = 1; j < num; j++)
							{
								if (array2[j] != dimensions2[j - 1])
								{
									num = j;
									break;
								}
							}
						}
						string text = Head.ToString();
						int num2 = checkHeads(text, 0, num);
						array = new int[num2];
						Array.Copy(array2, 0, array, 0, num2);
						break;
					}
					}
				}
				return array;
			}
		}

		private Expr()
		{
		}

		public Expr(ExpressionType type, string val)
		{
			this.type = internalTypeFromExpressionType(type);
			switch (type)
			{
			case ExpressionType.Integer:
				head = SYM_INTEGER;
				try
				{
					this.val = Convert.ToInt64(val);
				}
				catch (Exception)
				{
					this.val = Convert.ToDecimal(val);
				}
				break;
			case ExpressionType.Real:
				head = SYM_REAL;
				try
				{
					this.val = Convert.ToDouble(val, NumberFormatInfo.InvariantInfo);
				}
				catch (Exception)
				{
					this.val = Convert.ToDecimal(val, NumberFormatInfo.InvariantInfo);
				}
				break;
			case ExpressionType.String:
				head = SYM_STRING;
				this.val = val;
				break;
			case ExpressionType.Symbol:
				if (val == "Symbol")
				{
					head = this;
				}
				else
				{
					head = SYM_SYMBOL;
				}
				this.val = val;
				break;
			case ExpressionType.Boolean:
				head = SYM_SYMBOL;
				this.val = ((val == "True" || val == "true") ? true : false);
				break;
			default:
				throw new ArgumentException(string.Concat("ExpressionType ", type, " is not supported in the Expr(ExpressionType, string) constructor."));
			}
		}

		public Expr(object obj)
		{
			if (obj == null)
			{
				val = "Null";
				type = 4;
				head = SYM_SYMBOL;
				return;
			}
			if (obj is int || obj is byte || obj is sbyte || obj is short || obj is ushort || obj is char || obj is uint || obj is long)
			{
				val = Convert.ToInt64(obj);
				type = 1;
				head = SYM_INTEGER;
				return;
			}
			if (obj is ulong)
			{
				val = Convert.ToDecimal(obj);
				type = 1;
				head = SYM_INTEGER;
				return;
			}
			if (obj is double)
			{
				val = obj;
				type = 2;
				head = SYM_REAL;
				return;
			}
			if (obj is float)
			{
				val = Convert.ToDouble(obj);
				type = 2;
				head = SYM_REAL;
				return;
			}
			if (obj is decimal)
			{
				val = obj;
				if (decimal.Truncate((decimal)val) == (decimal)val)
				{
					type = 1;
					head = SYM_INTEGER;
				}
				else
				{
					type = 2;
					head = SYM_REAL;
				}
				return;
			}
			if (obj is string)
			{
				val = obj;
				type = 3;
				head = SYM_STRING;
				return;
			}
			if (obj is bool)
			{
				val = (((bool)obj) ? "True" : "False");
				type = 4;
				head = SYM_SYMBOL;
				return;
			}
			if (obj is Expr)
			{
				((Expr)obj).prepareFromLoopback();
				type = ((Expr)obj).type;
				head = ((Expr)obj).head;
				args = ((Expr)obj).args;
				val = ((Expr)obj).val;
				return;
			}
			if (obj is Array || obj.GetType() == typeof(Array))
			{
				Array array = (Array)obj;
				if (array is int[] || array is byte[] || array is sbyte[] || array is short[] || array is ushort[] || array is char[])
				{
					val = new int[array.Length];
					Array.Copy(array, (Array)val, array.Length);
					type = 200;
					return;
				}
				if (array is uint[] || array is long[] || array is ulong[])
				{
					int[] array2 = new int[array.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array2[i] = (int)array.GetValue(i);
					}
					val = array2;
					type = 200;
					return;
				}
				if (array is double[] || array is float[])
				{
					val = new double[array.Length];
					Array.Copy(array, (Array)val, array.Length);
					type = 201;
					return;
				}
				if (array is decimal[])
				{
					double[] array3 = new double[array.Length];
					for (int j = 0; j < array.Length; j++)
					{
						array3[j] = (double)array.GetValue(j);
					}
					val = array3;
					type = 201;
					return;
				}
				if (array is string[])
				{
					val = new string[array.Length];
					Array.Copy(array, (Array)val, array.Length);
					type = 100;
					return;
				}
				if (array is bool[])
				{
					val = new string[array.Length];
					for (int k = 0; k < array.Length; k++)
					{
						((Array)val).SetValue(((bool)array.GetValue(k)) ? "True" : "False", k);
					}
					type = 100;
					return;
				}
				if (array is int[,] || array is byte[,] || array is sbyte[,] || array is short[,] || array is ushort[,] || array is char[,])
				{
					val = new int[array.GetLength(0), array.GetLength(1)];
					Array.Copy(array, (Array)val, array.Length);
					type = 202;
					return;
				}
				if (array is double[,] || array is float[,])
				{
					val = new double[array.GetLength(0), array.GetLength(1)];
					Array.Copy(array, (Array)val, array.Length);
					type = 203;
					return;
				}
				if (array is ulong[,] || array is uint[,] || array is long[,])
				{
					int[,] array4 = new int[array.GetLength(0), array.GetLength(1)];
					for (int l = 0; l < array.GetLength(0); l++)
					{
						int num = 0;
						while (num < array.GetLength(1))
						{
							array4[l, num] = (int)array.GetValue(l, num);
							l++;
						}
					}
					val = array4;
					type = 202;
					return;
				}
				if (array is decimal[,])
				{
					double[,] array5 = new double[array.GetLength(0), array.GetLength(1)];
					for (int m = 0; m < array.GetLength(0); m++)
					{
						int num2 = 0;
						while (num2 < array.GetLength(1))
						{
							array5[m, num2] = (double)array.GetValue(m, num2);
							m++;
						}
					}
					val = array5;
					type = 203;
					return;
				}
				if (array is string[,])
				{
					val = new string[array.GetLength(0), array.GetLength(1)];
					Array.Copy(array, (Array)val, array.Length);
					type = 100;
					return;
				}
				if (array is bool[,])
				{
					val = new string[array.GetLength(0), array.GetLength(1)];
					for (int n = 0; n < array.GetLength(0); n++)
					{
						for (int num3 = 0; num3 < array.GetLength(1); num3++)
						{
							((Array)val).SetValue(((bool)array.GetValue(n, num3)) ? "True" : "False", n, num3);
						}
					}
					type = 100;
					return;
				}
				throw new ArgumentException("Cannot construct an Expr from the supplied array object: " + obj);
			}
			throw new ArgumentException("Cannot construct an Expr from the supplied object: " + obj);
		}

		public Expr(object head, params object[] args)
		{
			type = 100;
			this.head = ((head is Expr) ? ((Expr)head) : new Expr(head));
			this.args = new Expr[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				this.args[i] = ((args[i] is Expr) ? ((Expr)args[i]) : new Expr(args[i]));
			}
		}

		public static Expr CreateFromLink(IMathLink ml)
		{
			return createFromLink(ml, allowLoopback: true);
		}

		~Expr()
		{
			disposer();
		}

		public void Dispose()
		{
			disposer();
			GC.SuppressFinalize(this);
		}

		private void disposer()
		{
			lock (this)
			{
				if (link != null)
				{
					link.Close();
					link = null;
				}
			}
		}

		private Expr(SerializationInfo info, StreamingContext context)
		{
			type = info.GetInt32("type");
			head = (Expr)info.GetValue("head", typeof(Expr));
			args = (Expr[])info.GetValue("args", typeof(Expr[]));
			val = info.GetValue("val", typeof(object));
			link = null;
			cachedHashCode = 0;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			prepareFromLoopback();
			info.AddValue("type", type);
			info.AddValue("head", head);
			info.AddValue("args", args);
			info.AddValue("val", val);
		}

		public override string ToString()
		{
			string result = null;
			prepareFromLoopback();
			switch (type)
			{
			case 1:
			case 4:
				result = val.ToString();
				break;
			case 2:
				result = doubleToInputFormString(Convert.ToDouble(val));
				break;
			case 3:
			{
				result = val.ToString();
				StringBuilder stringBuilder2 = new StringBuilder(result.Length + 10);
				stringBuilder2.Append('"');
				int length3 = result.Length;
				for (int k = 0; k < length3; k++)
				{
					char c = result[k];
					if (c == '\\' || c == '"')
					{
						stringBuilder2.Append('\\');
					}
					stringBuilder2.Append(c);
				}
				stringBuilder2.Append('"');
				result = stringBuilder2.ToString();
				break;
			}
			case 100:
			{
				bool flag = ListQ();
				int length4 = Length;
				StringBuilder stringBuilder3 = new StringBuilder(length4 * 2);
				stringBuilder3.Append(flag ? "{" : (Head.ToString() + "["));
				for (int l = 1; l <= length4; l++)
				{
					stringBuilder3.Append(this[l].ToString());
					if (l < length4)
					{
						stringBuilder3.Append(',');
					}
				}
				stringBuilder3.Append(flag ? '}' : ']');
				result = stringBuilder3.ToString();
				break;
			}
			case 200:
			case 201:
			{
				int length5 = ((Array)val).GetLength(0);
				int[] array3 = ((type == 200) ? ((int[])val) : null);
				double[] array4 = ((type == 201) ? ((double[])val) : null);
				StringBuilder stringBuilder4 = new StringBuilder(length5 * 2);
				stringBuilder4.Append('{');
				for (int m = 0; m < length5; m++)
				{
					stringBuilder4.Append((type == 200) ? array3[m].ToString() : doubleToInputFormString(array4[m]));
					if (m < length5 - 1)
					{
						stringBuilder4.Append(',');
					}
				}
				stringBuilder4.Append('}');
				result = stringBuilder4.ToString();
				break;
			}
			case 202:
			case 203:
			{
				int length = ((Array)val).GetLength(0);
				int length2 = ((Array)val).GetLength(1);
				int[,] array = ((type == 202) ? ((int[,])val) : null);
				double[,] array2 = ((type == 203) ? ((double[,])val) : null);
				StringBuilder stringBuilder = new StringBuilder(length * length2 * 2);
				stringBuilder.Append('{');
				for (int i = 0; i < length; i++)
				{
					stringBuilder.Append('{');
					for (int j = 0; j < length2; j++)
					{
						stringBuilder.Append((type == 202) ? array[i, j].ToString() : doubleToInputFormString(array2[i, j]));
						if (j < length2 - 1)
						{
							stringBuilder.Append(',');
						}
					}
					stringBuilder.Append((i < length - 1) ? "}," : "}");
				}
				stringBuilder.Append('}');
				result = stringBuilder.ToString();
				break;
			}
			}
			return result;
		}

		public static bool operator ==(Expr x, Expr y)
		{
			return x?.Equals(y) ?? ((object)y == null);
		}

		public static bool operator !=(Expr x, Expr y)
		{
			return !(x == y);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			if (object.ReferenceEquals(this, obj))
			{
				return true;
			}
			Expr expr = (Expr)obj;
			if (cachedHashCode != 0 && expr.cachedHashCode != 0 && cachedHashCode != expr.cachedHashCode)
			{
				return false;
			}
			expr.prepareFromLoopback();
			prepareFromLoopback();
			if (type != expr.type)
			{
				return false;
			}
			if (val != null)
			{
				if (expr.val == null)
				{
					return false;
				}
				switch (type)
				{
				case 1:
				case 2:
				case 3:
				case 4:
					return val.Equals(expr.val);
				case 200:
				{
					int[] array7 = (int[])val;
					int[] array8 = (int[])expr.val;
					if (array7.Length != array8.Length)
					{
						return false;
					}
					for (int l = 0; l < array7.Length; l++)
					{
						if (array7[l] != array8[l])
						{
							return false;
						}
					}
					return true;
				}
				case 201:
				{
					double[] array5 = (double[])val;
					double[] array6 = (double[])expr.val;
					if (array5.Length != array6.Length)
					{
						return false;
					}
					for (int k = 0; k < array5.Length; k++)
					{
						if (array5[k] != array6[k])
						{
							return false;
						}
					}
					return true;
				}
				case 202:
				{
					int[,] array3 = (int[,])val;
					int[,] array4 = (int[,])expr.val;
					if (array3.GetLength(0) != array4.GetLength(0) || array3.GetLength(1) != array4.GetLength(1))
					{
						return false;
					}
					for (int j = 0; j < array3.GetLength(0); j++)
					{
						int num2 = 0;
						for (; j < array3.GetLength(1); j++)
						{
							if (array3[j, num2] != array4[j, num2])
							{
								return false;
							}
						}
					}
					return true;
				}
				case 203:
				{
					double[,] array = (double[,])val;
					double[,] array2 = (double[,])expr.val;
					if (array.GetLength(0) != array2.GetLength(0) || array.GetLength(1) != array2.GetLength(1))
					{
						return false;
					}
					for (int i = 0; i < array.GetLength(0); i++)
					{
						int num = 0;
						for (; i < array.GetLength(1); i++)
						{
							if (array[i, num] != array2[i, num])
							{
								return false;
							}
						}
					}
					return true;
				}
				default:
					return false;
				}
			}
			if (expr.val != null)
			{
				return false;
			}
			if (!head.Equals(expr.head))
			{
				return false;
			}
			if (args.Length != expr.args.Length)
			{
				return false;
			}
			for (int m = 0; m < args.Length; m++)
			{
				if (!args[m].Equals(expr.args[m]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			if (cachedHashCode != 0)
			{
				return cachedHashCode;
			}
			prepareFromLoopback();
			if (type < 100)
			{
				return val.GetHashCode();
			}
			int num = 17;
			num = 37 * num + type;
			if (head != null)
			{
				num = 37 * num + head.GetHashCode();
			}
			if (args != null)
			{
				for (int i = 0; i < args.Length; i++)
				{
					num = 37 * num + args[i].GetHashCode();
				}
			}
			if (val != null)
			{
				if (type < 200)
				{
					num = 37 * num + val.GetHashCode();
				}
				else if (type == 200)
				{
					int[] array = (int[])val;
					int[] array2 = array;
					foreach (int num2 in array2)
					{
						num += num2;
					}
				}
				else if (type == 201)
				{
					double[] array3 = (double[])val;
					double[] array4 = array3;
					foreach (double num3 in array4)
					{
						num += (int)num3;
					}
				}
				else if (type == 202)
				{
					int[,] array5 = (int[,])val;
					int[,] array6 = array5;
					foreach (int num4 in array6)
					{
						num += num4;
					}
				}
				else if (type == 203)
				{
					double[,] array7 = (double[,])val;
					double[,] array8 = array7;
					foreach (double num6 in array8)
					{
						num += (int)num6;
					}
				}
			}
			cachedHashCode = num;
			return num;
		}

		internal int inheritedHashCode()
		{
			return base.GetHashCode();
		}

		public static explicit operator long(Expr e)
		{
			return e.AsInt64();
		}

		public static explicit operator double(Expr e)
		{
			return e.AsDouble();
		}

		public static explicit operator string(Expr e)
		{
			return e.ToString();
		}

		public long AsInt64()
		{
			prepareFromLoopback();
			try
			{
				if (type == 1)
				{
					return Convert.ToInt64(val);
				}
				throw new ArgumentException();
			}
			catch (Exception)
			{
				throw new ExprFormatException("This Expr cannot be represented as a .NET Int64 value.");
			}
		}

		public double AsDouble()
		{
			prepareFromLoopback();
			try
			{
				if (type == 2 || type == 1)
				{
					return Convert.ToDouble(val);
				}
				if (RationalQ())
				{
					return Part(1).AsDouble() / Part(2).AsDouble();
				}
				throw new ArgumentException();
			}
			catch (Exception)
			{
				throw new ExprFormatException("This Expr cannot be represented as a .NET Double value.");
			}
		}

		public Array AsArray(ExpressionType reqType, int depth)
		{
			prepareFromLoopback();
			if (depth > 2)
			{
				throw new ArgumentException("Depths > 2 are not supported in Expr.AsArray()");
			}
			if (reqType != ExpressionType.Integer && reqType != ExpressionType.Real)
			{
				throw new ArgumentException("Unsupported type in Expr.AsArray(): " + reqType);
			}
			switch (type)
			{
			case 200:
				if (depth != 1 || reqType != ExpressionType.Integer)
				{
					throw new ExprFormatException("This Expr cannot be represented as a .NET array of the requested type and/or depth.");
				}
				return (int[])((int[])val).Clone();
			case 201:
				if (depth != 1 || reqType != ExpressionType.Real)
				{
					throw new ExprFormatException("This Expr cannot be represented as a .NET array of the requested type and/or depth.");
				}
				return (double[])((double[])val).Clone();
			case 202:
				if (depth != 2 || reqType != ExpressionType.Integer)
				{
					throw new ExprFormatException("This Expr cannot be represented as a .NET array of the requested type and/or depth.");
				}
				return (int[,])((int[,])val).Clone();
			case 203:
				if (depth != 2 || reqType != ExpressionType.Real)
				{
					throw new ExprFormatException("This Expr cannot be represented as a .NET array of the requested type and/or depth.");
				}
				return (double[,])((double[,])val).Clone();
			case 100:
				if (depth == 1)
				{
					if (reqType == ExpressionType.Integer)
					{
						int[] array = new int[args.Length];
						for (int i = 0; i < args.Length; i++)
						{
							if (!args[i].IntegerQ())
							{
								throw new ExprFormatException("This Expr cannot be represented as a .NET array of ints because some elements are not integers");
							}
							array[i] = Convert.ToInt32(args[i].val);
						}
						return array;
					}
					double[] array2 = new double[args.Length];
					for (int j = 0; j < args.Length; j++)
					{
						if (!args[j].RealQ() && !args[j].IntegerQ())
						{
							throw new ExprFormatException("This Expr cannot be represented as a .NET array of doubles because some elements are not real numbers");
						}
						array2[j] = Convert.ToDouble(args[j].val);
					}
					return array2;
				}
				try
				{
					if (reqType == ExpressionType.Integer)
					{
						int[,] array3 = new int[args.Length, args[0].Length];
						for (int k = 0; k < args.Length; k++)
						{
							int[] array4 = (int[])args[k].AsArray(reqType, 1);
							for (int l = 0; l < array4.Length; l++)
							{
								array3[k, l] = array4[l];
							}
						}
						return array3;
					}
					double[,] array5 = new double[args.Length, args[0].Length];
					for (int m = 0; m < args.Length; m++)
					{
						double[] array6 = (double[])args[m].AsArray(reqType, 1);
						for (int n = 0; n < array6.Length; n++)
						{
							array5[m, n] = array6[n];
						}
					}
					return array5;
				}
				catch (Exception)
				{
					throw new ExprFormatException("This Expr cannot be represented as a .NET array of the requested type and/or depth.");
				}
			default:
				throw new ExprFormatException("This Expr cannot be represented as a .NET array of the requested type and/or depth.");
			}
		}

		public Expr Part(int i)
		{
			prepareFromLoopback();
			if (Math.Abs(i) > Length)
			{
				throw new IndexOutOfRangeException("Cannot take part " + i + " from this Expr because it has length " + Length + ".");
			}
			if (i == 0)
			{
				return Head;
			}
			if (i > 0)
			{
				return nonCopyingArgs()[i - 1];
			}
			return nonCopyingArgs()[Length + i];
		}

		public Expr Part(int[] ia)
		{
			try
			{
				int num = ia.Length;
				if (num == 1)
				{
					return Part(ia[0]);
				}
				int[] array = new int[num - 1];
				Array.Copy(ia, 0, array, 0, num - 1);
				return Part(array).Part(ia[num - 1]);
			}
			catch (IndexOutOfRangeException)
			{
				throw new IndexOutOfRangeException(string.Concat("Part ", new Expr(ia), " of this Expr does not exist."));
			}
		}

		public Expr Take(int n)
		{
			int num = Math.Abs(n);
			int num2 = nonCopyingArgs().Length;
			if (num > num2)
			{
				throw new ArgumentException("Cannot take " + n + " elements from this Expr because it has length " + num2 + ".");
			}
			Expr[] destinationArray = new Expr[num];
			if (n >= 0)
			{
				Array.Copy(args, 0, destinationArray, 0, num);
			}
			else
			{
				Array.Copy(args, num2 - num, destinationArray, 0, num);
			}
			return new Expr(head, destinationArray);
		}

		public Expr Delete(int n)
		{
			int num = nonCopyingArgs().Length;
			if (n == 0 || Math.Abs(n) > num)
			{
				throw new ArgumentException(n + " is an invalid deletion position in this Expr.");
			}
			Expr[] destinationArray = new Expr[num - 1];
			if (n > 0)
			{
				Array.Copy(args, 0, destinationArray, 0, n - 1);
				Array.Copy(args, n, destinationArray, n - 1, num - n);
			}
			else
			{
				Array.Copy(args, 0, destinationArray, 0, num + n);
				Array.Copy(args, num + n + 1, destinationArray, num + n, -n - 1);
			}
			return new Expr(head, destinationArray);
		}

		public Expr Insert(Expr e, int n)
		{
			int num = nonCopyingArgs().Length;
			if (n == 0 || Math.Abs(n) > num + 1)
			{
				throw new ArgumentException(n + " is an invalid insertion position into this Expr.");
			}
			Expr[] array = new Expr[num + 1];
			if (n > 0)
			{
				Array.Copy(args, 0, array, 0, n - 1);
				array[n - 1] = e;
				Array.Copy(args, n - 1, array, n, num - (n - 1));
			}
			else
			{
				Array.Copy(args, 0, array, 0, num + n + 1);
				array[num + n + 1] = e;
				Array.Copy(args, num + n + 1, array, num + n + 2, -n - 1);
			}
			return new Expr(head, array);
		}

		public bool AtomQ()
		{
			prepareFromLoopback();
			if (type < 100)
			{
				return true;
			}
			if (type == 100)
			{
				object obj = Head.val;
				if (obj != null)
				{
					string text = obj.ToString();
					if (text.Equals("Rational") || text.Equals("Complex"))
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool StringQ()
		{
			prepareFromLoopback();
			return type == 3;
		}

		public bool SymbolQ()
		{
			prepareFromLoopback();
			return type == 4;
		}

		public bool IntegerQ()
		{
			prepareFromLoopback();
			return type == 1;
		}

		public bool RealQ()
		{
			prepareFromLoopback();
			return type == 2;
		}

		public bool RationalQ()
		{
			prepareFromLoopback();
			if (type == 100)
			{
				return Head.ToString() == "Rational";
			}
			return false;
		}

		public bool ComplexQ()
		{
			prepareFromLoopback();
			if (type == 100)
			{
				return Head.ToString() == "Complex";
			}
			return false;
		}

		public bool NumberQ()
		{
			if (!IntegerQ() && !RealQ() && !RationalQ())
			{
				return ComplexQ();
			}
			return true;
		}

		public bool TrueQ()
		{
			prepareFromLoopback();
			if (type == 4)
			{
				return val.ToString() == "True";
			}
			return false;
		}

		public bool ListQ()
		{
			prepareFromLoopback();
			if (type < 200)
			{
				if (type == 100 && head.type == 4)
				{
					return head.val.ToString() == "List";
				}
				return false;
			}
			return true;
		}

		public bool VectorQ()
		{
			prepareFromLoopback();
			if (type == 200 || type == 201)
			{
				return true;
			}
			if (type == 202 || type == 203 || !ListQ())
			{
				return false;
			}
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].ListQ())
				{
					return false;
				}
			}
			return true;
		}

		public bool VectorQ(ExpressionType elementType)
		{
			if (!VectorQ())
			{
				return false;
			}
			switch (type)
			{
			case 200:
				return elementType == ExpressionType.Integer;
			case 201:
				return elementType == ExpressionType.Real;
			case 202:
			case 203:
				return false;
			default:
			{
				int num = internalTypeFromExpressionType(elementType);
				int length = Length;
				for (int i = 0; i < length; i++)
				{
					args[i].prepareFromLoopback();
					if (args[i].type != num)
					{
						return false;
					}
				}
				return true;
			}
			}
		}

		public bool MatrixQ()
		{
			prepareFromLoopback();
			if (type == 202 || type == 203)
			{
				return true;
			}
			if (type == 200 || type == 201 || !ListQ())
			{
				return false;
			}
			if (args.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < args.Length; i++)
			{
				if (!args[i].VectorQ())
				{
					return false;
				}
			}
			return Dimensions.Length >= 2;
		}

		public bool MatrixQ(ExpressionType elementType)
		{
			if (!MatrixQ())
			{
				return false;
			}
			int num = internalTypeFromExpressionType(elementType);
			if ((num == 1 && type == 202) || (num == 2 && type == 203))
			{
				return true;
			}
			int length = Length;
			nonCopyingArgs();
			for (int i = 0; i < length; i++)
			{
				if (!args[i].VectorQ(elementType))
				{
					return false;
				}
			}
			return true;
		}

		public void Put(IMathLink ml)
		{
			lock (this)
			{
				if (link != null)
				{
					ILinkMark mark = link.CreateMark();
					try
					{
						ml.TransferExpression(link);
					}
					finally
					{
						ml.ClearError();
						link.SeekMark(mark);
						link.DestroyMark(mark);
					}
				}
				else if (val != null)
				{
					if (type == 4)
					{
						ml.PutSymbol((string)val);
					}
					else
					{
						ml.Put(val);
					}
				}
				else
				{
					ml.PutNext(ExpressionType.Function);
					ml.PutArgCount(args.Length);
					ml.Put(head);
					for (int i = 0; i < args.Length; i++)
					{
						ml.Put(args[i]);
					}
				}
			}
		}

		private int internalTypeFromExpressionType(ExpressionType t)
		{
			return t switch
			{
				ExpressionType.Integer => 1, 
				ExpressionType.Real => 2, 
				ExpressionType.String => 3, 
				ExpressionType.Symbol => 4, 
				ExpressionType.Complex => 100, 
				ExpressionType.Boolean => 4, 
				ExpressionType.Object => throw new ArgumentException("You cannot currently create an Expr of type Object."), 
				ExpressionType.Function => throw new ArgumentException("You cannot directly create an Expr of type Function. You must build it out of other Exprs."), 
				_ => throw new ArgumentException("Unknown ExpressionType: " + t), 
			};
		}

		private void prepareFromLoopback()
		{
			lock (this)
			{
				if (link == null)
				{
					return;
				}
				try
				{
					fillFromLink(link);
				}
				catch (MathLinkException)
				{
				}
				finally
				{
					link.Close();
					link = null;
				}
			}
		}

		private static Expr createFromLink(IMathLink ml, bool allowLoopback)
		{
			ExpressionType nextExpressionType = ml.GetNextExpressionType();
			switch (nextExpressionType)
			{
			case ExpressionType.String:
			case ExpressionType.Symbol:
			case ExpressionType.Real:
			case ExpressionType.Integer:
			case ExpressionType.Boolean:
				return createAtomicExpr(ml, nextExpressionType);
			case ExpressionType.Object:
				return createAtomicExpr(ml, ExpressionType.Symbol);
			default:
			{
				Expr expr = new Expr();
				if (allowLoopback && NativeLink.canUseMathLinkLibrary())
				{
					expr.link = MathLinkFactory.CreateLoopbackLink();
					expr.link.TransferExpression(ml);
					expr.type = 0;
				}
				else
				{
					expr.fillFromLink(ml);
				}
				return expr;
			}
			}
		}

		private void fillFromLink(IMathLink ml)
		{
			lock (this)
			{
				switch (ml.GetExpressionType())
				{
				case ExpressionType.Complex:
				case ExpressionType.Function:
					try
					{
						int argCount = ml.GetArgCount();
						head = createFromLink(ml, allowLoopback: false);
						type = 100;
						args = new Expr[argCount];
						for (int i = 0; i < argCount; i++)
						{
							args[i] = createFromLink(ml, allowLoopback: false);
						}
					}
					catch (MathLinkException ex)
					{
						throw ex;
					}
					finally
					{
						ml.ClearError();
					}
					break;
				default:
					_ = 35;
					break;
				case ExpressionType.String:
				case ExpressionType.Real:
				case ExpressionType.Integer:
				case ExpressionType.Boolean:
					break;
				}
			}
		}

		private static Expr createAtomicExpr(IMathLink ml, ExpressionType type)
		{
			Expr expr = null;
			switch (type)
			{
			case ExpressionType.Integer:
			{
				string @string = ml.GetString();
				switch (@string)
				{
				case "0":
					expr = INT_ZERO;
					break;
				case "1":
					expr = INT_ONE;
					break;
				case "-1":
					expr = INT_MINUSONE;
					break;
				default:
					expr = new Expr();
					expr.head = SYM_INTEGER;
					try
					{
						expr.val = Convert.ToInt64(@string);
					}
					catch (Exception)
					{
						expr.val = Convert.ToDecimal(@string);
					}
					expr.type = 1;
					break;
				}
				break;
			}
			case ExpressionType.Real:
			{
				expr = new Expr();
				expr.head = SYM_REAL;
				string string2 = ml.GetString();
				try
				{
					expr.val = Convert.ToDouble(string2, NumberFormatInfo.InvariantInfo);
				}
				catch (Exception)
				{
					expr.val = Utils.DecimalFromString(string2);
				}
				expr.type = 2;
				break;
			}
			case ExpressionType.String:
				expr = new Expr();
				expr.type = 3;
				expr.head = SYM_STRING;
				expr.val = ml.GetString();
				break;
			case ExpressionType.Symbol:
			{
				string symbol = ml.GetSymbol();
				if (symbol == "List")
				{
					expr = SYM_LIST;
					break;
				}
				expr = new Expr();
				expr.type = 4;
				expr.head = SYM_SYMBOL;
				expr.val = symbol;
				break;
			}
			case ExpressionType.Boolean:
				expr = (ml.GetBoolean() ? SYM_TRUE : SYM_FALSE);
				break;
			}
			return expr;
		}

		private Expr[] nonCopyingArgs()
		{
			lock (this)
			{
				prepareFromLoopback();
				if (args == null)
				{
					if (type < 100)
					{
						args = new Expr[0];
					}
					else if (type == 200 || type == 201)
					{
						Array array = (Array)val;
						args = new Expr[array.GetLength(0)];
						for (int i = 0; i < args.Length; i++)
						{
							args[i] = new Expr(array.GetValue(i));
						}
					}
					else if (type == 202 || type == 203)
					{
						args = new Expr[((Array)val).GetLength(0)];
						int length = ((Array)val).GetLength(1);
						for (int j = 0; j < args.Length; j++)
						{
							args[j] = new Expr();
							args[j].head = SYM_LIST;
							if (type == 202)
							{
								int[,] array2 = (int[,])val;
								int[] array3 = new int[length];
								for (int k = 0; k < length; k++)
								{
									array3[k] = array2[j, k];
								}
								args[j].type = 200;
								args[j].val = array3;
							}
							else
							{
								double[,] array4 = (double[,])val;
								double[] array5 = new double[length];
								for (int l = 0; l < length; l++)
								{
									array5[l] = array4[j, l];
								}
								args[j].type = 201;
								args[j].val = array5;
							}
						}
					}
				}
				return args;
			}
		}

		private int checkHeads(string head, int curDepth, int maxDepth)
		{
			if (args == null || curDepth > maxDepth || Head.ToString() != head)
			{
				return curDepth;
			}
			curDepth++;
			for (int i = 0; i < args.Length; i++)
			{
				int num = args[i].checkHeads(head, curDepth, maxDepth);
				if (num < maxDepth)
				{
					maxDepth = num;
				}
			}
			return maxDepth;
		}

		private static string doubleToInputFormString(double d)
		{
			string text = d.ToString(NumberFormatInfo.InvariantInfo);
			int num = text.LastIndexOf('e');
			if (num == -1)
			{
				num = text.LastIndexOf('E');
			}
			if (num != -1)
			{
				text = text.Substring(0, num) + "*^" + text.Substring(num + 1);
			}
			if (text.IndexOf('.') == -1)
			{
				text += ".";
			}
			return text;
		}
	}
}
