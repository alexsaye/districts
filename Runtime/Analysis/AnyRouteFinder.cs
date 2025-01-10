using Saye.Districts.Model;
using System.Collections.Generic;
using System.Linq;

namespace Saye.Districts.Analysis
{
    /// <summary>
    /// Finds routes between nodes without checking conditions.
    /// </summary>
    public class AnyRouteFinder : IRouteFinder
    {
        private readonly IPlan plan;

        public AnyRouteFinder(IPlan plan)
        {
            this.plan = plan;
        }

        public ICollection<IRoute> AllRoutes(INode a, INode b, int maxNodes)
        {
            var routes = new List<IRoute>();
            BuildAllRoutes(a, b, new List<INode>(), new HashSet<IRoad>(), routes, maxNodes);
            return routes;
        }

        /// <summary>
        /// Perform a depth-first search to find all unique routes between two nodes, building up a found routes cache by caching the whole trace every time it finds the target node.
        /// </summary>
        private void BuildAllRoutes(INode current, INode target, IList<INode> trace, ISet<IRoad> travelled, ICollection<IRoute> routes, int maxNodes)
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
                        var nodes = new List<INode>(trace) { target };
                        var route = new Route(nodes);
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