using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BulletPoolSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletPool>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var pool = SystemAPI.GetSingleton<BulletPool>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        for (int i = 0; i < pool.Size; i++)
        {
            var e = ecb.Instantiate(pool.Prefab);
            ecb.AddComponent<BulletTag>(e);
            ecb.SetComponent(e,LocalTransform.FromPositionRotationScale(new float3(0, 999, 0), quaternion.identity, 1.0f));
            // 默认不激活（无 ActiveBullet 组件）
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.Enabled = false; // 只初始化一次
    }
}