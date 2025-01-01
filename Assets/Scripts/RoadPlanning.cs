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
        /// Get all the nodes connected to a node.
        /// </summary>
        IEnumerable<INode> ConnectedNodes(INode node);

        /// <summary>
        /// Get all the roads connected to a node.
        /// </summary>
        IEnumerable<IRoad> ConnectedRoads(INode node);

        /// <summary>
        /// Get the connecting road between two nodes.
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
        private readonly IDictionary<INode, IDictionary<INode, IRoad>> roadsByNode;

        private readonly IDictionary<IRoad, IDictionary<Side, IList<IRoad>>> districtsBySideOfRoad;

        public IEnumerable<INode> Nodes => roadsByNode.Keys;

        public IEnumerable<IRoad> Roads => districtsBySideOfRoad.Keys;

        public Plan(IEnumerable<IRoadBuilder> graph) : this(IRoadBuilder.Build(graph)) { }

        public Plan(IDictionary<INode, IDictionary<INode, IRoad>> graph)
        {
            // Cache the graph.
            roadsByNode = new Dictionary<INode, IDictionary<INode, IRoad>>(graph);

            // Prepare the cache for districts by road side.
            districtsBySideOfRoad = new Dictionary<IRoad, IDictionary<Side, IList<IRoad>>>();
            foreach (var road in graph.Values.SelectMany(connections => connections.Values))
            {
                districtsBySideOfRoad[road] = new Dictionary<Side, IList<IRoad>>();
            }

            // Build all cycles in the graph.
            var cycles = BuildAllCycles(graph.First().Key, null, new List<INode>(), new HashSet<IRoad>(), new HashSet<IList<IRoad>>());
            if (cycles.Count == 0)
            {
                throw new ArgumentException("No cycles found, plan is invalid.");
            }

            // Find the interior districts from all the cycles.
            var interiorDistricts = FindInteriorDistricts(cycles);
            if (interiorDistricts.Count == 1)
            {
                // If there is only one interior district, then it shares all its roads with the exterior district.
                var exteriorDistrict = new List<IRoad>(interiorDistricts.First());

                // TODO: Figure out how to determine which side of the road is the interior side when there is only one district.
                CacheDistrictSide(exteriorDistrict, exteriorDistrict.First(), Side.Left);
            }
            else
            {
                // Build the exterior district by finding roads that are not shared between interior districts.
                var exteriorDistrict = BuildExteriorDistrict(interiorDistricts);

                // Find an exit road from the exterior district. As this is exiting the exterior, it must be entering the interior (even if it reemerges on the exterior).
                var exitRoad = FindDistrictExit(exteriorDistrict, out var exteriorRoad, out var exteriorNode);

                // Get the node of the exit road that is not the exterior node.
                var referenceNode = IRoad.OtherNode(exitRoad, exteriorNode);

                // That node must be on the interior side of the exterior road.
                var interiorSide = exteriorRoad.SideOfPoint(referenceNode.Position);

                // Therefore the exterior district must be on the opposite side.
                CacheDistrictSide(exteriorDistrict, exteriorRoad, interiorSide.Opposite());
            }

            // Queue up the interior districts and propagate the exterior sides inwards.
            var pendingDistricts = new Queue<IList<IRoad>>(interiorDistricts);
            while (pendingDistricts.Count > 0)
            {
                // Check if the district contains a road that has already been cached with another district.
                var pendingDistrict = pendingDistricts.Dequeue();
                var cachedRoad = pendingDistrict.FirstOrDefault(road => districtsBySideOfRoad[road].Count > 0);
                if (cachedRoad != null)
                {
                    // This district must be on the opposite side of the road.
                    var cachedSide = districtsBySideOfRoad[cachedRoad].Keys.First();
                    CacheDistrictSide(pendingDistrict, cachedRoad, cachedSide.Opposite());
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

            foreach (var district in reduced)
            {
                Debug.Log($"Found interior district: {IPlan.DistrictToString(district)}");
            }
            return reduced;
        }

        /// <summary>
        /// Build the exterior district from a set of interior districts.
        /// </summary>
        private IList<IRoad> BuildExteriorDistrict(ICollection<IList<IRoad>> interiorDistricts)
        {
            // Create a pool of roads that are only covered by one cycle.
            var pool = new HashSet<IRoad>();
            foreach (var interior in interiorDistricts)
            {
                foreach (var road in interior)
                {
                    if (!interiorDistricts.Any(other => other != interior && other.Contains(road)))
                    {
                        pool.Add(road);
                    }
                }
            }

            // Build the exterior by following the roads.
            var exterior = new List<IRoad>();
            var current = pool.First();
            while (current != null)
            {
                pool.Remove(current);
                exterior.Add(current);
                current = pool.FirstOrDefault(road => IRoad.AreConnected(current, road));
            }

            Debug.Log($"Built exterior district: {IPlan.DistrictToString(exterior)}");
            return exterior;
        }

        /// <summary>
        /// Find a road exiting from the district, the road within the district that is approaching it, and the node within the district connecting both.
        /// </summary>
        private IRoad FindDistrictExit(IList<IRoad> district, out IRoad districtRoad, out INode districtNode)
        {
            foreach (var road in district)
            {
                var exitFromStart = ConnectedRoads(road.Start).Where(road => !district.Contains(road)).First();
                if (exitFromStart != null)
                {
                    districtRoad = road;
                    districtNode = road.Start;
                    Debug.Log($"Road {road.Name} exits into road {exitFromStart.Name} via start node {districtNode.Name}.");
                    return exitFromStart;
                }

                var exitFromEnd = ConnectedRoads(road.End).Where(road => !district.Contains(road)).First();
                if (exitFromEnd != null)
                {
                    districtRoad = road;
                    districtNode = road.End;
                    Debug.Log($"Road {road.Name} exits into road {exitFromEnd.Name} via end node {districtNode.Name}.");
                    return exitFromEnd;
                }
            }
            districtRoad = null;
            districtNode = null;
            return null;
        }

        /// <summary>
        /// Cache the district on a side of a road and propagate through all the roads of the district, inverting the side if travelling backwards along a road instead of forwards.
        /// </summary>
        private void CacheDistrictSide(IList<IRoad> district, IRoad road, Side side)
        {
            var startIndex = district.IndexOf(road);
            if (startIndex == -1)
            {
                throw new ArgumentException("The reference road is not in the district.");
            }

            districtsBySideOfRoad[road][side] = district;
            Debug.Log($"Cached district {IPlan.DistrictToString(district)} on the {side} side of road {road.Name}.");

            var currentRoad = road;
            var currentSide = side;
            for (var i = 1; i < district.Count; ++i)
            {
                var index = (startIndex + i) % district.Count;
                var previousRoad = currentRoad;
                currentRoad = district[index];
                currentSide = IRoad.AreConverging(previousRoad, currentRoad) || IRoad.AreDiverging(previousRoad, currentRoad) ? currentSide.Opposite() : currentSide;
                districtsBySideOfRoad[currentRoad][currentSide] = district;
                Debug.Log($"Cached district {IPlan.DistrictToString(district)} on the {currentSide} side of road {currentRoad.Name} through propagation.");
            }
        }

        public IEnumerable<INode> ConnectedNodes(INode node)
        {
            return roadsByNode[node].Keys;
        }

        public IEnumerable<IRoad> ConnectedRoads(INode node)
        {
            return roadsByNode[node].Values;
        }

        public IRoad ConnectingRoad(INode a, INode b)
        {
            return roadsByNode[a][b];
        }

        public IRoad ClosestRoad(Vector3 position)
        {
            return districtsBySideOfRoad.Keys
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
            return districtsBySideOfRoad[road][side];
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