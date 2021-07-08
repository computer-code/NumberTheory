using System;

namespace Wolfram.NETLink
{
	public class MathematicaNotReadyException : ApplicationException
	{
		internal const int KERNEL_NOT_SHARED = 0;

		internal const int FE_HAS_KERNEL_ATTENTION = 0;

		private int type;

		internal MathematicaNotReadyException(int type)
		{
			this.type = type;
		}

		public override string ToString()
		{
			if (type == 0)
			{
				return "Mathematica is not in a state where it is receptive to calls initiated in .NET.\n\nYou must call one of the Mathematica functions DoModal or ShareKernel\nbefore calls from .NET into Mathematica can succeeed.";
			}
			return "Mathematica is not in a state where it is receptive to calls initiated in .NET.\n\nAlthough ShareKernel has been called, the kernel is currently in use by the front end. Calls from .NET into Mathematica cannot occur until the kernel is no longer busy.";
		}
	}
}
