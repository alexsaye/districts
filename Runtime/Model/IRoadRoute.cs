using System.Collections.Generic;
using System.Linq;

namespace Districts.Model
{
    /// <summary>
    /// Describes a route of nodes in order.
    /// </summary>
    public interface IRoadRoute
    {
        IEnumerable<IRoadNode> Nodes { get; }

        /// <summary>
        /// Does a route end where it begins?
        /// </summary>
        static bool Cyclic(IRoadRoute route)
        {
            return route.Nodes.First() == route.Nodes.Last();
        }
    }
}