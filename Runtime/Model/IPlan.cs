using System.Collections.Generic;

namespace Districts.Model
{
    /// <summary>
    /// Describes a plan of nodes which form roads and districts.
    /// </summary>
    public interface IPlan
    {
        IEnumerable<INode> Nodes { get; }

        IEnumerable<IRoad> Roads { get; }

        IEnumerable<IRoute> Districts { get; }

        /// <summary>
        /// Get the nodes connected to a node.
        /// </summary>
        IEnumerable<INode> ConnectedNodes(INode node);

        /// <summary>
        /// Get the roads connected to a node.
        /// </summary>
        IEnumerable<IRoad> ConnectedRoads(INode node);

        /// <summary>
        /// Get the connecting road between two nodes.
        /// </summary>
        IRoad ConnectingRoad(INode a, INode b);

        /// <summary>
        /// Get the connecting roads along a series of nodes.
        /// </summary>
        IEnumerable<IRoad> ConnectingRoads(IEnumerable<INode> nodes);

        /// <summary>
        /// Get the district adjacent to a side of a road.
        /// </summary>
        IRoute ConnectedDistrict(IRoad road, Side side);
    }
}