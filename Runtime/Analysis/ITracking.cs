using Saye.Districts.Model;
using UnityEngine;

namespace Saye.Districts.Analysis
{
    /// <summary>
    /// Describes spatial tracking information within a plan.
    /// </summary>
    public interface ITracking
    {
        IRoute ClosestDistrict { get; }
        IRoad ClosestRoad { get; }
        Side ClosestSide { get; }
        Vector3 ClosestPoint { get; }
    }
}
