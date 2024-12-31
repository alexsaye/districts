using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoadPlanning
{
    /// <summary>
    /// Describes a plan of nodes, which form roads, which form districts.
    /// </summary>
    public interface IPlan
    {
        IEnumerable<INode> Nodes { get; }

        IEnumerable<IRoad> Roads { get; }

        /// <summary>
        /// Get all the nodes connecting to a node.
        /// </summary>
        IEnumerable<INode> ConnectingNodes(INode node);

        /// <summary>
        /// Get all the roads connecting to a node.
        /// </summary>
        IEnumerable<IRoad> ConnectingRoads(INode node);

        /// <summary>
        /// Get the road connecting two nodes.
        /// </summary>
        IRoad ConnectingRoad(INode a, INode b);

        /// <summary>
        /// Get the closest road to a position.
        /// </summary>
        IRoad ClosestRoad(Vector3 position);

        /// <summary>
        /// Get the closest road to a position within a selection of roads.
        /// </summary>
        IRoad ClosestRoad(Vector3 position, IEnumerable<IRoad> roads);

        /// <summary>
        /// Get the district adjacent to a side of a road.
        /// </summary>
        IList<IRoad> AdjacentDistrict(Side side, IRoad road);

        static string DistrictToString(IEnumerable<IRoad> district)
        {
            return string.Join(", ", district.Select(road => road.Name));
        }
    }

    /// <summary>
    /// A plan of nodes, which form roads, which form districts.
    /// </summary>
    public class Plan : IPlan
    {
        public IEnumerable<INode> Nodes => roadsByNode.Keys;

        public IEnumerable<IRoad> Roads { get; private set; }

        private readonly IDictionary<INode, IDictionary<INode, IRoad>> roadsByNode;

        private readonly IDictionary<IRoad, IDictionary<Side, IList<IRoad>>> districtsByRoadSide;

        public Plan(IDictionary<INode, IDictionary<INode, IRoad>> graph)
        {
            // Cache the graph.
            roadsByNode = new Dictionary<INode, IDictionary<INode, IRoad>>(graph);

            // Cache all unique roads.
            Roads = graph.Values.SelectMany(connections => connections.Values).ToHashSet();

            // Prepare the cache for districts by road side.
            districtsByRoadSide = new Dictionary<IRoad, IDictionary<Side, IList<IRoad>>>();
            foreach (var road in Roads)
            {
                districtsByRoadSide[road] = new Dictionary<Side, IList<IRoad>>();
            }

            // Build all cycles in the graph.
            var cycles = BuildAllCycles(graph.First().Key, null, new List<INode>(), new HashSet<IRoad>(), new HashSet<IList<IRoad>>());
            if (cycles.Count == 0)
            {
                throw new ArgumentException("No cycles found, plan is invalid.");
            }

            // Find the interior districts from all the cycles.
            var interiorDistricts = FindInteriorDistricts(cycles);
            foreach (var interiorDistrict in interiorDistricts)
            {
                Debug.Log($"Interior district: {IPlan.DistrictToString(interiorDistrict)}");
            }

            if (interiorDistricts.Count == 1)
            {
                // If there is only one interior district, then it shares all its roads with the exterior district, so duplicate it in case we need to detect whether we've crossed the plan boundary.
                var exteriorDistrict = new List<IRoad>(interiorDistricts.First());
                Debug.Log($"Exterior district: {IPlan.DistrictToString(exteriorDistrict)}");

                // TODO: Figure out how to determine which side of the road is the interior side when there is only one district.
                CacheDistrictSide(exteriorDistrict, exteriorDistrict.First(), Side.Left);
            }
            else
            {
                // Build the exterior district from the interior districts. TODO: This cycle has already been found, maybe it would be better to find it from all the cycles than to build it from the interior districts.
                var exteriorDistrict = BuildExteriorDistrict(interiorDistricts);
                Debug.Log($"Exterior district: {IPlan.DistrictToString(exteriorDistrict)}");

                // Get a connected pair of roads where one is on the exterior and the other is on the interior.
                IRoad exteriorRoad = null;
                IRoad interiorRoad = null;
                INode connectingNode = null;
                foreach (var road in exteriorDistrict)
                {
                    exteriorRoad = road;
                    interiorRoad = ConnectingRoads(road.Start).Where(road => !exteriorDistrict.Contains(road)).First();
                    if (interiorRoad != null)
                    {
                        connectingNode = road.Start;
                        Debug.Log($"Exterior road {exteriorRoad.Name} connects to interior road {interiorRoad.Name} at start node {connectingNode.Name}.");
                        break;
                    }
                    interiorRoad = ConnectingRoads(road.End).Where(road => !exteriorDistrict.Contains(road)).First();
                    if (interiorRoad != null)
                    {
                        connectingNode = road.End;
                        Debug.Log($"Exterior road {exteriorRoad.Name} connects to interior road {interiorRoad.Name} at end node {connectingNode.Name}.");
                        break;
                    }
                }

                // Get the node of the interior road that is not shared with the exterior road.
                var referenceNode = interiorRoad.Start.Equals(connectingNode) ? interiorRoad.End : interiorRoad.Start;

                // That node must be on the interior side of the exterior road.
                var interiorSide = exteriorRoad.SideOfPoint(referenceNode.Position);

                Debug.Log($"Reference node {referenceNode.Name} is on the {interiorSide} side of exterior road {exteriorRoad.Name}.");

                // The exterior district must be on the opposite side.
                CacheDistrictSide(exteriorDistrict, exteriorRoad, interiorSide.Opposite());
                Debug.Log($"Cached exterior district {IPlan.DistrictToString(exteriorDistrict)} on the {interiorSide.Opposite()} side of road {exteriorRoad.Name}.");
            }

            // Queue up the interior districts and propagate the exterior sides inwards.
            var pendingDistricts = new Queue<IList<IRoad>>(interiorDistricts);
            while (pendingDistricts.Count > 0)
            {
                // Check if the district contains a road that has already been cached with another district.
                var pendingDistrict = pendingDistricts.Dequeue();
                var cachedRoad = pendingDistrict.FirstOrDefault(road => districtsByRoadSide[road].Count > 0);
                if (cachedRoad != null)
                {
                    // This district must be on the opposite side of the road.
                    var cachedSide = districtsByRoadSide[cachedRoad].Keys.First();
                    CacheDistrictSide(pendingDistrict, cachedRoad, cachedSide.Opposite());
                    Debug.Log($"Cached district: {IPlan.DistrictToString(pendingDistrict)} on the {cachedSide.Opposite()} side of road {cachedRoad.Name}.");
                }
                else
                {
                    // The district has no roads it can reference yet, so move it to the back of the queue.
                    pendingDistricts.Enqueue(pendingDistrict);
                }
            }

            Debug.Log("Plan created.");
        }

        /// <summary>
        /// Cache the district on a side of a road and propagate through all the roads of the district, inverting the side if travelling backwards along a road instead of forwards.
        /// </summary>
        private void CacheDistrictSide(IList<IRoad> district, IRoad road, Side side)
        {
            var startIndex = district.IndexOf(road);
            if (startIndex == -1) {
                throw new ArgumentException("The reference road is not in the district.");
            }

            districtsByRoadSide[road][side] = district;

            var currentRoad = road;
            var currentSide = side;
            for (var i = 1; i < district.Count; ++i)
            {
                var index = (startIndex + i) % district.Count;
                var previousRoad = currentRoad;
                currentRoad = district[index];
                currentSide = currentRoad.Start.Equals(currentRoad.End) || currentRoad.End.Equals(currentRoad.Start) ? currentSide : currentSide.Opposite();
                districtsByRoadSide[currentRoad][currentSide] = district;
            }
        }

        public Plan(IEnumerable<IRoadBuilder> graph) : this(IRoadBuilder.Build(graph)) { }


        /// <summary>
        /// Perform a depth-first search to find all unique road cycles, building up a found cycles cache.
        /// </summary>
        private ICollection<IList<IRoad>> BuildAllCycles(INode current, INode previous, IList<INode> trace, ISet<IRoad> travelled, ICollection<IList<IRoad>> found)
        {
            trace.Add(current);

            foreach (var connection in roadsByNode[current].Keys)
            {
                // Don't go whence we came.
                if (connection == previous)
                {
                    continue;
                }

                // Get the road for this connection.
                var road = roadsByNode[connection][current];

                // Don't go along a road we've already travelled.
                if (travelled.Contains(road))
                {
                    continue;
                }

                travelled.Add(road);

                // If we've already visited this node, we've found a cycle.
                if (trace.Contains(connection))
                {
                    // Start with the road from the connection to the current node.
                    var cycle = new List<IRoad>() {
                        road,
                        roadsByNode[current][previous]
                    };

                    // Trace the roads back to the connection to form the cycle.
                    var index = trace.Count - 2;
                    do
                    {
                        cycle.Add(roadsByNode[trace[index]][trace[index - 1]]);
                        --index;
                    } while (!trace[index].Equals(connection));

                    // Cache the cycle if we haven't already found this cycle before.
                    if (!found.Any(existing => existing.Count == cycle.Count && existing.All(road => cycle.Contains(road))))
                    {
                        found.Add(cycle);
                    }
                }
                else
                {
                    // Otherwise, travel this road and continue the search
                    BuildAllCycles(connection, current, trace, travelled, found);
                }

                travelled.Remove(road);
            }
            trace.RemoveAt(trace.Count - 1);

            return found;
        }

        /// <summary>
        /// Find the interior districts by reducing a set of cycles to cover all roads with only the shortest cycles.
        /// </summary>
        private ICollection<IList<IRoad>> FindInteriorDistricts(ICollection<IList<IRoad>> cycles)
        {
            var reduced = new HashSet<IList<IRoad>>();
            var ordered = cycles.OrderBy(cycle => cycle.Count);
            var covered = new HashSet<IRoad>();
            while (covered.Count < Roads.Count())
            {
                // Find the shortest cycle that covers a road we haven't covered yet, mark its roads as covered and cache it. I'm pretty sure the efficiency of this can be improved.
                var shortest = ordered.First(cycle => cycle.Any(road => !covered.Contains(road)));
                covered.UnionWith(shortest);
                reduced.Add(shortest);
            }
            return reduced;
        }

        /// <summary>
        /// Build the exterior district from a set of interior districts.
        /// </summary>
        private IList<IRoad> BuildExteriorDistrict(ICollection<IList<IRoad>> interiorDistricts)
        {
            var exteriorDistrict = new List<IRoad>();
            foreach (var interiorDistrict in interiorDistricts)
            {
                foreach (var road in interiorDistrict)
                {
                    // Construct the exterior from roads that are only covered by one cycle - interior roads are shared by two districts.
                    if (!interiorDistricts.Any(other => other != interiorDistrict && other.Contains(road)))
                    {
                        exteriorDistrict.Add(road);
                    }
                }
            }

            // TODO: Sort the list so that adjacent roads share nodes.

            return exteriorDistrict;
        }

        public IEnumerable<INode> ConnectingNodes(INode node)
        {
            return roadsByNode[node].Keys;
        }

        public IEnumerable<IRoad> ConnectingRoads(INode node)
        {
            return roadsByNode[node].Values;
        }

        public IRoad ConnectingRoad(INode a, INode b)
        {
            return roadsByNode[a][b];
        }

        public IRoad ClosestRoad(Vector3 position)
        {
            return districtsByRoadSide.Keys
                .OrderBy(road => Vector3.SqrMagnitude(road.ClosestPoint(position) - position))
                .First();
        }

        public IRoad ClosestRoad(Vector3 position, IEnumerable<IRoad> roads)
        {
            return roads
                .OrderBy(road => Vector3.SqrMagnitude(road.ClosestPoint(position) - position))
                .First();
        }

        public IList<IRoad> AdjacentDistrict(Side side, IRoad road)
        {
            return districtsByRoadSide[road][side];
        }
    }

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
    }

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

    /// <summary>
    /// Describes a connection position for roads.
    /// </summary>
    public interface INode
    {
        string Name { get; }

        Vector3 Position { get; }
    }

    /// <summary>
    /// Describes a node that can build roads to other nodes.
    /// </summary>
    public interface IRoadBuilder : INode
    {
        /// <summary>
        /// Directional forward connections to other nodes.
        /// </summary>
        public IEnumerable<IRoadBuilder> Connections { get; }

        /// <summary>
        /// Build a road between this node and another node.
        /// </summary>
        public IRoad Build(INode node);

        /// <summary>
        /// Build all roads as an undirected graph of nodes superimposed with a directed graph of roads.
        /// </summary>
        public static IDictionary<INode, IDictionary<INode, IRoad>> Build(IEnumerable<IRoadBuilder> nodes)
        {
            var roads = new Dictionary<INode, IDictionary<INode, IRoad>>();
            foreach (var node in nodes)
            {
                roads[node] = new Dictionary<INode, IRoad>();
            }
            foreach (var node in nodes)
            {
                foreach (var connection in node.Connections)
                {
                    var road = node.Build(connection);
                    roads[node][connection] = road;
                    roads[connection][node] = road;
                }
            }
            return roads;
        }
    }

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