using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Wolfram.NETLink.Internal;

namespace Wolfram.NETLink
{
	public abstract class KernelLinkImpl : MathLinkImpl, IKernelLink, IMathLink
	{
		internal const string PACKAGE_INTERNAL_CONTEXT = "NETLink`Package`";

		private bool isPackageLoaded;

		private string graphicsFormat = "Automatic";

		private bool useFrontEnd = true;

		private bool useStdForm = true;

		private Exception lastError;

		private Exception lastExceptionDuringCallPacketHandling;

		private bool lastPktWasMsg;

		private bool isManual;

		private ObjectHandler objectHandler;

		private CallPacketHandler callPktHandler;

		private StringBuilder accumulatingPS;

		private int lastMessage;

		private object msgLock = new object();

		private static YieldFunction bailoutYieldFunction = bailoutYielder;

		public virtual bool UseFrontEnd
		{
			get
			{
				return useFrontEnd;
			}
			set
			{
				useFrontEnd = value;
			}
		}

		public virtual string GraphicsFormat
		{
			get
			{
				return graphicsFormat;
			}
			set
			{
				switch (value)
				{
				case "GIF":
				case "gif":
					graphicsFormat = "GIF";
					break;
				case "JPEG":
				case "jpeg":
					graphicsFormat = "JPEG";
					break;
				case "Metafile":
				case "metafile":
				case "METAFILE":
					graphicsFormat = "Metafile";
					break;
				default:
					graphicsFormat = "Automatic";
					break;
				}
			}
		}

		public virtual bool TypesetStandardForm
		{
			get
			{
				return useStdForm;
			}
			set
			{
				useStdForm = value;
			}
		}

		public virtual Exception LastError
		{
			get
			{
				if (Error == 0)
				{
					return lastError;
				}
				return new MathLinkException(Error, ErrorMessage);
			}
		}

		public virtual bool WasInterrupted
		{
			get
			{
				lock (msgLock)
				{
					return lastMessage == 2;
				}
			}
			set
			{
				lock (msgLock)
				{
					lastMessage = (value ? 2 : 0);
				}
			}
		}

		internal Exception LastExceptionDuringCallPacketHandling
		{
			get
			{
				return lastExceptionDuringCallPacketHandling;
			}
			set
			{
				lastExceptionDuringCallPacketHandling = value;
			}
		}

		internal bool IsManual
		{
			get
			{
				return isManual;
			}
			set
			{
				if (value && !isManual)
				{
					try
					{
						PutFunction("NETLink`Package`prepareForManualReturn", 1);
						PutSymbol("$CurrentLink");
						Flush();
					}
					catch (MathLinkException)
					{
						ClearError();
					}
				}
				isManual = value;
			}
		}

		public virtual event PacketHandler PacketArrived;

		public KernelLinkImpl()
		{
			objectHandler = new ObjectHandler();
			callPktHandler = new CallPacketHandler(objectHandler);
		}

		public virtual void Evaluate(string s)
		{
			PutFunction("EvaluatePacket", 1);
			PutFunction("ToExpression", 1);
			Put(s);
			EndPacket();
			Flush();
		}

		public virtual void Evaluate(Expr e)
		{
			PutFunction("EvaluatePacket", 1);
			Put(e);
			EndPacket();
			Flush();
		}

		public virtual string EvaluateToOutputForm(string s, int pageWidth)
		{
			return evalToString(s, pageWidth, "OutputForm");
		}

		public virtual string EvaluateToOutputForm(Expr e, int pageWidth)
		{
			return evalToString(e, pageWidth, "OutputForm");
		}

		public virtual string EvaluateToInputForm(string s, int pageWidth)
		{
			return evalToString(s, pageWidth, "InputForm");
		}

		public virtual string EvaluateToInputForm(Expr e, int pageWidth)
		{
			return evalToString(e, pageWidth, "InputForm");
		}

		public virtual Image EvaluateToTypeset(string s, int pageWidth)
		{
			return evalToImage(s, pageWidth, -1, isTypeset: true);
		}

