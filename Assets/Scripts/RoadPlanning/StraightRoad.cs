using UnityEngine;

namespace RoadPlanning
{
    /// <summary>
    /// A straight road, the simplest road.
    /// </summary>
    public class StraightRoad : IRoad
    {
        public string Name { get; private set; }

        public INode Start { get; private set; }

        public INode End { get; private set; }

        public StraightRoad(INode start, INode end, string name)
        {
            Start = start;
            End = end;
            Name = name;
        }

        public Side SideOfPoint(Vector3 position)
        {
            var axis = (End.Position - Start.Position).normalized;
            var sign = LineUtils.SignOfPointOnAxis(position - Start.Position, axis, Vector3.up);
            return sign > 0f ? Side.Right : Side.Left;
        }

        public Vector3 ClosestPoint(Vector3 position)
        {
            return LineUtils.ClosestPoint(position, Start.Position, End.Position);
        }
    }
}