using UnityEngine;

namespace Districts.Model
{
    /// <summary>
    /// Describes a road going from one node to another node.
    /// </summary>
    public interface IRoad
    {
        string Name { get; }

        INode Start { get; }

        INode End { get; }

        /// <summary>
        /// Get which side of the road the position is on compared with the direction of the road.
        /// </summary>
        Side SideOfPoint(Vector3 position);

        /// <summary>
        /// Get the closest point on the road to the given position.
        /// </summary>
        Vector3 ClosestPoint(Vector3 position);

        /// <summary>
        /// Are two roads connected by a convergence at their ends?
        /// </summary>
        static bool AreConverging(IRoad a, IRoad b)
        {
            return a.End.Equals(b.End);
        }

        /// <summary>
        /// Are two roads connected by a divergence from their starts?
        /// </summary>
        static bool AreDiverging(IRoad a, IRoad b)
        {
            return a.Start.Equals(b.Start);
        }

        /// <summary>
        /// Are two roads connected sequentially in the given order?
        /// </summary>
        static bool AreSequential(IRoad a, IRoad b)
        {
            return a.End.Equals(b.Start);
        }

        /// <summary>
        /// Are two roads connected directly in any way?
        /// </summary>
        static bool AreConnected(IRoad a, IRoad b)
        {
            return AreSequential(a, b) || AreSequential(b, a) || AreDiverging(a, b) || AreConverging(a, b);
        }

        /// <summary>
        /// Get the node shared by two connected roads. If the roads are connected to each other, the start node of the first road will be returned.
        /// </summary>
        static INode SharedNode(IRoad a, IRoad b)
        {
            if (AreDiverging(a, b) || AreSequential(b, a))
            {
                return a.Start;
            }
            if (AreConverging(a, b) || AreSequential(a, b))
            {
                return a.End;
            }
            return null;
        }

        /// <summary>
        /// Get the other node of the road that is not the given node.
        /// </summary>
        static INode OtherNode(IRoad road, INode node)
        {
            if (road.Start.Equals(node))
            {
                return road.End;
            }
            if (road.End.Equals(node))
            {
                return road.Start;
            }
            return null;
        }
    }
}