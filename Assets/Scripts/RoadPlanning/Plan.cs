using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoadPlanning
{
    /// <summary>
    /// A plan of nodes which form roads, routes, and districts.
    /// </summary>
    public class Plan : IPlan
    {
        private readonly IDictionary<INode, IDictionary<INode, IRoad>> roadsByNode;

        private readonly IDictionary<IRoad, IDictionary<Side, IEnumerable<INode>>> districtsBySideOfRoad;

        public IEnumerable<INode> Nodes => roadsByNode.Keys;

        public IEnumerable<IRoad> Roads => districtsBySideOfRoad.Keys;

        public Plan(IEnumerable<IPlanBuilderNode> graph) : this(IPlanBuilderNode.Build(graph)) { }

        public Plan(IDictionary<INode, IDictionary<INode, IRoad>> graph)
        {
            // Deep copy the road graph. TODO: build the graph bidirectionally here instead of relying on it being provided already bidirectional.
            roadsByNode = new Dictionary<INode, IDictionary<INode, IRoad>>();
            foreach (var (node, roads) in graph)
            {
                roadsByNode.Add(node, new Dictionary<INode, IRoad>(roads));
            }

            // Build the districts by traversing each road forwards and backwards until we cover all roads in both directions with non-overlapping cycles.
            districtsBySideOfRoad = new Dictionary<IRoad, IDictionary<Side, IEnumerable<INode>>>();
            foreach (var road in graph.Values.SelectMany(connections => connections.Values))
            {
                districtsBySideOfRoad[road] = new Dictionary<Side, IEnumerable<INode>>();
            }
            var forwards = new HashSet<IRoad>();
            var backwards = new HashSet<IRoad>();
            foreach (var road in Roads)
            {
                if (!forwards.Contains(road))
                {
                    BuildDistrict(road.Start, road.End, forwards, backwards, new List<INode>());
                }

                if (!backwards.Contains(road))
                {
                    BuildDistrict(road.End, road.Start, forwards, backwards, new List<INode>());
                }
            }
        }

        /// <summary>
        /// Build a district by traversing a road, populating the districts by side of road cache.
        /// </summary>
        private void BuildDistrict(INode a, INode b, ISet<IRoad> forwards, ISet<IRoad> backwards, ICollection<INode> district)
        {
            // Add the first node of the road to the district and cache which direction we're travelling along the road.
            var road = ConnectingRoad(a, b);
            district.Add(a);
            if (a == road.Start)
            {
                forwards.Add(road);
            }
            else
            {
                backwards.Add(road);
            }

            // Find the node connected to b which results in most rightward turn from the direction of the road. This bias means we follow the borders of the district by sticking to one side (like badly solving a maze).
            INode connectingNode = null;
            var rightestTurn = -float.PositiveInfinity;
            var roadDirection = Vector3.Normalize(b.Position - a.Position);
            foreach (var node in ConnectedNodes(b))
            {
                if (node == a)
                {
                    continue;
                }

                var nextDirection = Vector3.Normalize(node.Position - b.Position);
                var cross = Vector3.Cross(roadDirection, nextDirection).y;
                var dot = Vector3.Dot(roadDirection, nextDirection);
                var turn = Mathf.Atan2(cross, dot);
                if (turn > rightestTurn)
                {
                    rightestTurn = turn;
                    connectingNode = node;
                }
            }

            // Check whether we've travelled along this road before.
            var connectingRoad = ConnectingRoad(b, connectingNode);
            if (b == connectingRoad.Start && forwards.Contains(connectingRoad) || b == connectingRoad.End && backwards.Contains(connectingRoad))
            {
                // The connecting road is the start road, so we've completed a district and can re-add the start node (which is b) to close the cycle.
                district.Add(b);

                // The current side of the road is right if we went forwards along it, left if we went backwards.
                var currentRoad = connectingRoad;
                var currentSide = b == connectingRoad.Start ? Side.Right : Side.Left;
                districtsBySideOfRoad[currentRoad][currentSide] = district;

                // Propagate the current side along the district, inverting it when roads converge or diverge.
                for (var i = 2; i < district.Count; ++i)
                {
                    var previousRoad = currentRoad;
                    currentRoad = ConnectingRoad(district.ElementAt(i - 1), district.ElementAt(i));
                    currentSide = IRoad.AreConverging(currentRoad, previousRoad) || IRoad.AreDiverging(currentRoad, previousRoad) ? currentSide.Opposite() : currentSide;
                    districtsBySideOfRoad[currentRoad][currentSide] = district;
                    Debug.Log($"District {IPlan.RouteToString(district)} is on the {currentSide} of {currentRoad.Name}.");
                }
            }
            else
            {
                // We haven't travelled the connecting road yet, so continue building the district along it.
                BuildDistrict(b, connectingNode, forwards, backwards, district);
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

        public IEnumerable<IRoad> ConnectingRoads(IEnumerable<INode> route)
        {
            var routeEnumerator = route.GetEnumerator();
            if (!routeEnumerator.MoveNext())
            {
                yield break;
            }

            var a = routeEnumerator.Current;
            while (routeEnumerator.MoveNext())
            {
                var b = routeEnumerator.Current;
                yield return ConnectingRoad(a, b);
                a = b;
            }
        }

        public IRoad ClosestRoad(Vector3 position)
        {
            return Roads
                .OrderBy(road => Vector3.SqrMagnitude(road.ClosestPoint(position) - position))
                .First();
        }

        public IRoad ClosestRoad(Vector3 position, IEnumerable<INode> route)
        {
            return ConnectingRoads(route)
                .OrderBy(road => Vector3.SqrMagnitude(road.ClosestPoint(position) - position))
                .First();
        }

        public IEnumerable<INode> AdjacentDistrict(IRoad road, Side side)
        {
            return districtsBySideOfRoad[road][side];
        }
        public ICollection<IEnumerable<INode>> AllRoutes(INode a, INode b, int maxNodes)
        {
            var routes = new List<IEnumerable<INode>>();
            BuildAllRoutes(a, b, new List<INode>(), new HashSet<IRoad>(), routes, maxNodes);
            return routes;
        }

        /// <summary>
        /// Perform a depth-first search to find all unique routes between two nodes, building up a found routes cache by caching the whole trace every time it finds the target node.
        /// </summary>
        private void BuildAllRoutes(INode current, INode target, IList<INode> trace, ISet<IRoad> travelled, ICollection<IEnumerable<INode>> routes, int maxNodes)
        {
            if (trace.Count == maxNodes)
            {
                return;
            }

            trace.Add(current);

            foreach (var connection in ConnectedNodes(current))
            {
                // Don't go whence we came.
                if (connection == trace.ElementAtOrDefault(trace.Count - 2))
                {
                    continue;
                }

                var road = ConnectingRoad(current, connection);

                // Don't go along a road we've already travelled.
                if (travelled.Contains(road))
                {
                    continue;
                }

                travelled.Add(road);

                // Have we found the target?
                if (connection.Equals(target))
                {
                    // Cache the route if we haven't already found this route before.
                    if (!routes.Any(existing => existing.Count() == trace.Count - 1 && existing.All(node => trace.Contains(node))))
                    {
                        var route = new List<INode>(trace) { target };
                        routes.Add(route);
                    }
                }
                else
                {
                    // Otherwise, travel to this connection and continue the search
                    BuildAllRoutes(connection, target, trace, travelled, routes, maxNodes);
                }

                travelled.Remove(road);
            }
            trace.RemoveAt(trace.Count - 1);
        }

    }
}