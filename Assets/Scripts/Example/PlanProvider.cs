using RoadPlanning;
using UnityEngine;

public class PlanProvider : MonoBehaviour
{
    private IPlan plan;
    public IPlan Plan
    {
        get
        {
            if (plan == null)
            {
                plan = new Plan(GetComponentsInChildren<PlanBuilderNode>());
            }
            return plan;
        }
    }
}