		public virtual Image EvaluateToTypeset(Expr e, int pageWidth)
		{
			return evalToImage(e, pageWidth, -1, isTypeset: true);
		}

		public virtual Image EvaluateToImage(string s, int width, int height)
		{
			return evalToImage(s, width, height, isTypeset: false);
		}

		public virtual Image EvaluateToImage(Expr e, int width, int height)
		{
			return evalToImage(e, width, height, isTypeset: false);
		}

		public virtual PacketType WaitForAnswer()
		{
			accumulatingPS = null;
			PacketType packetType;
			while (true)
			{
				packetType = NextPacket();
				if (OnPacketArrived(packetType))
				{
					HandlePacket(packetType);
				}
				if (packetType == PacketType.Return || packetType == PacketType.InputName || packetType == PacketType.ReturnText || packetType == PacketType.ReturnExpression)
				{
					break;
				}
				NewPacket();
			}
			return packetType;
		}

		public virtual void WaitAndDiscardAnswer()
		{
			PacketType packetType = WaitForAnswer();
			NewPacket();
			while (packetType != PacketType.Return && packetType != PacketType.InputName)
			{
				packetType = WaitForAnswer();
				NewPacket();
			}
		}

		public virtual void HandlePacket(PacketType pkt)
		{
			switch (pkt)
			{
			case PacketType.Call:
			{
				ExpressionType expressionType = GetExpressionType();
				if (expressionType == ExpressionType.Integer)
				{
					callPktHandler.handleCallPacket(this);
					break;
				}
				IMathLink fEServerLink2 = callPktHandler.FEServerLink;
				if (fEServerLink2 != null)
				{
					fEServerLink2.PutFunction("CallPacket", 1);
					fEServerLink2.TransferExpression(this);
					TransferExpression(fEServerLink2);
				}
				break;
			}
			case PacketType.Display:
			case PacketType.DisplayEnd:
			{
				IMathLink fEServerLink4 = callPktHandler.FEServerLink;
				if (fEServerLink4 != null)
				{
					if (accumulatingPS == null)
					{
						accumulatingPS = new StringBuilder(34000);
					}
					accumulatingPS.Append(GetString());
					if (pkt == PacketType.DisplayEnd)
					{
						fEServerLink4.PutFunction("FrontEnd`FrontEndExecute", 1);
						fEServerLink4.PutFunction("FrontEnd`NotebookWrite", 2);
						fEServerLink4.PutFunction("FrontEnd`SelectedNotebook", 0);
						fEServerLink4.PutFunction("Cell", 2);
						fEServerLink4.PutFunction("GraphicsData", 2);
						fEServerLink4.Put("PostScript");
						fEServerLink4.Put(accumulatingPS.ToString());
						fEServerLink4.Put("Graphics");
						fEServerLink4.Flush();
						accumulatingPS = null;
					}
				}
				break;
			}
			case PacketType.Input:
			case PacketType.InputString:
			{
				IMathLink fEServerLink5 = callPktHandler.FEServerLink;
				if (fEServerLink5 != null)
				{
					fEServerLink5.PutFunction((pkt == PacketType.InputString) ? "InputStringPacket" : "InputPacket", 1);
					fEServerLink5.Put(GetString());
					fEServerLink5.Flush();
					NewPacket();
					Put(fEServerLink5.GetString());
					Flush();
				}
				break;
			}
			case PacketType.Text:
			case PacketType.Expression:
			{
				IMathLink fEServerLink3 = callPktHandler.FEServerLink;
				if (fEServerLink3 != null)
				{
					fEServerLink3.PutFunction("FrontEnd`FrontEndExecute", 1);
					fEServerLink3.PutFunction("FrontEnd`NotebookWrite", 2);
					fEServerLink3.PutFunction("FrontEnd`SelectedNotebook", 0);
					fEServerLink3.PutFunction("Cell", 2);
					fEServerLink3.TransferExpression(this);
					fEServerLink3.Put(lastPktWasMsg ? "Message" : "Print");
					fEServerLink3.Flush();
				}
				else if (pkt == PacketType.Expression)
				{
					GetFunction(out var _);
				}
				break;
			}
			case PacketType.FrontEnd:
			{
				IMathLink fEServerLink = callPktHandler.FEServerLink;
				if (fEServerLink != null)
				{
					ILinkMark mark = CreateMark();
					try
					{
						int argCount;
						string function = GetFunction(out argCount);
						if (function != "FrontEnd`FrontEndExecute")
						{
							fEServerLink.PutFunction("FrontEnd`FrontEndExecute", 1);
						}
					}
					finally
					{
						SeekMark(mark);
						DestroyMark(mark);
					}
					fEServerLink.TransferExpression(this);
					fEServerLink.Flush();
					do
					{
						Thread.Sleep(50);
					}
					while (!fEServerLink.Ready && !Ready);
					if (fEServerLink.Ready)
					{
						TransferExpression(fEServerLink);
						Flush();
					}
				}
				else
				{
					GetFunction(out var _);
				}
				break;
			}
			}
			lastPktWasMsg = pkt == PacketType.Message;
		}

