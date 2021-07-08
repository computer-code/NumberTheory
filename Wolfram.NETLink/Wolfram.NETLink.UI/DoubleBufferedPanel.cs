using System.Windows.Forms;

namespace Wolfram.NETLink.UI
{
	public class DoubleBufferedPanel : Panel
	{
		public DoubleBufferedPanel()
		{
			SetStyle(ControlStyles.UserPaint, value: true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
			SetStyle(ControlStyles.DoubleBuffer, value: true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			try
			{
				base.OnPaint(e);
			}
			catch (MathematicaNotReadyException)
			{
			}
		}
	}
}
