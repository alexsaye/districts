namespace Districts.Model
{
    /// <summary>
    /// The side of a road.
    /// </summary>
    public enum RoadSide
    {
        Left,
        Right
    }

    public static class RoadSideExtensions
    {
        public static RoadSide Opposite(this RoadSide side)
        {
            // This enum is just a new flavour of bool. Yes it's less efficient, but I like how declarative this is compared with getting a road by "true" or "false".
            return side == RoadSide.Left ? RoadSide.Right : RoadSide.Left;
        }
    }
}