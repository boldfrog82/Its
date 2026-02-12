using UnityEngine;

public class TrafficRuntimeMetrics : MonoBehaviour
{
    [SerializeField] private TrafficManager manager;
    [SerializeField] private float reportInterval = 5f;

    private float timer;

    private void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<TrafficManager>();
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < reportInterval)
        {
            return;
        }

        timer = 0f;

        int active = manager != null ? manager.ActiveCarCount : 0;
        float fps = 1f / Mathf.Max(0.0001f, Time.deltaTime);

        Debug.Log($"[TrafficRuntimeMetrics] ActiveCars={active}, FPS~={fps:0.0}, Time={Time.time:0.0}s");
    }
}
