using RoadPlanning;
using UnityEngine;

public class RoadTracker : MonoBehaviour
{
    /// <summary>
    /// The road plan to track within.
    /// </summary>
    private IPlan plan;

    /// <summary>
    /// The current district that the tracker is in. Cached for performant closest road lookup, as only the roads within the district need to be considered in most cases.
    /// </summary>
    private IDistrict district;

    private void Start()
    {
        var roadPlanner = FindFirstObjectByType<PlanManager>();
        plan = roadPlanner.Plan;
        InitialiseClosestRoad();
    }

    private void Update()
    {
        var road = ClosestRoad();
        Debug.Log($"Closest road: {road.Name}");
        Debug.Log($"Current side of road: {road.SideOfPoint(transform.position)}");
        if (district != null)
        {
            Debug.Log($"Current district: {district.Name}");
        }
    }

    private void OnDrawGizmos()
    {
        if (plan != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, ClosestRoad().ClosestPoint(transform.position));
            Gizmos.color = Color.white;
        }
    }

    /// <summary>
    /// Initialise the closest road and district by searching the whole plan.
    /// </summary>
    private IRoad InitialiseClosestRoad()
    {
        // Get the closest road in the whole plan.
        var road = plan.ClosestRoad(transform.position);
        Debug.Log($"Initialised closest road: {road.Name}");

        // Determine which side of the road we are on.
        var side = road.SideOfPoint(transform.position);
        Debug.Log($"Initialised side of road: {side}");

        // Cache the district on this side of the road.
        district = plan.AdjacentDistrict(side, road);
        Debug.Log($"Initialised district: {district.Name}");

        return road;
    }

    /// <summary>
    /// Get the closest road to the current position.
    /// </summary
    public IRoad ClosestRoad()
    {
        // Get the closest road in the cached district to our position.
        var road = plan.ClosestRoad(transform.position, district);
        
        // Determine which side of the road we are on.
        var side = road.SideOfPoint(transform.position);

        // Are we still on the side that the cached district expects for the road?
        if (side == district.Sides[road])
        {
            // We are still in the cached district, return the closest road.
            return road;
        }

        // We have crossed to the opposite side of the road, so get and cache the district on this side.
        district = plan.AdjacentDistrict(side, road);

        // Is the road we found before still the closest road?
        if (road == plan.ClosestRoad(transform.position, district))
        {
            // We have crossed into the district.
            Debug.Log($"Crossed into district: {district.Name}");
            return road;
        }

        // We have somehow teleported to a far away district, so reinitialise the closest road.
        return InitialiseClosestRoad();
    }
}