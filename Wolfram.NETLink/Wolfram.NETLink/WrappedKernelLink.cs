using System;

namespace Wolfram.NETLink
{
	public class WrappedKernelLink : KernelLinkImpl
	{
		protected IMathLink impl;

		private bool linkConnected;

		public override int Error => impl.Error;

		public override string ErrorMessage => impl.ErrorMessage;

		public override bool Ready => impl.Ready;

		public override string Name => impl.Name;

		public override Type ComplexType
		{
			get
			{
				return impl.ComplexType;
			}
			set
			{
				impl.ComplexType = value;
			}
		}

		public override event YieldFunction Yield
		{
			add
			{
				impl.Yield += value;
			}
			remove
			{
				impl.Yield -= value;
			}
		}

		public override event MessageHandler MessageArrived
		{
			add
			{
				impl.MessageArrived += value;
			}
			remove
			{
				impl.MessageArrived -= value;
			}
		}

		public WrappedKernelLink()
			: this(null)
		{
		}

		public WrappedKernelLink(IMathLink ml)
		{
			SetMathLink(ml);
			MessageArrived += interruptDetector;
		}

		public IMathLink GetMathLink()
		{
			return impl;
		}

		public void SetMathLink(IMathLink ml)
		{
			impl = ml;
		}

		public override void Close()
		{
			impl.Close();
		}

		public override void Connect()
		{
			impl.Connect();
		}

		public override void NewPacket()
		{
			impl.NewPacket();
		}

		public override void EndPacket()
		{
			impl.EndPacket();
		}

		public override bool ClearError()
		{
			return impl.ClearError();
		}

		public override string GetFunction(out int argCount)
		{
			return impl.GetFunction(out argCount);
		}

		public override void Flush()
		{
			impl.Flush();
		}

		public override void PutNext(ExpressionType type)
		{
			impl.PutNext(type);
		}

		public override int GetArgCount()
		{
			return impl.GetArgCount();
		}

		public override void PutArgCount(int argCount)
		{
			impl.PutArgCount(argCount);
		}

		public override void PutSize(int size)
		{
			impl.PutSize(size);
		}

		public override void PutData(byte[] data)
		{
			impl.PutData(data);
		}

		public override byte[] GetData(int numRequested)
		{
			return impl.GetData(numRequested);
		}

		public override int BytesToPut()
		{
			return impl.BytesToPut();
		}

		public override int BytesToGet()
		{
			return impl.BytesToGet();
		}

		public override string GetString()
		{
			return impl.GetString();
		}

		public override string GetSymbol()
		{
			return impl.GetSymbol();
		}

		public override byte[] GetByteString(int missing)
		{
			return impl.GetByteString(missing);
		}

		public override void PutSymbol(string s)
		{
			impl.PutSymbol(s);
		}

		public override int GetInteger()
		{
			return impl.GetInteger();
		}

		public override void Put(int i)
		{
			impl.Put(i);
		}

		public override double GetDouble()
		{
			return impl.GetDouble();
		}

		public override void Put(double d)
		{
			impl.Put(d);
		}

		public override void TransferExpression(IMathLink source)
		{
			impl.TransferExpression(source);
		}

		public override void TransferToEndOfLoopbackLink(ILoopbackLink source)
		{
			impl.TransferToEndOfLoopbackLink(source);
		}

		public override void PutMessage(MathLinkMessage msg)
		{
			impl.PutMessage(msg);
		}

		public override ILinkMark CreateMark()
		{
			return impl.CreateMark();
		}

		public override void SeekMark(ILinkMark mark)
		{
			impl.SeekMark(mark);
		}

		public override void DestroyMark(ILinkMark mark)
		{
			impl.DestroyMark(mark);
		}

		public override object GetComplex()
		{
			return impl.GetComplex();
		}

		public override void DeviceInformation(int selector, IntPtr buf, ref int len)
		{
			impl.DeviceInformation(selector, buf, ref len);
		}

		public override ExpressionType GetNextExpressionType()
		{
			ExpressionType expressionType = impl.GetNextExpressionType();
			if (expressionType == ExpressionType.Symbol && isObject())
			{
				expressionType = ExpressionType.Object;
			}
			return expressionType;
		}

		public override ExpressionType GetExpressionType()
		{
			ExpressionType expressionType = impl.GetExpressionType();
			if (expressionType == ExpressionType.Symbol && isObject())
			{
				expressionType = ExpressionType.Object;
			}
			return expressionType;
		}

		public override PacketType NextPacket()
		{
			if (!linkConnected)
			{
				Connect();
				linkConnected = true;
			}
			ILinkMark mark = CreateMark();
			try
			{
				return impl.NextPacket();
			}
			catch (MathLinkException ex)
			{
				if (ex.ErrCode == 23)
				{
					ClearError();
					SeekMark(mark);
					int argCount;
					string function = GetFunction(out argCount);
					if (function == "ExpressionPacket")
					{
						return PacketType.Expression;
					}
					if (function == "BoxData")
					{
						SeekMark(mark);
						return PacketType.Expression;
					}
					SeekMark(mark);
					return PacketType.FrontEnd;
				}
				throw ex;
			}
			finally
			{
				DestroyMark(mark);
			}
		}

		public override Array GetArray(Type leafType, int depth, out string[] heads)
		{
			if (leafType == ComplexType || Utils.IsTrulyPrimitive(leafType))
			{
				return impl.GetArray(leafType, depth, out heads);
			}
			return base.GetArray(leafType, depth, out heads);
		}

		public override object GetObject()
		{
			return base.GetObject();
		}

		public override void Put(object obj)
		{
			base.Put(obj);
		}

		protected override void putString(string s)
		{
			impl.Put(s);
		}

		protected override void putArray(Array obj, string[] heads)
		{
			Type elementType = obj.GetType().GetElementType();
			if (Utils.IsTrulyPrimitive(elementType))
			{
				impl.Put(obj, heads);
			}
			else
			{
				putArrayPiecemeal(obj, heads, 0);
			}
		}

		protected override void putComplex(object obj)
		{
			impl.Put(obj);
		}

		private void interruptDetector(MathLinkMessage msg)
		{
			WasInterrupted = msg == MathLinkMessage.Abort || msg == MathLinkMessage.Interrupt;
		}
	}
}
