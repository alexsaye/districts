using Districts.Model;
using System.Collections.Generic;
using System.Linq;

namespace Districts.Analysis
{
    /// <summary>
    /// Finds routes between nodes without checking conditions.
    /// </summary>
    public class AnyRoadRouteFinder : IRoadRouteFinder
    {
        private readonly IRoadPlan plan;

        public AnyRoadRouteFinder(IRoadPlan plan)
        {
            this.plan = plan;
        }

        public ICollection<IRoadRoute> AllRoutes(IRoadNode a, IRoadNode b, int maxNodes)
        {
            var routes = new List<IRoadRoute>();
            BuildAllRoutes(a, b, new List<IRoadNode>(), new HashSet<IRoad>(), routes, maxNodes);
            return routes;
        }

        /// <summary>
        /// Perform a depth-first search to find all unique routes between two nodes, building up a found routes cache by caching the whole trace every time it finds the target node.
        /// </summary>
        private void BuildAllRoutes(IRoadNode current, IRoadNode target, IList<IRoadNode> trace, ISet<IRoad> travelled, ICollection<IRoadRoute> routes, int maxNodes)
        {
            if (trace.Count == maxNodes)
            {
                return;
            }

            trace.Add(current);

            foreach (var connection in plan.ConnectedNodes(current))
            {
                // Don't go whence we came.
                if (connection == trace.ElementAtOrDefault(trace.Count - 2))
                {
                    continue;
                }

                var road = plan.ConnectingRoad(current, connection);

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
                    if (!routes.Any(existing => existing.Nodes.Count() == trace.Count - 1 && existing.Nodes.All(node => trace.Contains(node))))
                    {
                        var nodes = new List<IRoadNode>(trace) { target };
                        var route = new RoadRoute(nodes);
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