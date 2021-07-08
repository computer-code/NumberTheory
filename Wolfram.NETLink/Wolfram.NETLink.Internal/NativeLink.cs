using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Wolfram.NETLink.Internal
{
	internal class NativeLink : MathLinkImpl
	{
		internal delegate bool YielderCallback(IntPtr a, IntPtr b);

		internal delegate void MessageCallback(IntPtr link, int msg, int ignore);

		private class VersionComparer : IComparer
		{
			public int Compare(object v1, object v2)
			{
				string[] array = ((string)v1).Split('.');
				string[] array2 = ((string)v2).Split('.');
				for (int i = 0; i < Math.Max(array.Length, array2.Length); i++)
				{
					int num = ((array.Length > i) ? int.Parse(array[i]) : 0);
					int num2 = ((array2.Length > i) ? int.Parse(array2[i]) : 0);
					if (num > num2)
					{
						return -1;
					}
					if (num < num2)
					{
						return 1;
					}
				}
				return 0;
			}
		}

		internal const int MLE_LINK_IS_NULL = 1100;

		private const string LINK_NULL_MESSAGE = "Link is not open.";

		private const string CREATE_FAILED_MESSAGE = "Link failed to open.";

		protected static IMathLinkAPIProvider api;

		public static IntPtr env;

		public IntPtr link;

		protected static object envLock;

		private YielderCallback yielder;

		private MessageCallback msgHandler;

		public override int Error
		{
			get
			{
				if (link == IntPtr.Zero)
				{
					throw new MathLinkException(1100, "Link is not open.");
				}
				return api.extMLError(link);
			}
		}

		public override string ErrorMessage
		{
			get
			{
				if (!(link == IntPtr.Zero))
				{
					return api.extMLErrorMessage(link);
				}
				return "Link is not open.";
			}
		}

		public override bool Ready
		{
			get
			{
				if (link == IntPtr.Zero)
				{
					throw new MathLinkException(1100, "Link is not open.");
				}
				return api.extMLReady(link) != 0;
			}
		}

		public override string Name
		{
			get
			{
				if (link == IntPtr.Zero)
				{
					throw new MathLinkException(1100, "Link is not open.");
				}
				return api.extMLName(link);
			}
		}

		public override event YieldFunction Yield;

		public override event MessageHandler MessageArrived;

		static NativeLink()
		{
			if (Utils.IsWindows)
			{
				if (Utils.Is64Bit)
				{
					api = new Win64MathLinkAPIProvider();
				}
				else
				{
					api = new WindowsMathLinkAPIProvider();
				}
			}
			else if (Utils.IsMac)
			{
				api = new MacMathLinkAPIProvider();
			}
			else if (Utils.Is64Bit)
			{
				api = new Unix64MathLinkAPIProvider();
			}
			else
			{
				api = new UnixMathLinkAPIProvider();
			}
			env = api.extMLBegin(IntPtr.Zero);
			envLock = new object();
		}

		internal NativeLink(string cmdLine)
		{
			if (cmdLine == "autolaunch")
			{
				cmdLine = getDefaultLaunchString();
			}
			lock (envLock)
			{
				link = api.extMLOpenString(env, cmdLine + " -linkoptions MLForceYield", out var err);
				if (link == IntPtr.Zero)
				{
					throw new MathLinkException(1012, api.extMLErrorString(env, err));
				}
				establishYieldFunction();
				establishMessageHandler();
			}
		}

		internal NativeLink(string[] argv)
		{
			string[] array = new string[argv.Length + 2];
			Array.Copy(argv, array, argv.Length);
			for (int i = 0; i < argv.Length; i++)
			{
				if (array[i].Length == 0)
				{
					array[i] = "\0";
				}
			}
			array[array.Length - 2] = "-linkoptions";
			array[array.Length - 1] = "MLForceYield";
			lock (envLock)
			{
				link = api.extMLOpenInEnv(env, array.Length, array, out var err);
				if (link == IntPtr.Zero)
				{
					throw new MathLinkException(1012, api.extMLErrorString(env, err));
				}
				establishYieldFunction();
				establishMessageHandler();
			}
		}

		protected internal NativeLink()
		{
		}

		public override void Close()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			lock (envLock)
			{
				api.extMLClose(link);
			}
			link = IntPtr.Zero;
		}

		public override void Connect()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLConnect(link) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override void NewPacket()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			api.extMLNewPacket(link);
		}

		public override PacketType NextPacket()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			int num = api.extMLNextPacket(link);
			if (num == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return (PacketType)num;
		}

		public override void EndPacket()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLEndPacket(link) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override bool ClearError()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			return api.extMLClearError(link) != 0;
		}

		public override void Flush()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLFlush(link) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override string GetFunction(out int argCount)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			switch (api.extMLGetType(link))
			{
			case 0:
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			default:
				throw new MathLinkException(1014);
			case 70:
				api.extMLGetArgCount(link, out argCount);
				return GetSymbol();
			}
		}

		public override ExpressionType GetNextExpressionType()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			int num = api.extMLGetNext(link);
			switch (num)
			{
			case 0:
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			case 35:
			{
				ILinkMark mark2 = CreateMark();
				string symbol = GetSymbol();
				if (symbol == "True" || symbol == "False")
				{
					num = 84;
				}
				SeekMark(mark2);
				DestroyMark(mark2);
				break;
			}
			case 70:
			{
				ILinkMark mark = CreateMark();
				api.extMLGetArgCount(link, out var argCount);
				if (argCount == 2 && GetNextExpressionType() == ExpressionType.Symbol && GetSymbol() == "Complex")
				{
					num = 67;
				}
				SeekMark(mark);
				DestroyMark(mark);
				break;
			}
			}
			return (ExpressionType)num;
		}

		public override ExpressionType GetExpressionType()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			int num = api.extMLGetType(link);
			switch (num)
			{
			case 0:
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			case 35:
			{
				ILinkMark mark2 = CreateMark();
				string symbol = GetSymbol();
				if (symbol == "True" || symbol == "False")
				{
					num = 84;
				}
				SeekMark(mark2);
				DestroyMark(mark2);
				break;
			}
			case 70:
			{
				ILinkMark mark = CreateMark();
				api.extMLGetArgCount(link, out var argCount);
				if (argCount == 2 && GetNextExpressionType() == ExpressionType.Symbol && GetSymbol() == "Complex")
				{
					num = 67;
				}
				SeekMark(mark);
				DestroyMark(mark);
				break;
			}
			}
			return (ExpressionType)num;
		}

		public override void PutNext(ExpressionType type)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			switch (type)
			{
			case ExpressionType.Object:
			case ExpressionType.Boolean:
				type = ExpressionType.Symbol;
				break;
			case ExpressionType.Complex:
				type = ExpressionType.Function;
				break;
			}
			if (api.extMLPutNext(link, (int)type) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override int GetArgCount()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLGetArgCount(link, out var argCount) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return argCount;
		}

		public override void PutArgCount(int argCount)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLPutArgCount(link, argCount) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override void PutSize(int n)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLPutSize(link, n) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override void PutData(byte[] data)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLPutData(link, data, data.Length) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override byte[] GetData(int numRequested)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			byte[] array = new byte[numRequested];
			GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			try
			{
				if (api.extMLGetData(link, Marshal.UnsafeAddrOfPinnedArrayElement(array, 0), numRequested, out var num) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				if (num < numRequested)
				{
					byte[] array2 = new byte[num];
					Array.Copy(array, array2, num);
					return array2;
				}
				return array;
			}
			finally
			{
				gCHandle.Free();
			}
		}

		public override int BytesToPut()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLBytesToPut(link, out var num) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return num;
		}

		public override int BytesToGet()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLBytesToGet(link, out var num) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return num;
		}

		public unsafe override string GetString()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLGetUnicodeString(link, out var strAddress, out var len) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			string result;
			if (len == 0)
			{
				result = string.Empty;
			}
			else
			{
				char* value = (char*)strAddress.ToPointer();
				result = new string(value, 0, len);
			}
			api.extMLDisownUnicodeString(link, strAddress, len);
			return result;
		}

		public unsafe override string GetSymbol()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLGetUnicodeSymbol(link, out var strAddress, out var len) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			string result;
			if (len == 0)
			{
				result = string.Empty;
			}
			else
			{
				char* value = (char*)strAddress.ToPointer();
				result = new string(value, 0, len);
			}
			api.extMLDisownUnicodeSymbol(link, strAddress, len);
			return result;
		}

		public override void PutSymbol(string s)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLPutUnicodeSymbol(link, s, s.Length) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public unsafe override byte[] GetByteString(int missing)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLGetByteString(link, out var strAddress, out var len, missing) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			byte[] array = new byte[len];
			byte* ptr = (byte*)strAddress.ToPointer();
			for (int i = 0; i < len; i++)
			{
				array[i] = *(ptr++);
			}
			api.extMLDisownByteString(link, strAddress, len);
			return array;
		}

		public override int GetInteger()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLGetInteger(link, out var i) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return i;
		}

		public override void Put(int i)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLPutInteger(link, i) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override double GetDouble()
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLGetDouble(link, out var d) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return d;
		}

		public override void Put(double d)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			if (api.extMLPutDouble(link, d) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		public override void TransferExpression(IMathLink source)
		{
			if (source is NativeLink)
			{
				NativeLink nativeLink = (NativeLink)source;
				if (link == IntPtr.Zero || nativeLink.link == IntPtr.Zero)
				{
					throw new MathLinkException(1100, "Link is not open.");
				}
				if (api.extMLTransferExpression(link, nativeLink.link) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
			}
			else if (source is WrappedKernelLink)
			{
				TransferExpression(((WrappedKernelLink)source).GetMathLink());
			}
			else
			{
				Put(source.GetExpr());
			}
			if (source.Error != 0)
			{
				throw new MathLinkException(source.Error, source.ErrorMessage);
			}
		}

		public override void TransferToEndOfLoopbackLink(ILoopbackLink source)
		{
			if (source is NativeLoopbackLink)
			{
				NativeLoopbackLink nativeLoopbackLink = (NativeLoopbackLink)source;
				if (link == IntPtr.Zero || nativeLoopbackLink.link == IntPtr.Zero)
				{
					throw new MathLinkException(1100, "Link is not open.");
				}
				if (api.extMLTransferToEndOfLoopbackLink(link, nativeLoopbackLink.link) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
			}
			else
			{
				while (source.Ready)
				{
					Put(source.GetExpr());
				}
			}
			if (source.Error != 0)
			{
				throw new MathLinkException(source.Error, source.ErrorMessage);
			}
		}

		public override void PutMessage(MathLinkMessage msg)
		{
			api.extMLPutMessage(link, (int)msg);
		}

		public override ILinkMark CreateMark()
		{
			IntPtr intPtr = api.extMLCreateMark(link);
			if (intPtr == IntPtr.Zero)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
			return new NativeMark(this, intPtr);
		}

		public override void SeekMark(ILinkMark mark)
		{
			api.extMLSeekMark(link, mark.Mark, 0);
		}

		public override void DestroyMark(ILinkMark mark)
		{
			api.extMLDestroyMark(link, mark.Mark);
		}

		public unsafe override Array GetArray(Type leafType, int depth, out string[] heads)
		{
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1100, "Link is not open.");
			}
			int[] array = new int[depth];
			IntPtr dataAddress;
			IntPtr dimsAddress;
			IntPtr headsAddress;
			int depth2;
			Array array2;
			switch (Type.GetTypeCode(leafType))
			{
			case TypeCode.Int32:
				if (api.extMLGetIntegerArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					int* ptr13 = (int*)dataAddress.ToPointer();
					int* ptr14 = (int*)dimsAddress.ToPointer();
					for (int num45 = 0; num45 < depth; num45++)
					{
						array[num45] = ptr14[num45];
					}
					switch (depth)
					{
					case 1:
					{
						int num54 = array[0];
						int[] array16 = new int[num54];
						for (int num55 = 0; num55 < num54; num55++)
						{
							int num56 = num55;
							int* num57 = ptr13;
							ptr13 = num57 + 1;
							array16[num56] = *num57;
						}
						array2 = array16;
						break;
					}
					case 2:
					{
						int num47 = array[0];
						int num48 = array[1];
						int[,] array15 = new int[num47, num48];
						for (int num49 = 0; num49 < num47; num49++)
						{
							for (int num50 = 0; num50 < num48; num50++)
							{
								int num51 = num49;
								int num52 = num50;
								int* num53 = ptr13;
								ptr13 = num53 + 1;
								array15[num51, num52] = *num53;
							}
						}
						array2 = array15;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(int), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices5 = new int[depth];
						do
						{
							Array array14 = array2;
							int* num46 = ptr13;
							ptr13 = num46 + 1;
							array14.SetValue(*num46, indices5);
						}
						while (Utils.nextIndex(indices5, array));
						break;
					}
					}
					sbyte** ptr15 = (sbyte**)headsAddress.ToPointer();
					for (int num58 = 0; num58 < depth; num58++)
					{
						headsHolder[num58] = new string(ptr15[num58]);
					}
				}
				finally
				{
					api.extMLDisownIntegerArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.Byte:
				if (api.extMLGetByteArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					byte* ptr4 = (byte*)dataAddress.ToPointer();
					int* ptr5 = (int*)dimsAddress.ToPointer();
					for (int n = 0; n < depth; n++)
					{
						array[n] = ptr5[n];
					}
					switch (depth)
					{
					case 1:
					{
						int num14 = array[0];
						byte[] array7 = new byte[num14];
						for (int num15 = 0; num15 < num14; num15++)
						{
							array7[num15] = *(ptr4++);
						}
						array2 = array7;
						break;
					}
					case 2:
					{
						int num10 = array[0];
						int num11 = array[1];
						byte[,] array6 = new byte[num10, num11];
						for (int num12 = 0; num12 < num10; num12++)
						{
							for (int num13 = 0; num13 < num11; num13++)
							{
								array6[num12, num13] = *(ptr4++);
							}
						}
						array2 = array6;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(byte), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices2 = new int[depth];
						do
						{
							array2.SetValue(*(ptr4++), indices2);
						}
						while (Utils.nextIndex(indices2, array));
						break;
					}
					}
					sbyte** ptr6 = (sbyte**)headsAddress.ToPointer();
					for (int num16 = 0; num16 < depth; num16++)
					{
						headsHolder[num16] = new string(ptr6[num16]);
					}
				}
				finally
				{
					api.extMLDisownByteArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.SByte:
				if (api.extMLGetByteArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					sbyte* ptr16 = (sbyte*)dataAddress.ToPointer();
					int* ptr17 = (int*)dimsAddress.ToPointer();
					for (int num59 = 0; num59 < depth; num59++)
					{
						array[num59] = ptr17[num59];
					}
					switch (depth)
					{
					case 1:
					{
						int num64 = array[0];
						sbyte[] array18 = new sbyte[num64];
						for (int num65 = 0; num65 < num64; num65++)
						{
							array18[num65] = *(ptr16++);
						}
						array2 = array18;
						break;
					}
					case 2:
					{
						int num60 = array[0];
						int num61 = array[1];
						sbyte[,] array17 = new sbyte[num60, num61];
						for (int num62 = 0; num62 < num60; num62++)
						{
							for (int num63 = 0; num63 < num61; num63++)
							{
								array17[num62, num63] = *(ptr16++);
							}
						}
						array2 = array17;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(sbyte), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices6 = new int[depth];
						do
						{
							array2.SetValue(*(ptr16++), indices6);
						}
						while (Utils.nextIndex(indices6, array));
						break;
					}
					}
					sbyte** ptr18 = (sbyte**)headsAddress.ToPointer();
					for (int num66 = 0; num66 < depth; num66++)
					{
						headsHolder[num66] = new string(ptr18[num66]);
					}
				}
				finally
				{
					api.extMLDisownByteArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.Char:
				if (api.extMLGetShortIntegerArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					char* ptr22 = (char*)dataAddress.ToPointer();
					int* ptr23 = (int*)dimsAddress.ToPointer();
					for (int num81 = 0; num81 < depth; num81++)
					{
						array[num81] = ptr23[num81];
					}
					switch (depth)
					{
					case 1:
					{
						int num90 = array[0];
						char[] array24 = new char[num90];
						for (int num91 = 0; num91 < num90; num91++)
						{
							int num92 = num91;
							char* num93 = ptr22;
							ptr22 = num93 + 1;
							array24[num92] = *num93;
						}
						array2 = array24;
						break;
					}
					case 2:
					{
						int num83 = array[0];
						int num84 = array[1];
						char[,] array23 = new char[num83, num84];
						for (int num85 = 0; num85 < num83; num85++)
						{
							for (int num86 = 0; num86 < num84; num86++)
							{
								int num87 = num85;
								int num88 = num86;
								char* num89 = ptr22;
								ptr22 = num89 + 1;
								array23[num87, num88] = *num89;
							}
						}
						array2 = array23;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(char), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices8 = new int[depth];
						do
						{
							Array array22 = array2;
							char* num82 = ptr22;
							ptr22 = num82 + 1;
							array22.SetValue(*num82, indices8);
						}
						while (Utils.nextIndex(indices8, array));
						break;
					}
					}
					sbyte** ptr24 = (sbyte**)headsAddress.ToPointer();
					for (int num94 = 0; num94 < depth; num94++)
					{
						headsHolder[num94] = new string(ptr24[num94]);
					}
				}
				finally
				{
					api.extMLDisownShortIntegerArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.Int16:
				if (api.extMLGetShortIntegerArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					short* ptr7 = (short*)dataAddress.ToPointer();
					int* ptr8 = (int*)dimsAddress.ToPointer();
					for (int num17 = 0; num17 < depth; num17++)
					{
						array[num17] = ptr8[num17];
					}
					switch (depth)
					{
					case 1:
					{
						int num26 = array[0];
						short[] array10 = new short[num26];
						for (int num27 = 0; num27 < num26; num27++)
						{
							int num28 = num27;
							short* num29 = ptr7;
							ptr7 = num29 + 1;
							array10[num28] = *num29;
						}
						array2 = array10;
						break;
					}
					case 2:
					{
						int num19 = array[0];
						int num20 = array[1];
						short[,] array9 = new short[num19, num20];
						for (int num21 = 0; num21 < num19; num21++)
						{
							for (int num22 = 0; num22 < num20; num22++)
							{
								int num23 = num21;
								int num24 = num22;
								short* num25 = ptr7;
								ptr7 = num25 + 1;
								array9[num23, num24] = *num25;
							}
						}
						array2 = array9;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(short), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices3 = new int[depth];
						do
						{
							Array array8 = array2;
							short* num18 = ptr7;
							ptr7 = num18 + 1;
							array8.SetValue(*num18, indices3);
						}
						while (Utils.nextIndex(indices3, array));
						break;
					}
					}
					sbyte** ptr9 = (sbyte**)headsAddress.ToPointer();
					for (int num30 = 0; num30 < depth; num30++)
					{
						headsHolder[num30] = new string(ptr9[num30]);
					}
				}
				finally
				{
					api.extMLDisownShortIntegerArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.UInt16:
				if (api.extMLGetShortIntegerArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					ushort* ptr19 = (ushort*)dataAddress.ToPointer();
					int* ptr20 = (int*)dimsAddress.ToPointer();
					for (int num67 = 0; num67 < depth; num67++)
					{
						array[num67] = ptr20[num67];
					}
					switch (depth)
					{
					case 1:
					{
						int num76 = array[0];
						ushort[] array21 = new ushort[num76];
						for (int num77 = 0; num77 < num76; num77++)
						{
							int num78 = num77;
							ushort* num79 = ptr19;
							ptr19 = num79 + 1;
							array21[num78] = *num79;
						}
						array2 = array21;
						break;
					}
					case 2:
					{
						int num69 = array[0];
						int num70 = array[1];
						ushort[,] array20 = new ushort[num69, num70];
						for (int num71 = 0; num71 < num69; num71++)
						{
							for (int num72 = 0; num72 < num70; num72++)
							{
								int num73 = num71;
								int num74 = num72;
								ushort* num75 = ptr19;
								ptr19 = num75 + 1;
								array20[num73, num74] = *num75;
							}
						}
						array2 = array20;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(ushort), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices7 = new int[depth];
						do
						{
							Array array19 = array2;
							ushort* num68 = ptr19;
							ptr19 = num68 + 1;
							array19.SetValue(*num68, indices7);
						}
						while (Utils.nextIndex(indices7, array));
						break;
					}
					}
					sbyte** ptr21 = (sbyte**)headsAddress.ToPointer();
					for (int num80 = 0; num80 < depth; num80++)
					{
						headsHolder[num80] = new string(ptr21[num80]);
					}
				}
				finally
				{
					api.extMLDisownShortIntegerArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.Double:
				if (api.extMLGetDoubleArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					double* ptr10 = (double*)dataAddress.ToPointer();
					int* ptr11 = (int*)dimsAddress.ToPointer();
					for (int num31 = 0; num31 < depth; num31++)
					{
						array[num31] = ptr11[num31];
					}
					switch (depth)
					{
					case 1:
					{
						int num40 = array[0];
						double[] array13 = new double[num40];
						for (int num41 = 0; num41 < num40; num41++)
						{
							int num42 = num41;
							double* num43 = ptr10;
							ptr10 = num43 + 1;
							array13[num42] = *num43;
						}
						array2 = array13;
						break;
					}
					case 2:
					{
						int num33 = array[0];
						int num34 = array[1];
						double[,] array12 = new double[num33, num34];
						for (int num35 = 0; num35 < num33; num35++)
						{
							for (int num36 = 0; num36 < num34; num36++)
							{
								int num37 = num35;
								int num38 = num36;
								double* num39 = ptr10;
								ptr10 = num39 + 1;
								array12[num37, num38] = *num39;
							}
						}
						array2 = array12;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(double), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices4 = new int[depth];
						do
						{
							Array array11 = array2;
							double* num32 = ptr10;
							ptr10 = num32 + 1;
							array11.SetValue(*num32, indices4);
						}
						while (Utils.nextIndex(indices4, array));
						break;
					}
					}
					sbyte** ptr12 = (sbyte**)headsAddress.ToPointer();
					for (int num44 = 0; num44 < depth; num44++)
					{
						headsHolder[num44] = new string(ptr12[num44]);
					}
				}
				finally
				{
					api.extMLDisownDoubleArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			case TypeCode.Single:
				if (api.extMLGetFloatArray(link, out dataAddress, out dimsAddress, out headsAddress, out depth2) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
				try
				{
					if (depth2 != depth)
					{
						throw new MathLinkException(1002);
					}
					float* ptr = (float*)dataAddress.ToPointer();
					int* ptr2 = (int*)dimsAddress.ToPointer();
					for (int i = 0; i < depth; i++)
					{
						array[i] = ptr2[i];
					}
					switch (depth)
					{
					case 1:
					{
						int num7 = array[0];
						float[] array5 = new float[num7];
						for (int l = 0; l < num7; l++)
						{
							int num8 = l;
							float* num9 = ptr;
							ptr = num9 + 1;
							array5[num8] = *num9;
						}
						array2 = array5;
						break;
					}
					case 2:
					{
						int num2 = array[0];
						int num3 = array[1];
						float[,] array4 = new float[num2, num3];
						for (int j = 0; j < num2; j++)
						{
							for (int k = 0; k < num3; k++)
							{
								int num4 = j;
								int num5 = k;
								float* num6 = ptr;
								ptr = num6 + 1;
								array4[num4, num5] = *num6;
							}
						}
						array2 = array4;
						break;
					}
					default:
					{
						array2 = Array.CreateInstance(typeof(float), array);
						if (array2.Length == 0)
						{
							goto end_IL_0036;
						}
						int[] indices = new int[depth];
						do
						{
							Array array3 = array2;
							float* num = ptr;
							ptr = num + 1;
							array3.SetValue(*num, indices);
						}
						while (Utils.nextIndex(indices, array));
						break;
					}
					}
					sbyte** ptr3 = (sbyte**)headsAddress.ToPointer();
					for (int m = 0; m < depth; m++)
					{
						headsHolder[m] = new string(ptr3[m]);
					}
				}
				finally
				{
					api.extMLDisownFloatArray(link, dataAddress, dimsAddress, headsAddress, depth2);
				}
				break;
			default:
				{
					array2 = base.GetArray(leafType, depth, out heads);
					break;
				}
				end_IL_0036:
				break;
			}
			heads = new string[depth];
			Array.Copy(headsHolder, heads, depth);
			return array2;
		}

		public override void DeviceInformation(int selector, IntPtr buf, ref int len)
		{
			if (api.extMLDeviceInformation(link, (uint)selector, buf, ref len) == 0)
			{
				throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
			}
		}

		protected unsafe override void putArray(Array a, string[] heads)
		{
			Type elementType = a.GetType().GetElementType();
			bool isArray = elementType.IsArray;
			if (!Utils.IsTrulyPrimitive(elementType) || elementType == typeof(bool) || isArray)
			{
				putArrayPiecemeal(a, heads, 0);
				return;
			}
			int rank = a.Rank;
			int[] array = new int[rank];
			for (int i = 0; i < rank; i++)
			{
				array[i] = a.GetLength(i);
			}
			if (array[rank - 1] == 0)
			{
				putArrayPiecemeal(a, heads, 0);
				return;
			}
			switch (Type.GetTypeCode(elementType))
			{
			case TypeCode.Byte:
				switch (rank)
				{
				case 1:
					fixed (byte* data21 = (byte[])a)
					{
						api.extMLPutByteArray(link, data21, array, heads, 1);
					}
					break;
				case 2:
					fixed (byte* data23 = (byte[,])a)
					{
						api.extMLPutByteArray(link, data23, array, heads, 2);
					}
					break;
				case 3:
					fixed (byte* data22 = (byte[,,])a)
					{
						api.extMLPutByteArray(link, data22, array, heads, 3);
					}
					break;
				default:
				{
					byte[] array9 = new byte[a.Length];
					int num13 = 0;
					foreach (byte item in a)
					{
						array9[num13++] = item;
					}
					fixed (byte* data20 = array9)
					{
						api.extMLPutByteArray(link, data20, array, heads, rank);
					}
					break;
				}
				}
				break;
			case TypeCode.SByte:
			{
				short[] array7 = new short[a.Length];
				int num11 = 0;
				foreach (sbyte item2 in a)
				{
					array7[num11++] = item2;
				}
				fixed (short* data18 = array7)
				{
					api.extMLPutShortIntegerArray(link, data18, array, heads, rank);
				}
				break;
			}
			case TypeCode.Char:
			{
				int[] array8 = new int[a.Length];
				int num12 = 0;
				foreach (char item3 in a)
				{
					array8[num12++] = item3;
				}
				fixed (int* data19 = array8)
				{
					api.extMLPutIntegerArray(link, data19, array, heads, rank);
				}
				break;
			}
			case TypeCode.UInt16:
			{
				int[] array6 = new int[a.Length];
				int num9 = 0;
				foreach (ushort item4 in a)
				{
					array6[num9++] = item4;
				}
				fixed (int* data17 = array6)
				{
					api.extMLPutIntegerArray(link, data17, array, heads, rank);
				}
				break;
			}
			case TypeCode.Int16:
				switch (rank)
				{
				case 1:
					fixed (short* data14 = (short[])a)
					{
						api.extMLPutShortIntegerArray(link, data14, array, heads, 1);
					}
					break;
				case 2:
					fixed (short* data16 = (short[,])a)
					{
						api.extMLPutShortIntegerArray(link, data16, array, heads, 2);
					}
					break;
				case 3:
					fixed (short* data15 = (short[,,])a)
					{
						api.extMLPutShortIntegerArray(link, data15, array, heads, 3);
					}
					break;
				default:
				{
					short[] array5 = new short[a.Length];
					int num7 = 0;
					foreach (short item5 in a)
					{
						array5[num7++] = item5;
					}
					fixed (short* data13 = array5)
					{
						api.extMLPutShortIntegerArray(link, data13, array, heads, rank);
					}
					break;
				}
				}
				break;
			case TypeCode.Int32:
				switch (rank)
				{
				case 1:
					fixed (int* data10 = (int[])a)
					{
						api.extMLPutIntegerArray(link, data10, array, heads, 1);
					}
					break;
				case 2:
					fixed (int* data12 = (int[,])a)
					{
						api.extMLPutIntegerArray(link, data12, array, heads, 2);
					}
					break;
				case 3:
					fixed (int* data11 = (int[,,])a)
					{
						api.extMLPutIntegerArray(link, data11, array, heads, 3);
					}
					break;
				default:
				{
					int[] array4 = new int[a.Length];
					int num5 = 0;
					foreach (int item6 in a)
					{
						array4[num5++] = item6;
					}
					fixed (int* data9 = array4)
					{
						api.extMLPutIntegerArray(link, data9, array, heads, rank);
					}
					break;
				}
				}
				break;
			case TypeCode.Single:
				switch (rank)
				{
				case 1:
					fixed (float* data6 = (float[])a)
					{
						api.extMLPutFloatArray(link, data6, array, heads, 1);
					}
					break;
				case 2:
					fixed (float* data8 = (float[,])a)
					{
						api.extMLPutFloatArray(link, data8, array, heads, 2);
					}
					break;
				case 3:
					fixed (float* data7 = (float[,,])a)
					{
						api.extMLPutFloatArray(link, data7, array, heads, 3);
					}
					break;
				default:
				{
					float[] array3 = new float[a.Length];
					int num3 = 0;
					foreach (float item7 in a)
					{
						array3[num3++] = item7;
					}
					fixed (float* data5 = array3)
					{
						api.extMLPutFloatArray(link, data5, array, heads, rank);
					}
					break;
				}
				}
				break;
			case TypeCode.Double:
				switch (rank)
				{
				case 1:
					fixed (double* data2 = (double[])a)
					{
						api.extMLPutDoubleArray(link, data2, array, heads, 1);
					}
					break;
				case 2:
					fixed (double* data4 = (double[,])a)
					{
						api.extMLPutDoubleArray(link, data4, array, heads, 2);
					}
					break;
				case 3:
					fixed (double* data3 = (double[,,])a)
					{
						api.extMLPutDoubleArray(link, data3, array, heads, 3);
					}
					break;
				default:
				{
					double[] array2 = new double[a.Length];
					int num = 0;
					foreach (double item8 in a)
					{
						array2[num++] = item8;
					}
					fixed (double* data = array2)
					{
						api.extMLPutDoubleArray(link, data, array, heads, rank);
					}
					break;
				}
				}
				break;
			default:
				putArrayPiecemeal(a, heads, 0);
				break;
			}
		}

		protected override void putString(string s)
		{
			lock (this)
			{
				if (link == IntPtr.Zero)
				{
					throw new MathLinkException(1100, "Link is not open.");
				}
				if (api.extMLPutUnicodeString(link, s, s.Length) == 0)
				{
					throw new MathLinkException(api.extMLError(link), api.extMLErrorMessage(link));
				}
			}
		}

		protected override void putComplex(object obj)
		{
			complexHandler.PutComplex(this, obj);
		}

		internal static bool canUseMathLinkLibrary()
		{
			return true;
		}

		private void establishYieldFunction()
		{
			yielder = yielderCallbackFunction;
			IntPtr yfObject = api.extMLCreateYieldFunction(env, yielder, IntPtr.Zero);
			api.extMLSetYieldFunction(link, yfObject);
		}

		private void establishMessageHandler()
		{
			msgHandler = messageCallbackFunction;
			IntPtr mhObject = api.extMLCreateMessageHandler(env, msgHandler, IntPtr.Zero);
			api.extMLSetMessageHandler(link, mhObject);
		}

		private bool yielderCallbackFunction(IntPtr a, IntPtr b)
		{
			bool flag = false;
			if (Yield != null)
			{
				Delegate[] invocationList = Yield.GetInvocationList();
				if (invocationList.Length > 0)
				{
					Delegate[] array = invocationList;
					foreach (Delegate @delegate in array)
					{
						flag = (bool)@delegate.DynamicInvoke(null);
						if (flag)
						{
							break;
						}
					}
				}
			}
			return flag;
		}

		private void messageCallbackFunction(IntPtr link, int msg, int ignore)
		{
			if (MessageArrived != null)
			{
				MessageArrived((MathLinkMessage)msg);
			}
		}

		private string getDefaultLaunchString()
		{
			try
			{
				string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mathematica\\WolframProductRegistry";
				string value;
				if (File.Exists(path))
				{
					SortedList sortedList = new SortedList(new VersionComparer());
					using (FileStream stream = File.OpenRead(path))
					{
						StreamReader streamReader = new StreamReader(stream);
						for (string text = streamReader.ReadLine(); text != null; text = streamReader.ReadLine())
						{
							if (text.StartsWith("Mathematica "))
							{
								string key = text.Split(' ', '=')[1];
								value = text.Split('=')[1];
								try
								{
									sortedList.Add(key, value);
								}
								catch (Exception)
								{
								}
							}
						}
					}
					foreach (DictionaryEntry item in sortedList)
					{
						value = (string)item.Value;
						if (File.Exists(value))
						{
							value = value.Replace("\\", "/").Replace("Mathematica.exe", "MathKernel.exe");
							return "-linkmode launch -linkname \"" + value + "\"";
						}
					}
				}
				string name = (string)Registry.ClassesRoot.OpenSubKey(".nb").GetValue("");
				value = (string)Registry.ClassesRoot.OpenSubKey(name).OpenSubKey("DefaultIcon").GetValue("");
				value = value.Split(',')[0];
				if (value.StartsWith("\""))
				{
					value = value.Substring(1, value.Length - 2);
				}
				if (File.Exists(value))
				{
					value = value.Replace("\\", "/").Replace("Mathematica.exe", "MathKernel.exe");
					return "-linkmode launch -linkname \"" + value + "\"";
				}
			}
			catch (Exception)
			{
			}
			return "-linkmode launch";
		}
	}
}
