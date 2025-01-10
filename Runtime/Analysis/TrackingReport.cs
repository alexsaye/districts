using Saye.Districts.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Saye.Districts.Analysis
{
    /// <summary>
    /// A report of tracking information within a plan.
    /// </summary>
    public class TrackingReport : ITracking
    {
        public Vector3 Position { get; private set; }
        public IRoute ClosestDistrict { get; private set; }
        public IRoad ClosestRoad { get; private set; }
        public Side ClosestSide { get; private set; }
        public Vector3 ClosestPoint { get; private set; }

        public TrackingReport(Vector3 position, IPlan plan) : this(position, plan, plan.Roads) { }

        public TrackingReport(Vector3 position, IPlan plan, IEnumerable<INode> nodes) : this(position, plan, plan.ConnectingRoads(nodes)) { }

        public TrackingReport(Vector3 position, IPlan plan, IEnumerable<IRoad> roads)
        {
						Position = position;

            // Find the closest road and the closest point on that road.
            var closestSqrDistance = float.PositiveInfinity;
            foreach (var road in roads)
            {
                var point = road.ClosestPoint(position);
                var sqrDistance = Vector3.SqrMagnitude(point - position);
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    ClosestRoad = road;
                    ClosestPoint = point;
                }
            }

            // Determine which side of the road we are on and its connected district.
            ClosestSide = ClosestRoad.SideOfPoint(position);
            ClosestDistrict = plan.ConnectedDistrict(ClosestRoad, ClosestSide);
        }
    }
}