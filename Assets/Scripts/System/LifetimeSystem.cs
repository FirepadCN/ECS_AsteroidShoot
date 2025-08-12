using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct LifetimeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (life, entity) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
        {
            life.ValueRW.Seconds -= SystemAPI.Time.DeltaTime;
            if (life.ValueRO.Seconds <= 0f)
                ecb.DestroyEntity(entity);
        }
    }
}