using Saye.Districts.Model;
using System.Collections.Generic;

namespace Saye.Districts.Analysis
{
    /// <summary>
    /// Describes a system for finding routes between nodes.
    /// </summary>
    public interface IRouteFinder
    {
        /// <summary>
        /// Find all routes between two nodes, optionally within a maximum number of nodes.
        /// </summary>
        ICollection<IRoute> AllRoutes(INode a, INode b, int maxNodes = int.MaxValue);
    }
}