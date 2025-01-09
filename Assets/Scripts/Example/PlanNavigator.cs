using RoadPlanning;
using UnityEngine;

public class PlanNavigator : MonoBehaviour
{
    private PlanProvider provider;
    private Tracker tracker;

    private void Start()
    {
        provider = FindFirstObjectByType<PlanProvider>();
        tracker = new Tracker(transform.position, provider.Plan);
    }

    private void Update()
    {
        tracker.Move(transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (tracker == null)
        {
            return;
        }

        Gizmos.color = tracker.ClosestSide == Side.Left ? Color.red : Color.blue;
        Gizmos.DrawLine(tracker.ClosestPoint, tracker.Position);

        Gizmos.color = Color.green;
        foreach (var road in tracker.Plan.ConnectingRoads(tracker.ClosestDistrict.Nodes))
        {
            Gizmos.DrawLine(road.Start.Position, road.End.Position);
        }

        Gizmos.color = Color.white;
    }
}
