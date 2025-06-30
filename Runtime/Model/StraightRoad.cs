using UnityEngine;

namespace Districts.Model
{
    /// <summary>
    /// A straight road, the simplest road.
    /// </summary>
    public class StraightRoad : IRoad
    {
        public string Name { get; private set; }

        public IRoadNode Start { get; private set; }

        public IRoadNode End { get; private set; }

        public float Length { get; private set; }

        public StraightRoad(IRoadNode start, IRoadNode end, string name)
        {
            Start = start;
            End = end;
            Name = name;
            Length = (end.Position - start.Position).magnitude;
        }

        public RoadSide SideOfPoint(Vector3 position)
        {
            var axis = (End.Position - Start.Position).normalized;
            var sign = LineUtils.SignOfPointOnAxis(position - Start.Position, axis, Vector3.up);
            return sign > 0f ? RoadSide.Right : RoadSide.Left;
        }

        public Vector3 ClosestPoint(Vector3 position)
        {
            return LineUtils.ClosestPoint(position, Start.Position, End.Position);
        }

        public IRoadNode ClosestNode(Vector3 position)
        {
            return (position - Start.Position).sqrMagnitude < (position - End.Position).sqrMagnitude ? Start : End;
        }
    }
}