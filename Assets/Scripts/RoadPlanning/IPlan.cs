using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoadPlanning
{
    /// <summary>
    /// Describes a plan of nodes which form roads and districts.
    /// </summary>
    public interface IPlan
    {
        IEnumerable<INode> Nodes { get; }

        IEnumerable<IRoad> Roads { get; }

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
        /// Get the connecting roads along a route.
        /// </summary>
        IEnumerable<IRoad> ConnectingRoads(IEnumerable<INode> route);

        /// <summary>
        /// Get the closest road to a position.
        /// </summary>
        IRoad ClosestRoad(Vector3 position);

        /// <summary>
        /// Get the closest road to a position within a route.
        /// </summary>
        IRoad ClosestRoad(Vector3 position, IEnumerable<INode> route);

        /// <summary>
        /// Get the district adjacent to a side of a road.
        /// </summary>
        IEnumerable<INode> AdjacentDistrict(IRoad road, Side side);

        /// <summary>
        /// Get all routes between two nodes, optionally within a maximum number of nodes.
        /// </summary>
        ICollection<IEnumerable<INode>> AllRoutes(INode a, INode b, int maxNodes = int.MaxValue);

        static string RouteToString(IEnumerable<INode> route)
        {
            return string.Join(" -> ", route.Select(node => node.Name));
        }
    }
}