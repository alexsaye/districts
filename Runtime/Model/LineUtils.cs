using UnityEngine;

namespace Saye.Districts.Model
{
    public static class LineUtils
    {
        /// <summary>
        /// Get whether the position is "above" (-1), "below" (1), or "on" (0) the primary axis when compared with the secondary axis.
        /// </summary>
        public static float SignOfPointOnAxis(Vector3 position, Vector3 primaryAxis, Vector3 secondaryAxis)
        {
            // Get the axis perpendicular to the primary and secondary axes.
            var tertiaryAxis = Vector3.Cross(secondaryAxis, primaryAxis);

            // Project the position onto the tertiary axis to see whether it is in the same direction.
            var projection = Vector3.Dot(position, tertiaryAxis);

            return Mathf.Sign(projection);
        }

        /// <summary>
        /// Get the closest point to the position on the line segment formed from the start to the end.
        /// </summary>
        public static Vector3 ClosestPoint(Vector3 position, Vector3 start, Vector3 end)
        {
            // Project the offset to the position onto the line.
            var lineVector = end - start;
            var offsetVector = position - start;
            var projection = Vector3.Dot(offsetVector, lineVector) / lineVector.sqrMagnitude;

            // If the projected point is before the start of the line segment, return the start.
            if (projection <= 0f)
            {
                return start;
            }

            // If the projected point is after the end of the line segment, return the end.
            if (projection >= 1f)
            {
                return end;
            }

            // Return the projected point along the line segment.
            return start + lineVector * projection;
        }
    }
}