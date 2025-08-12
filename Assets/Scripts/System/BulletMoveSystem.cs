using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public partial struct BulletMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, velocity, entity) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<BulletVelocity>>()
                     .WithAll<ActiveBullet>().WithEntityAccess())
        {
            transform.ValueRW.Position += velocity.ValueRO.Value * dt;

            // 超出场景范围 → 回收
            if (math.lengthsq(transform.ValueRW.Position) > 10000f)
            {
                transform.ValueRW.Scale = 0f;
                transform.ValueRW.Position=Vector3.up*100f;
                ecb.RemoveComponent<ActiveBullet>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}