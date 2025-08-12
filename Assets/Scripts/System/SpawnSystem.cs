using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnSystem : ISystem
{
    // 系统初始化
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //只有存在SpawnSettings组件的实体才会被激活
        state.RequireForUpdate<SpawnSettings>();
    }

    // 系统更新
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<SpawnSettings>();
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var rnd = new Unity.Mathematics.Random(
            (uint)SystemAPI.Time.ElapsedTime * 1664525u + 1013904223u);// 随机种子

        for (int i = 0; i < settings.Count; i++)
        {
            // 实例化ECS实体预制体，发生在内存连续的ArchetypeChunk中
            var e = ecb.Instantiate(settings.Prefab);

            float3 pos = new float3(
                rnd.NextFloat(-settings.AreaSize.x * .5f, settings.AreaSize.x * .5f),
                0.0f,
                rnd.NextFloat(-settings.AreaSize.z * .5f, settings.AreaSize.z * .5f));
            float3 dir = math.normalize(rnd.NextFloat3Direction());
            dir.y = 0;

            ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(
                pos, quaternion.LookRotationSafe(dir, math.up()), 1f));

            ecb.AddComponent(e, new Heading { Value = dir });
            ecb.AddComponent(e, new MoveSpeed { Value = rnd.NextFloat(2f, 8f) });
            ecb.AddComponent(e, new Lifetime { Seconds = rnd.NextFloat(20f, 45f) });
            ecb.AddComponent(e,new AsteroidTag());
        }

        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        // 只生成一次
        state.Enabled = false;
    }
}