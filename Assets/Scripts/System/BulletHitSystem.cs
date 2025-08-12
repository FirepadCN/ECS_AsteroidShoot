using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public partial struct BulletHitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (bulletTransform, bulletEntity) in 
                 SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<ActiveBullet, BulletTag>()
                     .WithEntityAccess())
        {
            foreach (var (asteroidTransform, asteroidEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<AsteroidTag>()
                         .WithEntityAccess())
            {
                var bulletPos = SystemAPI.GetComponent<LocalToWorld>(bulletEntity).Position;
                var asteroidPos = SystemAPI.GetComponent<LocalToWorld>(asteroidEntity).Position;
                float dist = math.distance(bulletPos,
                    asteroidPos);

                if (dist < 1.0f)
                {
                    ecb.DestroyEntity(asteroidEntity);
                    ecb.RemoveComponent<ActiveBullet>(bulletEntity);
                    ecb.SetComponent(bulletEntity,LocalTransform
                        .FromPositionRotationScale(new float3(0,1000f,0)
                            ,quaternion.identity,1f));
                    // 分数累加：让 Score +1（假设全局唯一 Score 组件）
                    if (SystemAPI.TryGetSingletonEntity<Score>(out Entity scoreEntity))
                    {
                        int cur = SystemAPI.GetComponent<Score>(scoreEntity).Value;
                        ecb.SetComponent(scoreEntity, new Score { Value = cur + 1 });
                    }

                    break;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}