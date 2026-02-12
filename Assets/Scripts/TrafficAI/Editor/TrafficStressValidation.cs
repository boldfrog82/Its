#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TrafficStressValidation
{
    [MenuItem("Tools/TrafficAI/Run Stress Validation")]
    public static void RunStressValidation()
    {
        TrafficCar[] cars = Object.FindObjectsOfType<TrafficCar>();
        TrafficIntersection[] intersections = Object.FindObjectsOfType<TrafficIntersection>();
        TrafficLane[] lanes = Object.FindObjectsOfType<TrafficLane>();
        TrafficDestination[] destinations = Object.FindObjectsOfType<TrafficDestination>();
        TrafficRoutePlanner[] planners = Object.FindObjectsOfType<TrafficRoutePlanner>();

        int activeCars = 0;
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] != null && cars[i].isActiveAndEnabled)
            {
                activeCars++;
            }
        }

        Debug.Log($"[TrafficStressValidation] Cars={cars.Length}, ActiveCars={activeCars}, Intersections={intersections.Length}, Lanes={lanes.Length}, Destinations={destinations.Length}, Planners={planners.Length}");
        Debug.Log("[TrafficStressValidation] Static validation done. Next step: run Play Mode with Profiler Timeline/Physics and traffic stress runner.");
    }
}
#endif
