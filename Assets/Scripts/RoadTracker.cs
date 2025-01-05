using RoadPlanning;
using System.Collections.Generic;
using System.Linq;
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
    private IEnumerable<INode> currentDistrict;

    private void Start()
    {
        var roadPlanner = FindFirstObjectByType<PlanManager>();
        plan = roadPlanner.Plan;
        InitialiseClosestRoad();
    }

    private void OnDrawGizmosSelected()
    {
        if (plan == null)
        {
            return;
        }

        var closestRoad = ClosestRoad();
        var closestPoint = closestRoad.ClosestPoint(transform.position);
        var side = closestRoad.SideOfPoint(transform.position);
            
        Gizmos.color = side == Side.Left ? Color.red : Color.blue;
        Gizmos.DrawLine(transform.position, closestPoint);

        Gizmos.color = Color.green;
        var district = plan.AdjacentDistrict(closestRoad, side);
        foreach (var road in plan.ConnectingRoads(district))
        {
            Gizmos.DrawLine(road.Start.Position, road.End.Position);
        }

        Gizmos.color = Color.white;
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
        currentDistrict = plan.AdjacentDistrict(road, side);
        Debug.Log($"Initialised district: {IPlan.RouteToString(currentDistrict)}");

        return road;
    }

    /// <summary>
    /// Get the closest road to the current position.
    /// </summary
    public IRoad ClosestRoad()
    {
        // Get the closest road in the cached district to our position.
        var road = plan.ClosestRoad(transform.position, currentDistrict);
        
        // Determine which side of the road we are on.
        var side = road.SideOfPoint(transform.position);

        // Get the expected district on this side of the road.
        var district = plan.AdjacentDistrict(road, side);

        // Are we still in the cached district?
        if (district == currentDistrict)
        {
            // We are still in the cached district, return the closest road.
            return road;
        }

        // We have crossed to the opposite side of the road, so cache the district on this side.
        currentDistrict = district;

        // Is the road we found before still the closest road?
        if (road == plan.ClosestRoad(transform.position, district))
        {
            // We have crossed into the district.
            Debug.Log($"Crossed into district: {IPlan.RouteToString(district)}");
            return road;
        }

        // We have somehow teleported to a far away district, so reinitialise the closest road.
        return InitialiseClosestRoad();
    }
}