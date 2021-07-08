namespace Wolfram.NETLink.Internal
{
	internal class OutParamRecord
	{
		internal int argPosition;

		internal object val;

		internal OutParamRecord(int argPosition, object val)
		{
			this.argPosition = argPosition;
			this.val = val;
		}
	}
}
