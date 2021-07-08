using System;
using System.Reflection;

namespace Wolfram.NETLink.Internal
{
	internal class CallNETException : ApplicationException
	{
		private string tag;

		private string typeName;

		private string memberName;

		internal CallNETException(string tag, string typeName, string memberName)
		{
			this.tag = tag;
			this.typeName = typeName;
			this.memberName = memberName;
		}

		internal CallNETException(Exception innerException, string memberName)
			: base("", innerException)
		{
			this.memberName = memberName;
		}

		internal void writeToLink(IKernelLink ml)
		{
			if (base.InnerException == null)
			{
				ml.PutFunction("NETLink`Package`specialException", 3);
				ml.Put(tag);
				ml.Put(memberName);
				ml.Put(typeName);
				return;
			}
			Exception innerException = base.InnerException;
			if (innerException is TargetInvocationException)
			{
				innerException = innerException.InnerException;
			}
			ml.Put(innerException.ToString().Replace("\r\n", "\n"));
		}
	}
}
