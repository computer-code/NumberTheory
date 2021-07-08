using System;
using System.Drawing;

namespace Wolfram.NETLink
{
	[CLSCompliant(true)]
	public interface IKernelLink : IMathLink
	{
		bool UseFrontEnd
		{
			get;
			set;
		}

		string GraphicsFormat
		{
			get;
			set;
		}

		bool TypesetStandardForm
		{
			get;
			set;
		}

		Exception LastError
		{
			get;
		}

		bool WasInterrupted
		{
			get;
			set;
		}

		event PacketHandler PacketArrived;

		void Evaluate(string s);

		void Evaluate(Expr e);

		string EvaluateToInputForm(string s, int pageWidth);

		string EvaluateToInputForm(Expr e, int pageWidth);

		string EvaluateToOutputForm(string s, int pageWidth);

		string EvaluateToOutputForm(Expr e, int pageWidth);

		Image EvaluateToImage(string s, int width, int height);

		Image EvaluateToImage(Expr e, int width, int height);

		Image EvaluateToTypeset(string s, int width);

		Image EvaluateToTypeset(Expr e, int width);

		PacketType WaitForAnswer();

		void WaitAndDiscardAnswer();

		void HandlePacket(PacketType pkt);

		bool OnPacketArrived(PacketType pkt);

		new void Put(object obj);

		void PutReference(object obj);

		void PutReference(object obj, Type t);

		new ExpressionType GetNextExpressionType();

		new ExpressionType GetExpressionType();

		new object GetObject();

		new Array GetArray(Type leafType, int depth);

		new Array GetArray(Type leafType, int depth, out string[] heads);

		void EnableObjectReferences();

		void AbortEvaluation();

		void InterruptEvaluation();

		void AbandonEvaluation();

		void TerminateKernel();

		void Print(string s);

		void Message(string symtag, params string[] args);

		void BeginManual();
	}
}
