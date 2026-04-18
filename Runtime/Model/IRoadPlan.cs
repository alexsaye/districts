using System.Collections.Generic;
using UnityEngine;

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

        IEnumerable<IRoadNode> DeadEndNodes { get; }

        IEnumerable<IRoadRoute> DeadEndRoutes { get; }

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

        /// <summary>
        /// Get the districts adjacent to a node.
        /// </summary>
        IEnumerable<IRoadRoute> ConnectedDistricts(IRoadNode node);

        /// <summary>
        /// Get the district containing a position.
        /// </summary>
        IRoadRoute ContainingDistrict(Vector3 position);

        /// <summary>
        /// Get the district containing a position, searching only within a set of districts.
        /// </summary>
        IRoadRoute ContainingDistrict(Vector3 position, IEnumerable<IRoadRoute> searchDistricts);

        /// <summary>
        /// Get the roads contained within a district, including any dead-end roads within that district.
        /// </summary>
        IEnumerable<IRoad> ContainedRoads(IRoadRoute district);
    }
}