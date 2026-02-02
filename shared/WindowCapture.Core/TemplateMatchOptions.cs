using OpenCvSharp;

namespace capture.Core
{
    public sealed class TemplateMatchOptions
    {
        public TemplateMatchModes Method { get; set; } = TemplateMatchModes.CCoeffNormed;
        public bool UseGrayscale { get; set; } = true;
        public bool UseCannyEdges { get; set; } = false;

        public double Threshold { get; set; } = 0.80;

        public double CannyThreshold1 { get; set; } = 60;
        public double CannyThreshold2 { get; set; } = 180;

        public Rect? SearchRoi { get; set; }
    }
}

