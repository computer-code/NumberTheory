using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Wolfram.NETLink
{
	public class Reader
	{
		private const int sleepInterval = 2;

		private const int BLOCKING = 1;

		private const int MODAL = 2;

		private const int SHARING_ALLOW_COMPUTATIONS = 3;

		private const int SHARING_DISALLOW_COMPUTATIONS = 4;

		private static IKernelLink ml;

		private static bool quitWhenLinkEnds = true;

		private static bool linkHasDied = false;

		private static Thread readerThread;

		private static bool isModal = false;

		private static bool isSharing = false;

		private static bool allowUIComps = true;

		private static bool isInNextPacket = false;

		private static YieldFunction yieldFunctionWithEvents = yielderWithEvents;

		private static YieldFunction yieldFunctionWithoutEvents = yielderWithoutEvents;

		internal static bool allowUIComputations
		{
			get
			{
				return allowUIComps;
			}
			set
			{
				allowUIComps = value;
			}
		}

		internal static bool isInsideNextPacket
		{
			get
			{
				return isInNextPacket;
			}
			set
			{
				isInNextPacket = value;
			}
		}

		internal static bool isInModalState
		{
			get
			{
				return isModal;
			}
			set
			{
				isModal = value;
				if (isModal)
				{
					ml.Yield += yieldFunctionWithoutEvents;
					ml.Yield -= yieldFunctionWithEvents;
				}
				else
				{
					ml.Yield += yieldFunctionWithEvents;
					ml.Yield -= yieldFunctionWithoutEvents;
				}
			}
		}

		protected Reader()
		{
		}

		public static Thread StartReader(IKernelLink link, bool dieWhenLinkEnds)
		{
			ml = link;
			quitWhenLinkEnds = dieWhenLinkEnds;
			linkHasDied = false;
			StdLink.Link = ml;
			StdLink.HasReader = true;
			ml.MessageArrived += terminateMsgHandler;
			readerThread = new Thread(Run);
			readerThread.Name = ".NET/Link Reader";
			readerThread.ApartmentState = ApartmentState.STA;
			readerThread.Start();
			return readerThread;
		}

		public static void StopReader()
		{
			readerThread.Abort();
			StdLink.HasReader = false;
		}

		public static void Run()
		{
			Application.ThreadException += onThreadException;
			ml.Yield += yieldFunctionWithEvents;
			try
			{
				while (true)
				{
					if (isModal || isSharing)
					{
						Application.DoEvents();
						if (!ml.Ready)
						{
							Thread.Sleep(2);
						}
						lock (ml)
						{
							try
							{
								if (ml.Error != 0)
								{
									throw new MathLinkException(ml.Error, ml.ErrorMessage);
								}
								if (ml.Ready)
								{
									PacketType pkt = ml.NextPacket();
									ml.HandlePacket(pkt);
									ml.NewPacket();
								}
							}
							catch (MathLinkException ex)
							{
								if (ex.ErrCode == 11 || !ml.ClearError())
								{
									return;
								}
								ml.NewPacket();
							}
						}
						continue;
					}
					lock (ml)
					{
						try
						{
							PacketType pkt2 = ml.NextPacket();
							ml.HandlePacket(pkt2);
							ml.NewPacket();
						}
						catch (MathLinkException ex2)
						{
							if (ex2.ErrCode == 11 || !ml.ClearError())
							{
								return;
							}
							ml.NewPacket();
						}
					}
				}
			}
			finally
			{
				if (quitWhenLinkEnds)
				{
					ml.Close();
					ml = null;
					Application.Exit();
				}
			}
		}

		internal static void shareKernel(bool entering)
		{
			isSharing = entering;
			if (entering)
			{
				allowUIComps = true;
				ml.Yield -= yieldFunctionWithEvents;
				ml.Yield += yieldFunctionWithoutEvents;
			}
			else
			{
				ml.Yield -= yieldFunctionWithoutEvents;
				ml.Yield += yieldFunctionWithEvents;
			}
		}

		internal static bool yielderWithEvents()
		{
			if (Utils.IsWindows)
			{
				isInsideNextPacket = true;
				Application.DoEvents();
				isInsideNextPacket = false;
			}
			return false;
		}

		internal static bool yielderWithoutEvents()
		{
			return false;
		}

		public static void terminateMsgHandler(MathLinkMessage msg)
		{
			if (msg == MathLinkMessage.Terminate)
			{
				readerThread.Abort();
			}
		}

		public static void onThreadException(object sender, ThreadExceptionEventArgs t)
		{
			if (!Utils.IsWindows)
			{
				return;
			}
			if (t.Exception is MathematicaNotReadyException)
			{
				MessageBeep(0u);
				return;
			}
			if (!linkHasDied)
			{
				MessageBox.Show("An unhandled exception has occurred:\n" + t.Exception.ToString() + ".\n\n.NET/Link will attempt to continue.", ".NET/Link", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			if (t.Exception is MathLinkException)
			{
				int errCode = ((MathLinkException)t.Exception).ErrCode;
				if (errCode == 11 || errCode == 1)
				{
					linkHasDied = true;
				}
			}
		}

		[DllImport("user32.dll")]
		private static extern int MessageBeep(uint n);
	}
}
