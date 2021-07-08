using System;

namespace Wolfram.NETLink
{
	[CLSCompliant(true)]
	public interface IMathLink
	{
		int Error
		{
			get;
		}

		string ErrorMessage
		{
			get;
		}

		bool Ready
		{
			get;
		}

		Type ComplexType
		{
			get;
			set;
		}

		string Name
		{
			get;
		}

		event YieldFunction Yield;

		event MessageHandler MessageArrived;

		void Close();

		void Connect();

		void Connect(long timeoutMillis);

		void NewPacket();

		PacketType NextPacket();

		void EndPacket();

		bool ClearError();

		void Flush();

		ExpressionType GetNextExpressionType();

		ExpressionType GetExpressionType();

		void PutNext(ExpressionType type);

		int GetArgCount();

		void PutArgCount(int argCount);

		void PutSize(int size);

		void PutData(byte[] data);

		byte[] GetData(int numRequested);

		int BytesToPut();

		int BytesToGet();

		string GetString();

		string GetStringCRLF();

		string GetSymbol();

		void PutSymbol(string s);

		byte[] GetByteString(int missing);

		bool GetBoolean();

		int GetInteger();

		double GetDouble();

		decimal GetDecimal();

		void Put(bool b);

		void Put(int i);

		void Put(long i);

		void Put(double d);

		void Put(decimal d);

		void Put(object obj);

		void Put(Array obj, string[] heads);

		bool[] GetBooleanArray();

		byte[] GetByteArray();

		char[] GetCharArray();

		short[] GetInt16Array();

		int[] GetInt32Array();

		long[] GetInt64Array();

		float[] GetSingleArray();

		double[] GetDoubleArray();

		decimal[] GetDecimalArray();

		string[] GetStringArray();

		object[] GetComplexArray();

		Array GetArray(Type leafType, int depth);

		Array GetArray(Type leafType, int depth, out string[] heads);

		string GetFunction(out int argCount);

		void PutFunction(string f, int argCount);

		void PutFunctionAndArgs(string f, params object[] args);

		int CheckFunction(string f);

		void CheckFunctionWithArgCount(string f, int argCount);

		object GetComplex();

		object GetObject();

		void TransferExpression(IMathLink source);

		void TransferToEndOfLoopbackLink(ILoopbackLink source);

		Expr GetExpr();

		Expr PeekExpr();

		void PutMessage(MathLinkMessage msg);

		ILinkMark CreateMark();

		void SeekMark(ILinkMark mark);

		void DestroyMark(ILinkMark mark);

		void DeviceInformation(int selector, IntPtr buffer, ref int bufLen);
	}
}
