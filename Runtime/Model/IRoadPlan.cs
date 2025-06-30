using System.Collections.Generic;

namespace Districts.Model
{
    /// <summary>
    /// Describes a plan of nodes which form roads and districts.
    /// </summary>
    public interface IRoadPlan
    {
        IEnumerable<IRoadNode> Nodes { get; }

        IEnumerable<IRoad> Roads { get; }

        IEnumerable<IRoadRoute> Districts { get; }

        /// <summary>
        /// Get the nodes connected to a node.
        /// </summary>
        IEnumerable<IRoadNode> ConnectedNodes(IRoadNode node);

        /// <summary>
        /// Get the roads connected to a node.
        /// </summary>
        IEnumerable<IRoad> ConnectedRoads(IRoadNode node);

        /// <summary>
        /// Get the connecting road between two nodes.
        /// </summary>
        IRoad ConnectingRoad(IRoadNode a, IRoadNode b);

        /// <summary>
        /// Get the connecting roads along a series of nodes.
        /// </summary>
        IEnumerable<IRoad> ConnectingRoads(IEnumerable<IRoadNode> nodes);

        /// <summary>
        /// Get the district adjacent to a side of a road.
        /// </summary>
        IRoadRoute ConnectedDistrict(IRoad road, RoadSide side);
    }
}