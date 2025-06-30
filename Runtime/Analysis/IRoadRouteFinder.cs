using Districts.Model;
using System.Collections.Generic;

namespace Districts.Analysis
{
    /// <summary>
    /// Describes a system for finding routes between nodes.
    /// </summary>
    public interface IRoadRouteFinder
    {
        /// <summary>
        /// Find all routes between two nodes, optionally within a maximum number of nodes.
        /// </summary>
        ICollection<IRoadRoute> AllRoutes(IRoadNode a, IRoadNode b, int maxNodes = int.MaxValue);
    }
}