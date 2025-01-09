using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoadPlanning
{
    /// <summary>
    /// A plan of nodes which form roads and districts.
    /// </summary>
    public class Plan : IPlan
    {
        private readonly IDictionary<INode, IDictionary<INode, IRoad>> roadsByNode;

        private readonly IDictionary<IRoad, IDictionary<Side, IRoute>> districtsBySideOfRoad;

        private readonly ICollection<IRoute> districts;

        public IEnumerable<INode> Nodes => roadsByNode.Keys;

        public IEnumerable<IRoad> Roads => districtsBySideOfRoad.Keys;

        public IEnumerable<IRoute> Districts => districts;

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
            districts = new List<IRoute>();
            districtsBySideOfRoad = new Dictionary<IRoad, IDictionary<Side, IRoute>>();
            foreach (var road in graph.Values.SelectMany(connections => connections.Values))
            {
                districtsBySideOfRoad[road] = new Dictionary<Side, IRoute>();
            }
            var forwards = new HashSet<IRoad>();
            var backwards = new HashSet<IRoad>();
            foreach (var road in Roads)
            {
                if (!forwards.Contains(road))
                {
                    var forwardsDistrict = BuildDistrict(road.Start, road.End, forwards, backwards, new List<INode>());
                    districts.Add(forwardsDistrict);
                }

                if (!backwards.Contains(road))
                {
                    var backwardsDistrict = BuildDistrict(road.End, road.Start, forwards, backwards, new List<INode>());
                    districts.Add(backwardsDistrict);
                }
            }
        }

        /// <summary>
        /// Build a district by traversing a road, populating the districts by side of road cache.
        /// </summary>
        private IRoute BuildDistrict(INode a, INode b, HashSet<IRoad> forwards, HashSet<IRoad> backwards, List<INode> found)
        {
            // Add the first node of the road to the district and cache which direction we're travelling along the road.
            var road = ConnectingRoad(a, b);
            found.Add(a);
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
                found.Add(b);

                // Create the district from the found nodes.
                var district = new Route(found);

                // The current side of the road is right if we went forwards along it, left if we went backwards.
                var currentRoad = connectingRoad;
                var currentSide = b == currentRoad.Start ? Side.Right : Side.Left;
                districtsBySideOfRoad[currentRoad][currentSide] = district;

                // Propagate the current side along the district, inverting it when roads converge or diverge. (Skip the first road as we've just cached its side.)
                var enumerator = ConnectingRoads(found).GetEnumerator();
                enumerator.MoveNext();
                while (enumerator.MoveNext())
                {
                    var previousRoad = currentRoad;
                    currentRoad = enumerator.Current;
                    currentSide = IRoad.AreConverging(currentRoad, previousRoad) || IRoad.AreDiverging(currentRoad, previousRoad) ? currentSide.Opposite() : currentSide;
                    districtsBySideOfRoad[currentRoad][currentSide] = district;
                    Debug.Log($"District {string.Join(" -> ", district.Nodes.Select(node => node.Name))} is on the {currentSide} of {currentRoad.Name}.");
                }

                return district;
            }

            // We haven't travelled the connecting road yet, so continue building the district along it.
            return BuildDistrict(b, connectingNode, forwards, backwards, found);
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

        public IEnumerable<IRoad> ConnectingRoads(IEnumerable<INode> nodes)
        {
            var enumerator = nodes.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            var a = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var b = enumerator.Current;
                yield return ConnectingRoad(a, b);
                a = b;
            }
        }

        public IRoute ConnectedDistrict(IRoad road, Side side)
        {
            return districtsBySideOfRoad[road][side];
        }
    }
}