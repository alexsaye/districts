using System.Collections.Generic;

namespace Districts.Model
{
    /// <summary>
    /// Describes a node that can build roads to other nodes.
    /// </summary>
    public interface IRoadBuilderNode : INode
    {
        /// <summary>
        /// Directional forward connections to other nodes.
        /// </summary>
        public IEnumerable<IRoadBuilderNode> Connections { get; }

        /// <summary>
        /// Build a road between this node and another node.
        /// </summary>
        public IRoad Build(INode node);

        /// <summary>
        /// Build all roads as an undirected graph of nodes superimposed with a directed graph of roads.
        /// </summary>
        public static IDictionary<INode, IDictionary<INode, IRoad>> Build(IEnumerable<IRoadBuilderNode> nodes)
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
                    // Cache the road in both directions.
                    var road = node.Build(connection);
                    roads[node][connection] = road;
                    roads[connection][node] = road;
                }
            }
            return roads;
        }
    }
}