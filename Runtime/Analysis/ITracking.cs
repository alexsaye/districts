using Districts.Model;
using UnityEngine;

namespace Districts.Analysis
{
    /// <summary>
    /// Describes spatial tracking information within a plan.
    /// </summary>
    public interface ITracking
    {
        Vector3 Position { get; }
        IRoute ClosestDistrict { get; }
        IRoad ClosestRoad { get; }
        Side ClosestSide { get; }
        Vector3 ClosestPoint { get; }
    }
}
