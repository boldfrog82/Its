using System.Collections.Generic;
using UnityEngine;

public class TrafficSimulationScheduler : MonoBehaviour
{
    [SerializeField] private int maxCarsPerTick = 25;
    [SerializeField] private TrafficDensityJobSystem densityJobSystem;

    private readonly List<TrafficCar> registeredCars = new List<TrafficCar>();
    private int cursor;
    private int cadence = 1;

    public void Register(TrafficCar car)
    {
        if (car != null && !registeredCars.Contains(car))
        {
            registeredCars.Add(car);
        }
    }

    public void Unregister(TrafficCar car)
    {
        registeredCars.Remove(car);
    }

    private void LateUpdate()
    {
        if (densityJobSystem == null || registeredCars.Count == 0)
        {
            cadence = 1;
            return;
        }

        Vector3[] positions = new Vector3[registeredCars.Count];
        for (int i = 0; i < registeredCars.Count; i++)
        {
            positions[i] = registeredCars[i] != null ? registeredCars[i].transform.position : Vector3.zero;
        }

        cadence = Mathf.Max(1, densityJobSystem.ComputeCadence(positions));
    }

    public bool ShouldSimulateThisFrame(TrafficCar car)
    {
        if (cadence > 1 && Time.frameCount % cadence != 0)
        {
            return false;
        }

        if (registeredCars.Count <= maxCarsPerTick)
        {
            return true;
        }

        int idx = registeredCars.IndexOf(car);
        if (idx < 0)
        {
            return true;
        }

        int bucketStart = cursor;
        int bucketEnd = Mathf.Min(cursor + maxCarsPerTick, registeredCars.Count);
        bool active = idx >= bucketStart && idx < bucketEnd;

        if (car == registeredCars[registeredCars.Count - 1])
        {
            cursor += maxCarsPerTick;
            if (cursor >= registeredCars.Count)
            {
                cursor = 0;
            }
        }

        return active;
    }
}
