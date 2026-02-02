using OpenCvSharp;

namespace capture.Core
{
    public static class OpenCvTemplateMatcher
    {
        public static TemplateMatchResult MatchFile(string imagePath, string templatePath, TemplateMatchOptions? options = null)
        {
            options ??= new TemplateMatchOptions();

            using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (image.Empty())
                throw new FileNotFoundException($"Failed to read image: {imagePath}", imagePath);

            using var template = Cv2.ImRead(templatePath, ImreadModes.Color);
            if (template.Empty())
                throw new FileNotFoundException($"Failed to read template: {templatePath}", templatePath);

            return Match(image, template, options);
        }

        public static TemplateMatchResult Match(Mat image, Mat template, TemplateMatchOptions options)
        {
            if (image.Empty())
                throw new ArgumentException("Image is empty", nameof(image));
            if (template.Empty())
                throw new ArgumentException("Template is empty", nameof(template));

            var roi = options.SearchRoi ?? new Rect(0, 0, image.Width, image.Height);
            roi = ClampToImage(roi, image.Width, image.Height);

            using var imageRoi = new Mat(image, roi);
            using var imageProcessed = Preprocess(imageRoi, options);
            using var templateProcessed = Preprocess(template, options);

            if (templateProcessed.Width > imageProcessed.Width || templateProcessed.Height > imageProcessed.Height)
            {
                throw new ArgumentException(
                    $"Template ({templateProcessed.Width}x{templateProcessed.Height}) is larger than search area ({imageProcessed.Width}x{imageProcessed.Height})."
                );
            }

            using var result = new Mat();
            Cv2.MatchTemplate(imageProcessed, templateProcessed, result, options.Method);

            Cv2.MinMaxLoc(result, out var minVal, out var maxVal, out var minLoc, out var maxLoc);

            var (score, locInRoi) = SelectScoreAndLocation(options.Method, minVal, maxVal, minLoc, maxLoc);
            var loc = new capture.Core.Point(locInRoi.X + roi.X, locInRoi.Y + roi.Y);

            return new TemplateMatchResult(
                Score: score,
                Location: loc,
                TemplateWidth: template.Width,
                TemplateHeight: template.Height,
                SearchArea: new Rectangle(roi.X, roi.Y, roi.X + roi.Width, roi.Y + roi.Height)
            );
        }

        private static Mat Preprocess(Mat src, TemplateMatchOptions options)
        {
            Mat current = src;
            Mat? gray = null;
            Mat? edges = null;

            if (options.UseGrayscale && current.Channels() > 1)
            {
                gray = new Mat();
                Cv2.CvtColor(current, gray, ColorConversionCodes.BGR2GRAY);
                current = gray;
            }

            if (options.UseCannyEdges)
            {
                edges = new Mat();
                Cv2.Canny(current, edges, options.CannyThreshold1, options.CannyThreshold2);
                current = edges;
            }

            // Ensure we own a standalone Mat to avoid holding onto ROI parent buffers unexpectedly.
            var output = current.Clone();

            edges?.Dispose();
            gray?.Dispose();

            return output;
        }

        private static (double score, OpenCvSharp.Point loc) SelectScoreAndLocation(
            TemplateMatchModes mode,
            double minVal,
            double maxVal,
            OpenCvSharp.Point minLoc,
            OpenCvSharp.Point maxLoc
        )
        {
            return mode switch
            {
                TemplateMatchModes.SqDiff => (1.0 - minVal, minLoc),
                TemplateMatchModes.SqDiffNormed => (1.0 - minVal, minLoc),
                _ => (maxVal, maxLoc),
            };
        }

        private static Rect ClampToImage(Rect roi, int imageWidth, int imageHeight)
        {
            var x = Math.Clamp(roi.X, 0, imageWidth);
            var y = Math.Clamp(roi.Y, 0, imageHeight);
            var right = Math.Clamp(roi.X + roi.Width, 0, imageWidth);
            var bottom = Math.Clamp(roi.Y + roi.Height, 0, imageHeight);

            var w = Math.Max(0, right - x);
            var h = Math.Max(0, bottom - y);

            return new Rect(x, y, w, h);
        }
    }
}
