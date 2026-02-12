using UnityEngine;
#if UNITY_ENTITIES
using Unity.Entities;
using Unity.Mathematics;
#endif

public class TrafficEcsBridge : MonoBehaviour
{
    [SerializeField] private bool useEcsSimulation;
    [SerializeField] private int ecsBatchSize = 256;

    public bool UseEcsSimulation => useEcsSimulation;
    public int EcsBatchSize => ecsBatchSize;
}

#if UNITY_ENTITIES
public struct TrafficCarData : IComponentData
{
    public float Speed;
    public float DesiredSpeed;
    public float3 Position;
}

public class TrafficEcsBridgeBaker : Baker<TrafficEcsBridge>
{
    public override void Bake(TrafficEcsBridge authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TrafficCarData
        {
            Speed = 0f,
            DesiredSpeed = 0f,
            Position = float3.zero
        });
    }
}
#endif
