using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TrafficDensityJobSystem : MonoBehaviour
{
    [SerializeField] private Transform referencePoint;
    [SerializeField] private float nearDistance = 80f;
    [SerializeField] private float midDistance = 160f;

    public int ComputeCadence(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0 || referencePoint == null)
        {
            return 1;
        }

        NativeArray<Vector3> posArray = new NativeArray<Vector3>(positions, Allocator.TempJob);
        NativeArray<float> distArray = new NativeArray<float>(positions.Length, Allocator.TempJob);

        DistanceJob job = new DistanceJob
        {
            reference = referencePoint.position,
            positions = posArray,
            distances = distArray
        };

        JobHandle handle = job.Schedule(positions.Length, 32);
        handle.Complete();

        float avg = 0f;
        for (int i = 0; i < distArray.Length; i++)
        {
            avg += distArray[i];
        }

        avg /= Mathf.Max(1, distArray.Length);

        posArray.Dispose();
        distArray.Dispose();

        if (avg < nearDistance)
        {
            return 1;
        }

        if (avg < midDistance)
        {
            return 2;
        }

        return 3;
    }

    private struct DistanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> positions;
        [WriteOnly] public NativeArray<float> distances;
        public Vector3 reference;

        public void Execute(int index)
        {
            distances[index] = Vector3.Distance(reference, positions[index]);
        }
    }
}
