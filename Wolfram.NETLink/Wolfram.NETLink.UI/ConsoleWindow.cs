using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Wolfram.NETLink.UI
{
	public class ConsoleWindow : Form
	{
		[Flags]
		public enum StreamType
		{
			None = 0x0,
			Out = 0x1,
			Error = 0x2
		}

		internal class TextBoxStream : Stream
		{
			private TextBox tb;

			public override bool CanRead => false;

			public override bool CanWrite => true;

			public override bool CanSeek => false;

			public override long Length
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			public override long Position
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			internal TextBoxStream(TextBox tb)
			{
				this.tb = tb;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (tb.IsHandleCreated && !tb.IsDisposed)
				{
					StringBuilder stringBuilder = new StringBuilder(count);
					for (int i = 0; i < count; i++)
					{
						stringBuilder.Append((char)buffer[offset + i]);
					}
					tb.AppendText(stringBuilder.ToString());
					if (tb.Lines.Length > MaxLines)
					{
						string[] array = new string[MaxLines];
						Array.Copy(tb.Lines, tb.Lines.Length - MaxLines, array, 0, MaxLines);
						tb.Lines = array;
					}
					tb.SelectionStart = int.MaxValue;
					tb.SelectionLength = 0;
					tb.ScrollToCaret();
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			public override void Flush()
			{
			}

			public override long Seek(long i, SeekOrigin org)
			{
				throw new NotSupportedException();
			}

			public override void SetLength(long i)
			{
				throw new NotSupportedException();
			}
		}

		private static ConsoleWindow singleton;

		private static StreamType strms = StreamType.Out | StreamType.Error;

		private static int maxLines = 500;

		private TextWriter origOut;

		private TextWriter origErr;

		private StreamWriter writer;

		private TextBox textBox1;

		private Container components;

		public static ConsoleWindow Instance
		{
			get
			{
				if (singleton == null || singleton.IsDisposed)
				{
					singleton = new ConsoleWindow();
				}
				return singleton;
			}
		}

		public static StreamType StreamsToCapture
		{
			get
			{
				return strms;
			}
			set
			{
				StreamType oldValue = strms;
				strms = value;
				if (singleton != null)
				{
					singleton.updateStreams(oldValue);
				}
			}
		}

		public static int MaxLines
		{
			get
			{
				return maxLines;
			}
			set
			{
				maxLines = value;
			}
		}

		public static void Clear()
		{
			Instance.textBox1.Lines = new string[1]
			{
				string.Empty
			};
		}

		private void updateStreams(StreamType oldValue)
		{
			if ((strms & StreamType.Out) != 0 && (oldValue & StreamType.Out) == 0)
			{
				origOut = Console.Out;
				Console.SetOut(writer);
			}
			else if ((strms & StreamType.Out) == 0 && (oldValue & StreamType.Out) != 0)
			{
				Console.SetOut(origOut);
			}
			if ((strms & StreamType.Error) != 0 && (oldValue & StreamType.Error) == 0)
			{
				origErr = Console.Error;
				Console.SetError(writer);
			}
			else if ((strms & StreamType.Error) == 0 && (oldValue & StreamType.Error) != 0)
			{
				Console.SetError(origErr);
			}
		}

		private ConsoleWindow()
		{
			InitializeComponent();
			writer = new StreamWriter(new TextBoxStream(textBox1));
			writer.AutoFlush = true;
			textBox1.Text = string.Concat(".NET/Link Version 1.7.0\r\n.NET Framework Version ", Environment.Version, "\r\n===========================================\r\n");
			updateStreams(StreamType.None);
		}

		protected override void OnCreateControl()
		{
			textBox1.SelectionStart = 10000;
			base.OnCreateControl();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (origOut != null)
				{
					Console.SetOut(origOut);
				}
				if (origErr != null)
				{
					Console.SetError(origErr);
				}
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			textBox1 = new System.Windows.Forms.TextBox();
			SuspendLayout();
			textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			textBox1.Font = new System.Drawing.Font("Courier New", 10f);
			textBox1.Multiline = true;
			textBox1.Name = "textBox1";
			textBox1.ReadOnly = true;
			textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			textBox1.Size = new System.Drawing.Size(448, 397);
			textBox1.TabIndex = 0;
			textBox1.Text = "";
			AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			base.ClientSize = new System.Drawing.Size(448, 397);
			base.Controls.AddRange(new System.Windows.Forms.Control[1]
			{
				textBox1
			});
			base.Name = "ConsoleWindow";
			Text = ".NET Console";
			ResumeLayout(false);
		}
	}
}
