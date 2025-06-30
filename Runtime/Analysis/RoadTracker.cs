using Districts.Model;
using System.Linq;
using UnityEngine;

namespace Districts.Analysis
{
    /// <summary>
    /// Provides continuous tracking information within a plan, referencing previous tracking information to avoid unnecessary checks.
    /// </summary>
    public class RoadTracker : IRoadTracking
    {
        public readonly IRoadPlan Plan;

        public RoadTrackingReport Current { get; private set; }

        public Vector3 Position => Current.Position;

        public IRoadRoute ClosestDistrict => Current.ClosestDistrict;

        public IRoad ClosestRoad => Current.ClosestRoad;

        public RoadSide ClosestSide => Current.ClosestSide;

        public Vector3 ClosestPoint => Current.ClosestPoint;

        public RoadTracker(Vector3 position, IRoadPlan plan)
        {
            Plan = plan;
            Current = new RoadTrackingReport(position, plan);
        }

        /// <summary>
        /// Move the tracker to a new position, reporting tracking information.
        /// </summary
        public RoadTrackingReport Move(Vector3 position)
        {
            // Create a new report for the new position, but only against the nodes in the previous district.
            var previous = Current;
            Current = new RoadTrackingReport(position, Plan, previous.ClosestDistrict.Nodes);

            // Are we still in the same district?
            if (Current.ClosestDistrict == previous.ClosestDistrict)
            {
                return Current;
            }

            // Did we simply cross the road into the adjacent district?
            if (Current.ClosestRoad == previous.ClosestRoad)
            {
                Debug.Log($"Crossed into district: {string.Join(" -> ", Current.ClosestDistrict.Nodes.Select(node => node.Name))}");
                return Current;
            }

            // We have somehow teleported to a far away district, so create a new report against the whole plan.
            Current = new RoadTrackingReport(position, Plan);
            return Current;
        }
    }
}