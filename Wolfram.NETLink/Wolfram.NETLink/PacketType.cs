namespace Wolfram.NETLink
{
	public enum PacketType
	{
		Illegal = 0,
		Call = 7,
		Evaluate = 13,
		Return = 3,
		InputName = 8,
		EnterText = 14,
		EnterExpression = 0xF,
		OutputName = 9,
		ReturnText = 4,
		ReturnExpression = 0x10,
		Display = 11,
		DisplayEnd = 12,
		Message = 5,
		Text = 2,
		Input = 1,
		InputString = 21,
		Menu = 6,
		Syntax = 10,
		Suspend = 17,
		Resume = 18,
		BeginDialog = 19,
		EndDialog = 20,
		FirstUser = 0x80,
		LastUser = 0xFF,
		FrontEnd = 100,
		Expression = 101
	}
}