		public virtual void PutReference(object obj)
		{
			PutReference(obj, null);
		}

		public virtual void PutReference(object obj, Type upCastCls)
		{
			if (obj == null)
			{
				PutSymbol("Null");
			}
			else
			{
				objectHandler.putReference(this, obj, upCastCls);
			}
		}

		public virtual void EnableObjectReferences()
		{
			Evaluate("Needs[\"NETLink`\"]");
			WaitAndDiscardAnswer();
			Evaluate("InstallNET[\"" + Name + "\"]");
			Flush();
			Install.install(this);
			WaitAndDiscardAnswer();
		}

		public virtual void InterruptEvaluation()
		{
			try
			{
				PutMessage(MathLinkMessage.Interrupt);
			}
			catch (MathLinkException)
			{
			}
		}

		public virtual void AbortEvaluation()
		{
			try
			{
				PutMessage(MathLinkMessage.Abort);
			}
			catch (MathLinkException)
			{
			}
		}

		public virtual void AbandonEvaluation()
		{
			Yield += bailoutYieldFunction;
		}

		public virtual void TerminateKernel()
		{
			try
			{
				PutMessage(MathLinkMessage.Terminate);
			}
			catch (MathLinkException)
			{
			}
		}

		public static bool bailoutYielder()
		{
			return true;
		}

		public virtual bool OnPacketArrived(PacketType pkt)
		{
			if (this.PacketArrived == null)
			{
				return true;
			}
			bool flag = true;
			Delegate[] invocationList = this.PacketArrived.GetInvocationList();
			if (invocationList.Length > 0)
			{
				object[] args = new object[2]
				{
					this,
					pkt
				};
				ILinkMark mark = CreateMark();
				try
				{
					Delegate[] array = invocationList;
					int num = 0;
					while (true)
					{
						if (num < array.Length)
						{
							Delegate @delegate = array[num];
							try
							{
								flag = (bool)@delegate.DynamicInvoke(args);
							}
							catch (Exception)
							{
							}
							ClearError();
							SeekMark(mark);
							if (flag)
							{
								num++;
								continue;
							}
							break;
						}
						return flag;
					}
					return flag;
				}
				finally
				{
					DestroyMark(mark);
				}
			}
			return flag;
		}

		public virtual void Print(string s)
		{
			try
			{
				PutFunction("EvaluatePacket", 1);
				PutFunction("Print", 1);
				Put(s);
				EndPacket();
				WaitAndDiscardAnswer();
			}
			catch (MathLinkException)
			{
				ClearError();
				NewPacket();
			}
		}

