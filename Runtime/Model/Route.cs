using System.Collections.Generic;

namespace Saye.Districts.Model
{
    /// <summary>
    /// A route of nodes in order.
    /// </summary>
    public class Route : IRoute
    {
        public IEnumerable<INode> Nodes { get; private set; }

        public Route(IEnumerable<INode> nodes)
        {
            Nodes = nodes;
        }
    }
}