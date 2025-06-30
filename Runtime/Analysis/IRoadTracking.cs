using Districts.Model;
using UnityEngine;

namespace Districts.Analysis
{
    /// <summary>
    /// Describes spatial tracking information within a road plan.
    /// </summary>
    public interface IRoadTracking
    {
        Vector3 Position { get; }
        IRoadRoute ClosestDistrict { get; }
        IRoad ClosestRoad { get; }
        RoadSide ClosestSide { get; }
        Vector3 ClosestPoint { get; }
    }
}
