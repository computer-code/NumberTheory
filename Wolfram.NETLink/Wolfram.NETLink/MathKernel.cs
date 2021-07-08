using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Wolfram.NETLink
{
	public class MathKernel : Component
	{
		public enum ResultFormatType
		{
			InputForm,
			OutputForm,
			StandardForm,
			TraditionalForm,
			MathML,
			Expr
		}

		private IKernelLink ml;

		private string linkArgs;

		private object input;

		private object result;

		private ArrayList prints = new ArrayList();

		private ArrayList messages = new ArrayList();

		private ArrayList graphics = new ArrayList();

		private string graphicsFormat = "Automatic";

		private int graphicsWidth;

		private int graphicsHeight;

		private int graphicsResolution;

		private ResultFormatType resultFormat = ResultFormatType.OutputForm;

		private int pageWidth;

		private bool captureMessages = true;

		private bool capturePrint = true;

		private bool captureGraphics;

		private bool useFrontEnd = true;

		private bool autoCloseLink = true;

		private volatile bool isConnected;

		private volatile bool isComputing;

		private bool handleEvents = true;

		private bool lastPktWasMsg;

		public IKernelLink Link
		{
			get
			{
				return ml;
			}
			set
			{
				ml = value;
			}
		}

		public string LinkArguments
		{
			get
			{
				return linkArgs;
			}
			set
			{
				linkArgs = value;
			}
		}

		public object Input
		{
			get
			{
				return input;
			}
			set
			{
				if (value == null || value is string || value is Expr)
				{
					input = value;
					return;
				}
				throw new ArgumentException("Input must be a string or Expr.");
			}
		}

		public ResultFormatType ResultFormat
		{
			get
			{
				return resultFormat;
			}
			set
			{
				resultFormat = value;
			}
		}

		public string GraphicsFormat
		{
			get
			{
				return graphicsFormat;
			}
			set
			{
				graphicsFormat = value;
			}
		}

		public int PageWidth
		{
			get
			{
				return pageWidth;
			}
			set
			{
				pageWidth = value;
			}
		}

		public int GraphicsWidth
		{
			get
			{
				return graphicsWidth;
			}
			set
			{
				graphicsWidth = value;
			}
		}

		public int GraphicsHeight
		{
			get
			{
				return graphicsHeight;
			}
			set
			{
				graphicsHeight = value;
			}
		}

		public int GraphicsResolution
		{
			get
			{
				return graphicsResolution;
			}
			set
			{
				graphicsResolution = value;
			}
		}

		public bool CaptureMessages
		{
			get
			{
				return captureMessages;
			}
			set
			{
				captureMessages = value;
			}
		}

		public bool CapturePrint
		{
			get
			{
				return capturePrint;
			}
			set
			{
				capturePrint = value;
			}
		}

		public bool CaptureGraphics
		{
			get
			{
				return captureGraphics;
			}
			set
			{
				captureGraphics = value;
			}
		}

		public bool UseFrontEnd
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

		public bool AutoCloseLink
		{
			get
			{
				return autoCloseLink;
			}
			set
			{
				autoCloseLink = value;
			}
		}

		public bool IsConnected => isConnected;

		public bool IsComputing => isComputing;

		public bool HandleEvents
		{
			get
			{
				return handleEvents;
			}
			set
			{
				handleEvents = value;
			}
		}

		public object Result => result;

		public string[] Messages => (string[])messages.ToArray(typeof(string));

		public string[] PrintOutput => (string[])prints.ToArray(typeof(string));

		public Image[] Graphics => (Image[])graphics.ToArray(typeof(Image));

		public MathKernel()
		{
		}

		public MathKernel(IKernelLink ml)
		{
			this.ml = ml;
		}

		~MathKernel()
		{
			if (ml != null && AutoCloseLink)
			{
				ml.Close();
			}
			ml = null;
		}

		public new void Dispose()
		{
			base.Dispose();
			if (ml != null && AutoCloseLink)
			{
				ml.Close();
			}
			ml = null;
			GC.SuppressFinalize(this);
		}

		public void Connect()
		{
			if (!isConnected)
			{
				if (ml == null)
				{
					ml = ((linkArgs == null) ? MathLinkFactory.CreateKernelLink() : MathLinkFactory.CreateKernelLink(linkArgs));
				}
				ml.Yield += yielder;
				ml.Connect();
				ml.Evaluate("Needs[\"NETLink`\"]");
				PacketType packetType = ml.WaitForAnswer();
				ml.NewPacket();
				if (packetType == PacketType.InputName)
				{
					ml.WaitAndDiscardAnswer();
				}
				ml.PacketArrived += MathKernelPacketHandler;
				isConnected = true;
			}
		}

		public void Compute()
		{
			if (IsComputing)
			{
				throw new InvalidOperationException("The Mathematica kernel is currently busy (you cannot make a reentrant call to MathKernel.Compute()).");
			}
			isComputing = true;
			try
			{
				Clear();
				Connect();
				lock (ml)
				{
					try
					{
						ml.PutFunction("EvaluatePacket", 1);
						ml.PutFunction("NETLink`Package`computeWrapper", 9);
						ml.Put(Input);
						ml.Put(ResultFormat.ToString());
						ml.Put(PageWidth);
						ml.Put(GraphicsFormat);
						ml.Put(GraphicsWidth);
						ml.Put(GraphicsHeight);
						ml.Put(GraphicsResolution);
						ml.Put(UseFrontEnd);
						ml.Put(CaptureGraphics);
						ml.WaitForAnswer();
						switch (ResultFormat)
						{
						case ResultFormatType.Expr:
							result = ml.GetExpr();
							break;
						case ResultFormatType.StandardForm:
						case ResultFormatType.TraditionalForm:
							result = readImage();
							break;
						default:
							result = ml.GetStringCRLF();
							break;
						}
					}
					catch (MathLinkException ex)
					{
						ml.ClearError();
						ml.NewPacket();
						throw ex;
					}
				}
			}
			finally
			{
				isComputing = false;
			}
		}

		public void Compute(string input)
		{
			Input = input;
			Compute();
		}

		public void Abort()
		{
			ml.AbortEvaluation();
		}

		public void Clear()
		{
			result = null;
			messages.Clear();
			prints.Clear();
			graphics.Clear();
		}

		private bool MathKernelPacketHandler(IKernelLink ml, PacketType pkt)
		{
			switch (pkt)
			{
			case PacketType.DisplayEnd:
				graphics.Add(readImage());
				break;
			case PacketType.Text:
				if (lastPktWasMsg && captureMessages)
				{
					messages.Add(ml.GetStringCRLF());
				}
				else if (capturePrint)
				{
					prints.Add(ml.GetStringCRLF());
				}
				break;
			}
			lastPktWasMsg = pkt == PacketType.Message;
			return true;
		}

		private Image readImage()
		{
			byte[] byteString = ml.GetByteString(-1);
			return Image.FromStream(new MemoryStream(byteString));
		}

		private bool yielder()
		{
			if (HandleEvents)
			{
				Application.DoEvents();
			}
			return false;
		}
	}
}
