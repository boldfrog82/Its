using UnityEngine;
using Unity.Profiling;

public class TrafficPlaymodeStressRunner : MonoBehaviour
{
    [SerializeField] private TrafficManager manager;
    [SerializeField] private int targetActiveCars = 120;
    [SerializeField] private int maxSpawnsPerTick = 20;
    [SerializeField] private float warmupDelay = 2f;
    [SerializeField] private float rebalanceInterval = 1.5f;

    private static readonly ProfilerMarker StressTickMarker = new ProfilerMarker("TrafficStressRunner.Tick");

    private void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<TrafficManager>();
        }

        InvokeRepeating(nameof(RebalanceTrafficLoad), warmupDelay, rebalanceInterval);
    }

    private void RebalanceTrafficLoad()
    {
        using (StressTickMarker.Auto())
        {
            if (manager == null)
            {
                return;
            }

            int active = manager.ActiveCarCount;
            if (active >= targetActiveCars)
            {
                return;
            }

            int missing = Mathf.Min(maxSpawnsPerTick, targetActiveCars - active);
            for (int i = 0; i < missing; i++)
            {
                manager.ForceSpawnOnce();
            }
        }
    }
}
