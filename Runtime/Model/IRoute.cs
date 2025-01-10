using System.Collections.Generic;
using System.Linq;

namespace Saye.Districts.Model
{
    /// <summary>
    /// Describes a route of nodes in order.
    /// </summary>
    public interface IRoute
    {
        IEnumerable<INode> Nodes { get; }

        /// <summary>
        /// Does a route end where it begins?
        /// </summary>
        static bool Cyclic(IRoute route)
        {
            return route.Nodes.First() == route.Nodes.Last();
        }
    }
}