		public virtual void Message(string symtag, params string[] args)
		{
			try
			{
				PutFunction("EvaluatePacket", 1);
				PutFunction("Apply", 2);
				PutFunction("ToExpression", 1);
				Put("Function[Null, Message[#1, ##2], HoldFirst]");
				PutFunction("Join", 2);
				PutFunction("ToHeldExpression", 1);
				Put(symtag);
				PutFunction("Hold", args.Length);
				foreach (string obj in args)
				{
					Put(obj);
				}
				EndPacket();
				WaitAndDiscardAnswer();
			}
			catch (MathLinkException)
			{
				ClearError();
				NewPacket();
			}
		}

		public virtual void BeginManual()
		{
			IsManual = true;
		}

		public void copyStateFrom(KernelLinkImpl other)
		{
			objectHandler = other.objectHandler;
			callPktHandler = other.callPktHandler;
		}

		protected override void putRef(object obj)
		{
			PutReference(obj);
		}

		protected override object getObj()
		{
			object obj = null;
			try
			{
				string symbol = GetSymbol();
				if (symbol == "Null")
				{
					return null;
				}
				if (symbol.StartsWith("NETLink`Objects`NETObject$"))
				{
					obj = objectHandler.lookupObject(symbol);
				}
			}
			catch (Exception)
			{
			}
			if (obj == null)
			{
				throw new MathLinkException(1100);
			}
			return obj;
		}

		protected bool isObject()
		{
			ILinkMark mark = CreateMark();
			try
			{
				getObj();
				return true;
			}
			catch (MathLinkException)
			{
				ClearError();
				return false;
			}
			finally
			{
				SeekMark(mark);
				DestroyMark(mark);
			}
		}

		private string evalToString(object obj, int pageWidth, string format)
		{
			string result = null;
			lastError = null;
			try
			{
				Utils.WriteEvalToStringExpression(this, obj, pageWidth, format);
				WaitForAnswer();
				result = GetStringCRLF();
				return result;
			}
			catch (MathLinkException ex)
			{
				ClearError();
				lastError = ex;
				return result;
			}
			finally
			{
				NewPacket();
			}
		}

		private Image evalToImage(object obj, int width, int height, bool isTypeset)
		{
			lastError = null;
			try
			{
				loadNETLinkPackage();
				if (isTypeset)
				{
					Utils.WriteEvalToTypesetExpression(this, obj, width, (GraphicsFormat == "Metafile") ? "Automatic" : GraphicsFormat, TypesetStandardForm);
				}
				else
				{
					Utils.WriteEvalToImageExpression(this, obj, width, height, GraphicsFormat, 0, UseFrontEnd);
				}
				WaitForAnswer();
			}
			catch (MathLinkException ex)
			{
				ClearError();
				lastError = ex;
				NewPacket();
				return null;
			}
			catch (ApplicationException ex2)
			{
				ApplicationException ex3 = (ApplicationException)(lastError = ex2);
				return null;
			}
			Image result = null;
			try
			{
				if (GetNextExpressionType() == ExpressionType.String)
				{
					byte[] byteString = GetByteString(0);
					result = Image.FromStream(new MemoryStream(byteString));
					return result;
				}
				return result;
			}
			catch (Exception ex4)
			{
				ClearError();
				lastError = ex4;
				return result;
			}
			finally
			{
				NewPacket();
			}
		}

		private void loadNETLinkPackage()
		{
			if (isPackageLoaded)
			{
				return;
			}
			PutFunction("EvaluatePacket", 1);
			PutFunction("Needs", 1);
			Put("NETLink`");
			PacketHandler packetArrived = this.PacketArrived;
			this.PacketArrived = null;
			try
			{
				WaitAndDiscardAnswer();
				string s = EvaluateToInputForm("JLink`Information`$VersionNumber", 0);
				if (double.Parse(s, NumberFormatInfo.InvariantInfo) < 2.1)
				{
					throw new ApplicationException("The J/Link application in your Mathematica installation must be updated to at least version 2.1. See www.wolfram.com/solutions/mathlink/jlink.");
				}
			}
			finally
			{
				this.PacketArrived = packetArrived;
			}
			isPackageLoaded = true;
		}
	}
}
