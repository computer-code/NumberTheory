#define LINUX

using Wolfram.NETLink;

namespace WolframLinkNamespace
{
    public class WolframLink
    {
        private MathKernel _mathKernel;

        public WolframLink()
        {
            string mlArgs = "";

#if WIN
            mlArgs = "-linkmode launch -linkname \"C:\\\\Program Files\\\\Wolfram Research\\\\Mathematica\\\\12.0\\\\MathKernel.exe\"";
#elif LINUX
            mlArgs = "-linkmode launch -linkname '/usr/local/Wolfram/WolframEngine/12.2/Executables/MathKernel'";
#endif

            IKernelLink ml = MathLinkFactory.CreateKernelLink(mlArgs);
            ml.WaitAndDiscardAnswer();

            _mathKernel = new MathKernel
            {
                AutoCloseLink = true,
                CaptureGraphics = true,
                CaptureMessages = true,
                CapturePrint = true,
                GraphicsFormat = "Metafile",
                GraphicsHeight = 0,
                GraphicsResolution = 80,
                GraphicsWidth = 0,
                HandleEvents = true,
                Input = null,
                Link = ml,
                LinkArguments = null,
                PageWidth = 0,
                ResultFormat = MathKernel.ResultFormatType.OutputForm,
                UseFrontEnd = false,
            };
        }

        public string Evaluate(string str)
        {
            return _mathKernel.Link.EvaluateToOutputForm(str, 0);
        }

        public string Evaluate(string str, string arg)
        {
            str = str.Replace("#", arg);
            return _mathKernel.Link.EvaluateToOutputForm(str, 0);
        }

        public string Evaluate(string str, double arg)
        {
            str = str.Replace("#", arg.ToString());
            return _mathKernel.Link.EvaluateToOutputForm(str, 0);
        }

        public void Close()
        {
            if (_mathKernel != null)
                _mathKernel.Dispose();
        }
    }
}