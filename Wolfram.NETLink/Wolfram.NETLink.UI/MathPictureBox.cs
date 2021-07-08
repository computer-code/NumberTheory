using System.Drawing;
using System.Windows.Forms;

namespace Wolfram.NETLink.UI
{
	public class MathPictureBox : PictureBox
	{
		private IKernelLink ml;

		private bool usesFE;

		private string mathCommand;

		private string pictureType;

		public IKernelLink Link
		{
			get
			{
				return ml;
			}
			set
			{
				ml = value;
			}
		}

		public string PictureType
		{
			get
			{
				return pictureType;
			}
			set
			{
				switch (value.ToLower())
				{
				case "gif":
					pictureType = "GIF";
					base.SizeMode = PictureBoxSizeMode.CenterImage;
					break;
				case "jpeg":
					pictureType = "JPEG";
					base.SizeMode = PictureBoxSizeMode.CenterImage;
					break;
				case "metafile":
					pictureType = "Metafile";
					base.SizeMode = PictureBoxSizeMode.StretchImage;
					break;
				case "standardform":
					pictureType = "StandardForm";
					base.SizeMode = PictureBoxSizeMode.CenterImage;
					break;
				case "traditionalform":
					pictureType = "TraditionalForm";
					base.SizeMode = PictureBoxSizeMode.CenterImage;
					break;
				default:
					pictureType = "Automatic";
					base.SizeMode = PictureBoxSizeMode.CenterImage;
					break;
				}
			}
		}

		public bool UseFrontEnd
		{
			get
			{
				return usesFE;
			}
			set
			{
				usesFE = value;
			}
		}

		public string MathCommand
		{
			get
			{
				return mathCommand;
			}
			set
			{
				mathCommand = value;
				if (Link == null)
				{
					base.Image = null;
					return;
				}
				if (Link == StdLink.Link)
				{
					StdLink.RequestTransaction();
				}
				if (PictureType == "Automatic" || PictureType == "GIF" || PictureType == "JPEG" || PictureType == "Metafile")
				{
					bool useFrontEnd = Link.UseFrontEnd;
					Link.UseFrontEnd = UseFrontEnd;
					string graphicsFormat = Link.GraphicsFormat;
					Link.GraphicsFormat = PictureType;
					Image image2 = (base.Image = Link.EvaluateToImage(mathCommand, base.Width - 4, base.Height - 4));
					Link.UseFrontEnd = useFrontEnd;
					Link.GraphicsFormat = graphicsFormat;
				}
				else
				{
					bool typesetStandardForm = Link.TypesetStandardForm;
					Link.TypesetStandardForm = PictureType != "TraditionalForm";
					base.Image = Link.EvaluateToTypeset(mathCommand, base.Width);
					Link.TypesetStandardForm = typesetStandardForm;
				}
			}
		}

		public MathPictureBox()
		{
			SetStyle(ControlStyles.UserPaint, value: true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
			SetStyle(ControlStyles.DoubleBuffer, value: true);
			base.SizeMode = PictureBoxSizeMode.CenterImage;
			Link = StdLink.Link;
			UseFrontEnd = true;
			PictureType = "Automatic";
		}

		public MathPictureBox(IKernelLink ml)
		{
			Link = ml;
		}

		public void Recompute()
		{
			if (MathCommand != null)
			{
				MathCommand = MathCommand;
			}
		}
	}
}
