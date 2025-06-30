using System.Collections.Generic;

namespace Districts.Model
{
    /// <summary>
    /// A route of nodes in order.
    /// </summary>
    public class RoadRoute : IRoadRoute
    {
        public IEnumerable<IRoadNode> Nodes { get; private set; }

        public RoadRoute(IEnumerable<IRoadNode> nodes)
        {
            Nodes = nodes;
        }
    }
}