using System;

namespace Wolfram.NETLink.Internal.COM
{
	internal class COMObjectWrapper
	{
		internal object wrappedObject;

		internal Type type;

		internal COMObjectWrapper(object obj, Type t)
		{
			wrappedObject = obj;
			type = t;
		}
	}
}
