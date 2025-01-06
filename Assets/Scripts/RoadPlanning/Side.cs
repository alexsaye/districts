namespace RoadPlanning
{
    /// <summary>
    /// The side of a road.
    /// </summary>
    public enum Side
    {
        Left,
        Right
    }

    public static class SideExtensions
    {
        public static Side Opposite(this Side side)
        {
            // This enum is just a new flavour of bool. Yes it's less efficient, but I like how declarative this is compared with getting a road by "true" or "false".
            return side == Side.Left ? Side.Right : Side.Left;
        }
    }
}