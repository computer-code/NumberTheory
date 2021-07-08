using System.Collections;
using System.Threading;

namespace Wolfram.NETLink
{
	public class StdLink
	{
		private static IKernelLink mainLink;

		private static bool mainLinkHasReader = false;

		private static IKernelLink uiLink;

		private static Hashtable stdLinkHash = new Hashtable(4, 0.75f);

		private static object stdLinkLock = new object();

		private static object uiLock = new object();

		public static IKernelLink Link
		{
			get
			{
				lock (stdLinkLock)
				{
					object currentThread = Thread.CurrentThread;
					Stack stack = (Stack)stdLinkHash[currentThread];
					if (stack != null && stack.Count > 0)
					{
						return (IKernelLink)stack.Peek();
					}
					if (uiLink == null)
					{
						return mainLink;
					}
					if (Reader.isInModalState)
					{
						return mainLink;
					}
					return uiLink;
				}
			}
			set
			{
				mainLink = value;
			}
		}

		internal static IKernelLink UILink
		{
			get
			{
				return uiLink;
			}
			set
			{
				uiLink = value;
			}
		}

		internal static bool HasReader
		{
			get
			{
				return mainLinkHasReader;
			}
			set
			{
				mainLinkHasReader = value;
			}
		}

		private StdLink()
		{
		}

		public static void RequestTransaction()
		{
			if (mainLink == null || !mainLinkHasReader)
			{
				return;
			}
			IKernelLink link = Link;
			if (link == null || link == uiLink)
			{
				return;
			}
			if (Reader.isInsideNextPacket)
			{
				throw new MathematicaNotReadyException(0);
			}
			if (Reader.isInModalState)
			{
				return;
			}
			lock (stdLinkLock)
			{
				object currentThread = Thread.CurrentThread;
				Stack stack = (Stack)stdLinkHash[currentThread];
				if (stack != null && stack.Count > 0)
				{
					return;
				}
			}
			if (Reader.allowUIComputations)
			{
				return;
			}
			throw new MathematicaNotReadyException(0);
		}

		internal static void setup(IKernelLink ml)
		{
			lock (stdLinkLock)
			{
				object currentThread = Thread.CurrentThread;
				Stack stack = (Stack)stdLinkHash[currentThread];
				if (stack == null)
				{
					stack = new Stack();
					stdLinkHash.Add(currentThread, stack);
				}
				stack.Push(ml);
			}
		}

		internal static void remove()
		{
			lock (stdLinkLock)
			{
				object currentThread = Thread.CurrentThread;
				Stack stack = (Stack)stdLinkHash[currentThread];
				stack.Pop();
			}
		}
	}
}
