using System;

namespace Wolfram.NETLink.Internal
{
	internal class NativeLoopbackLink : NativeLink, ILoopbackLink, IMathLink
	{
		public NativeLoopbackLink()
		{
			int err;
			lock (NativeLink.envLock)
			{
				link = NativeLink.api.extMLLoopbackOpen(NativeLink.env, out err);
			}
			if (link == IntPtr.Zero)
			{
				throw new MathLinkException(1012, NativeLink.api.extMLErrorString(NativeLink.env, err));
			}
		}
	}
}
