#if UNITY_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TrafficEcsRuntimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TrafficCarData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (RefRW<TrafficCarData> car in SystemAPI.Query<RefRW<TrafficCarData>>())
        {
            float target = car.ValueRO.DesiredSpeed;
            float speed = math.lerp(car.ValueRO.Speed, target, math.saturate(dt * 2f));
            float3 pos = car.ValueRO.Position;
            pos += new float3(0f, 0f, speed * dt);

            car.ValueRW.Speed = speed;
            car.ValueRW.Position = pos;
        }
    }
}
#endif
