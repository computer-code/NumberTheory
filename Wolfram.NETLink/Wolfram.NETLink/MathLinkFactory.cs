using Wolfram.NETLink.Internal;

namespace Wolfram.NETLink
{
	public class MathLinkFactory
	{
		public static IKernelLink CreateKernelLink()
		{
			return new WrappedKernelLink(CreateMathLink());
		}

		public static IKernelLink CreateKernelLink(string cmdLine)
		{
			return createKernelLink0(cmdLine, null);
		}

		public static IKernelLink CreateKernelLink(string[] argv)
		{
			return createKernelLink0(null, argv);
		}

		public static IKernelLink CreateKernelLink(IMathLink ml)
		{
			return new WrappedKernelLink(ml);
		}

		private static IKernelLink createKernelLink0(string cmdLine, string[] argv)
		{
			if (cmdLine == null && argv == null)
			{
				throw new MathLinkException(1012, "Null argument to KernelLink constructor");
			}
			bool flag = cmdLine != null;
			if (!flag)
			{
				determineProtocol(argv);
			}
			else
			{
				determineProtocol(cmdLine);
			}
			return new WrappedKernelLink(flag ? CreateMathLink(cmdLine) : CreateMathLink(argv));
		}

		public static IMathLink CreateMathLink()
		{
			return new NativeLink("autolaunch");
		}

		public static IMathLink CreateMathLink(string cmdLine)
		{
			return createMathLink0(cmdLine, null);
		}

		public static IMathLink CreateMathLink(string[] argv)
		{
			return createMathLink0(null, argv);
		}

		private static IMathLink createMathLink0(string cmdLine, string[] argv)
		{
			if (cmdLine == null && argv == null)
			{
				throw new MathLinkException(1012, "Null argument to MathLink constructor");
			}
			bool flag = cmdLine != null;
			if (!flag)
			{
				determineProtocol(argv);
			}
			else
			{
				determineProtocol(cmdLine);
			}
			if (!flag)
			{
				return new NativeLink(argv);
			}
			return new NativeLink(cmdLine);
		}

		public static ILoopbackLink CreateLoopbackLink()
		{
			return new NativeLoopbackLink();
		}

		private static string determineProtocol(string cmdLine)
		{
			return determineProtocol(cmdLine.Split());
		}

		private static string determineProtocol(string[] argv)
		{
			string text = "native";
			bool flag = false;
			foreach (string text2 in argv)
			{
				if (flag)
				{
					text = text2.ToLower();
					break;
				}
				if (string.Compare(text2, "-linkprotocol", ignoreCase: true) == 0)
				{
					flag = true;
				}
			}
			if (!isNative(text))
			{
				return text;
			}
			return "native";
		}

		private static bool isNative(string prot)
		{
			switch (prot)
			{
			default:
				return prot == "";
			case "native":
			case "local":
			case "filemap":
			case "tcpip":
			case "tcp":
			case "pipes":
			case "sharedmemory":
				return true;
			}
		}
	}
}
