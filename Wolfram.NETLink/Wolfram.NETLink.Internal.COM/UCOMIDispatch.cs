using System;
using System.Runtime.InteropServices;

namespace Wolfram.NETLink.Internal.COM
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("00020400-0000-0000-C000-000000000046")]
	internal interface UCOMIDispatch
	{
		void GetTypeInfoCount(out int pctinfo);

		void GetTypeInfo([In] int iTInfo, [In] int lcid, out UCOMITypeInfo typeInfo);

		void GetIDsOfNames([In] ref Guid riid, [In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr)] string[] rgszNames, [In] int cNames, [In] int lcid, out int rgDispId);

		void Invoke();
	}
}
