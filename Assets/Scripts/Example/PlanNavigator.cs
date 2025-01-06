using RoadPlanning;
using UnityEngine;

public class PlanNavigator : MonoBehaviour
{
    private PlanProvider provider;
    private PlanTracker tracker;

    private void Start()
    {
        provider = FindFirstObjectByType<PlanProvider>();
        tracker = new PlanTracker(provider.Plan, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (tracker != null)
        {
            tracker.DrawGizmos(transform.position);
        }
    }
}
