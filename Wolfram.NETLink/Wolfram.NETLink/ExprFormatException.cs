using System;

namespace Wolfram.NETLink
{
	public class ExprFormatException : ApplicationException
	{
		internal ExprFormatException(string msg)
			: base(msg)
		{
		}
	}
}
