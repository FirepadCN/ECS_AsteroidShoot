using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float DeltaTime;

    void Execute(MovementAspect aspect)
    {
        aspect.Move(DeltaTime);
    }
}

[BurstCompile]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new MoveJob { DeltaTime = SystemAPI.Time.DeltaTime }
            .ScheduleParallel();
    }
}