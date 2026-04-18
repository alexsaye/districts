using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Districts.Model
{
    /// <summary>
    /// A plan of nodes which form roads and districts.
    /// </summary>
    public class RoadPlan : IRoadPlan
    {
        private readonly Dictionary<IRoadNode, Dictionary<IRoadNode, IRoad>> roadsByNode;
        private readonly Dictionary<IRoadRoute, HashSet<IRoad>> roadsByContainingDistrict;

        private readonly HashSet<IRoadRoute> districts;
        private readonly Dictionary<IRoad, Dictionary<RoadSide, IRoadRoute>> districtsBySideOfRoad;
        private readonly Dictionary<IRoadNode, HashSet<IRoadRoute>> districtsByNode;

        private readonly HashSet<IRoadRoute> deadEndRoutes;
        private readonly HashSet<IRoadNode> deadEndNodes;

        public IEnumerable<IRoadNode> Nodes => roadsByNode.Keys;

        public IEnumerable<IRoad> Roads => districtsBySideOfRoad.Keys;

        public IEnumerable<IRoadRoute> Districts => districts;

        public IEnumerable<IRoadNode> DeadEndNodes => deadEndNodes;

        public IEnumerable<IRoadRoute> DeadEndRoutes => deadEndRoutes;

        public RoadPlan(IEnumerable<IRoadBuilderNode> graph) : this(IRoadBuilderNode.Build(graph)) { }

        public RoadPlan(IDictionary<IRoadNode, IDictionary<IRoadNode, IRoad>> graph)
        {
            // Deep copy the road graph.
            roadsByNode = new Dictionary<IRoadNode, Dictionary<IRoadNode, IRoad>>();
            foreach (var (node, roads) in graph)
            {
                roadsByNode.Add(node, new Dictionary<IRoadNode, IRoad>(roads));
            }

            // Build the dead-end nodes by repeatedly walking back from nodes that lead to only one other node.
            deadEndNodes = Nodes.Where(node => roadsByNode[node].Count == 1).ToHashSet();

            var deadEndEntryNodes = new HashSet<IRoadNode>();
            var deadEndNodesCurrentPass = new HashSet<IRoadNode>(deadEndNodes);
            var deadEndNodesLastPass = new HashSet<IRoadNode>();
            do
            {
                // Move the current pass to the last pass and clear the current pass for the upcoming walk.
                (deadEndNodesLastPass, deadEndNodesCurrentPass) = (deadEndNodesCurrentPass, deadEndNodesLastPass);
                deadEndNodesCurrentPass.Clear();

                // Walk back further from the dead-end nodes from the last pass.
                foreach (var deadEndNode in deadEndNodesLastPass)
                {
                    var connectedNodes = ConnectedNodes(deadEndNode).Where(node => !deadEndNodes.Contains(node)).ToList();
                    if (connectedNodes.Count == 0) continue;

                    var connectedNode = connectedNodes.First();
                    if (ConnectedNodes(connectedNode).Count(node => !deadEndNodes.Contains(node)) > 1)
                    {
                        // If the single next non-dead-end connected node leads to more than one non-dead-end road, it might be an entry node into the dead-end.
                        deadEndEntryNodes.Add(connectedNode);
                    }
                    else
                    {
                        // If the single next non-dead-end connected node only leads to one non-dead-end road then it becomes a dead-end node.
                        deadEndNodesCurrentPass.Add(connectedNode);
                        deadEndNodes.Add(connectedNode);

                        // If in a previous pass it was marked as an entry node (i.e. from a shorter route to a dead-end), unmark it as an entry node.
                        deadEndEntryNodes.Remove(connectedNode);
                    }
                }
            } while (deadEndNodesCurrentPass.Count != 0);

            // Build the dead-ends from the dead-end entry nodes by traversing each dead-end node until there are no further connections.
            deadEndRoutes = new HashSet<IRoadRoute>();
            foreach (var deadEndEntryNode in deadEndEntryNodes)
            {
                var connectedDeadEndNodes = ConnectedNodes(deadEndEntryNode).Where(node => deadEndNodes.Contains(node)).ToList();
                foreach (var connectedDeadEndNode in connectedDeadEndNodes)
                {
                    BuildDeadEnds(connectedDeadEndNode, new List<IRoadNode> { deadEndEntryNode }, deadEndRoutes);
                }
            }

            // Build the districts by traversing each road forwards and backwards until we cover all roads in both directions with non-overlapping cycles.
            districts = new HashSet<IRoadRoute>();
            districtsBySideOfRoad = new Dictionary<IRoad, Dictionary<RoadSide, IRoadRoute>>();
            foreach (var road in graph.Values.SelectMany(connections => connections.Values))
            {
                districtsBySideOfRoad[road] = new Dictionary<RoadSide, IRoadRoute>();
            }
            districtsByNode = new Dictionary<IRoadNode, HashSet<IRoadRoute>>();
            foreach (var node in graph.Keys)
            {
                districtsByNode[node] = new HashSet<IRoadRoute>();
            }
            roadsByContainingDistrict = new Dictionary<IRoadRoute, HashSet<IRoad>>();
            var forwards = new HashSet<IRoad>();
            var backwards = new HashSet<IRoad>();
            foreach (var road in Roads)
            {
                if (deadEndNodes.Contains(road.Start) || deadEndNodes.Contains(road.End))
                {
                    continue;
                }

                if (!forwards.Contains(road))
                {
                    var forwardsDistrict = BuildDistrict(road.Start, road.End, forwards, backwards, new List<IRoadNode>());
                    districts.Add(forwardsDistrict);
                    roadsByContainingDistrict.Add(forwardsDistrict, ConnectingRoads(forwardsDistrict.Nodes).ToHashSet());
                }

                if (!backwards.Contains(road))
                {
                    var backwardsDistrict = BuildDistrict(road.End, road.Start, forwards, backwards, new List<IRoadNode>());
                    districts.Add(backwardsDistrict);
                    roadsByContainingDistrict.Add(backwardsDistrict, ConnectingRoads(backwardsDistrict.Nodes).ToHashSet());
                }
            }

            // Determine the districts which dead-ends sit within by checking the districts connected to the entry nodes against the first dead-end nodes from that entry node.
            foreach (var deadEnd in deadEndRoutes)
            {
                var enumerator = deadEnd.Nodes.GetEnumerator();

                // Find the districts connected to the dead-end entry node.
                enumerator.MoveNext();
                var deadEndEntryNode = enumerator.Current;
                var potentialDistricts = ConnectedDistricts(deadEndEntryNode);

                // Find the district that the first dead-end node after the entry node sits within.
                enumerator.MoveNext();
                var deadEndNode = enumerator.Current;
                var district = ContainingDistrict(deadEndNode.Position, potentialDistricts);

                // Cache the district for the node and both sides of its road, as it is enclosed within a district.
                var prevNode = deadEndEntryNode;
                var currentNode = deadEndNode;
                var currentRoad = ConnectingRoad(prevNode, currentNode);
                districtsBySideOfRoad[currentRoad][RoadSide.Left] = district;
                districtsBySideOfRoad[currentRoad][RoadSide.Right] = district;
                districtsByNode[currentNode].Add(district);
                roadsByContainingDistrict[district].Add(currentRoad);

                // Propagate the district along the rest of the route.
                while (enumerator.MoveNext())
                {
                    prevNode = currentNode;
                    currentNode = enumerator.Current;
                    currentRoad = ConnectingRoad(prevNode, currentNode);
                    districtsBySideOfRoad[currentRoad][RoadSide.Left] = district;
                    districtsBySideOfRoad[currentRoad][RoadSide.Right] = district;
                    districtsByNode[currentNode].Add(district);
                    roadsByContainingDistrict[district].Add(currentRoad);
                }
            }
        }

        /// <summary>
        /// Build routes a dead-end node until there are no further connections.
        /// TODO: I don't like this but we can come back to it when it matters, important to get it working first.
        /// </summary>
        private void BuildDeadEnds(IRoadNode deadEndNode, List<IRoadNode> found, HashSet<IRoadRoute> routes)
        {
            // Add the current dead-end node.
            found.Add(deadEndNode);

            // Find further connected nodes, which will each lead to a single dead-end.
            var connectedNodes = ConnectedNodes(deadEndNode).Where(node => !found.Contains(node)).ToList();

            // If there are no more connected nodes, finally build the route from the found nodes.
            if (connectedNodes.Count == 0)
            {
                routes.Add(new RoadRoute(found));
                return;
            }

            // Continue building routes along each connected node to their separate dead-ends.
            foreach (var connectedNode in connectedNodes)
            {
                BuildDeadEnds(connectedNode, new List<IRoadNode>(found), routes);
            }
        }

        /// <summary>
        /// Build a district by traversing a road, populating the districts by side of road cache.
        /// </summary>
        private IRoadRoute BuildDistrict(IRoadNode a, IRoadNode b, HashSet<IRoad> forwards, HashSet<IRoad> backwards, List<IRoadNode> found)
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
            IRoadNode connectingNode = null;
            var rightestTurn = -float.PositiveInfinity;
            var roadDirection = Vector3.Normalize(b.Position - a.Position);
            foreach (var node in ConnectedNodes(b))
            {
                if (node == a)
                {
                    continue;
                }

                if (deadEndNodes.Contains(node))
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
                var district = new RoadRoute(found);

                // The current side of the road is right if we went forwards along it, left if we went backwards.
                var currentRoad = connectingRoad;
                var currentSide = b == currentRoad.Start ? RoadSide.Right : RoadSide.Left;
                districtsBySideOfRoad[currentRoad][currentSide] = district;
                districtsByNode[currentRoad.Start].Add(district);
                districtsByNode[currentRoad.End].Add(district);

                // Propagate the current side along the district, inverting it when roads converge or diverge. (Skip the first road as we've just cached its side.)
                var enumerator = ConnectingRoads(found).GetEnumerator();
                enumerator.MoveNext();
                while (enumerator.MoveNext())
                {
                    var previousRoad = currentRoad;
                    currentRoad = enumerator.Current;
                    currentSide = IRoad.AreConverging(currentRoad, previousRoad) || IRoad.AreDiverging(currentRoad, previousRoad) ? currentSide.Opposite() : currentSide;
                    districtsBySideOfRoad[currentRoad][currentSide] = district;
                    districtsByNode[currentRoad.Start].Add(district);
                    districtsByNode[currentRoad.End].Add(district);
                }

                return district;
            }

            // We haven't travelled the connecting road yet, so continue building the district along it.
            return BuildDistrict(b, connectingNode, forwards, backwards, found);
        }

        public IEnumerable<IRoadNode> ConnectedNodes(IRoadNode node)
        {
            return roadsByNode[node].Keys;
        }

        public IEnumerable<IRoad> ConnectedRoads(IRoadNode node)
        {
            return roadsByNode[node].Values;
        }

        public IRoad ConnectingRoad(IRoadNode a, IRoadNode b)
        {
            return roadsByNode[a][b];
        }

        public IEnumerable<IRoad> ConnectingRoads(IEnumerable<IRoadNode> nodes)
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

        public IRoadRoute ConnectedDistrict(IRoad road, RoadSide side)
        {
            return districtsBySideOfRoad[road][side];
        }

        public IEnumerable<IRoadRoute> ConnectedDistricts(IRoadNode node)
        {
            return districtsByNode[node];
        }

        public IRoadRoute ContainingDistrict(Vector3 position, IEnumerable<IRoadRoute> searchDistricts)
        {
            // Find the district where the position is on the matching side of most of its roads. (Not perfect, curse you concave districts... but it'll do.)
            return searchDistricts
                .OrderByDescending(district => ConnectingRoads(district.Nodes).Count(road => ConnectedDistrict(road, road.SideOfPoint(position)) == district))
                .First();
        }

        public IRoadRoute ContainingDistrict(Vector3 position) => ContainingDistrict(position, districts);

        public IEnumerable<IRoad> ContainedRoads(IRoadRoute district)
        {
            return roadsByContainingDistrict[district];
        }
    }
}