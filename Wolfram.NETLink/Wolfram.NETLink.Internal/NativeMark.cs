using System;

namespace Wolfram.NETLink.Internal
{
	internal class NativeMark : ILinkMark
	{
		private IntPtr mark;

		private IMathLink ml;

		public IntPtr Mark => mark;

		internal NativeMark(IMathLink ml, IntPtr mark)
		{
			this.mark = mark;
			this.ml = ml;
		}
	}
}
