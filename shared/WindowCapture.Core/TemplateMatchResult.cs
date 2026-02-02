namespace capture.Core
{
    public sealed record TemplateMatchResult(
        double Score,
        Point Location,
        int TemplateWidth,
        int TemplateHeight,
        Rectangle SearchArea
    )
    {
        public Rectangle MatchRect => new(
            Location.X,
            Location.Y,
            Location.X + TemplateWidth,
            Location.Y + TemplateHeight
        );

        public bool IsMatch(double threshold) => Score >= threshold;
    }
}
