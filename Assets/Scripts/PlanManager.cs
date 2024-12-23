using UnityEngine;
using RoadPlanning;

public class PlanManager : MonoBehaviour
{
    private IPlan plan;
    public IPlan Plan
    {
        get
        {
            if (plan == null)
            {
                plan = new Plan(GetComponentsInChildren<RoadBuilder>());
            }
            return plan;
        }
    }
}