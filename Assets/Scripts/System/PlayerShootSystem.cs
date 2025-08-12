using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct PlayerShootSystem : ISystem
{
    private bool useLeftGun; // 左右枪切换

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 按下鼠标左键才开火
        if (!Input.GetMouseButtonDown(0))
            return;

        // 获取枪口数据（假设场景中只有一个玩家）
        if (!SystemAPI.TryGetSingleton(out GunPoints gunPoints))
            return;

        // 构建查询：找闲置子弹（有 BulletTag，没有 ActiveBullet）
        var query = SystemAPI.QueryBuilder()
            .WithAll<BulletTag>()
            .WithNone<ActiveBullet>()
            .Build();

        // 从查询结果中取闲置子弹
        var availableBullets = query.ToEntityArray(Allocator.Temp);
        if (availableBullets.Length > 0)
        {
            var bullet = availableBullets[0];
            var gunEntity = useLeftGun ? gunPoints.Left : gunPoints.Right;
            var gunTransform = SystemAPI.GetComponent<LocalToWorld>(gunEntity);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.SetComponent(bullet, LocalTransform.FromPositionRotationScale(
                gunTransform.Position+gunTransform.Forward*0.5f, gunTransform.Rotation, 0.2f));
            ecb.AddComponent(bullet, new BulletVelocity
            {
                Value = gunTransform.Forward * 50f
            });
            ecb.AddComponent<ActiveBullet>(bullet);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            useLeftGun = !useLeftGun;
        }
        availableBullets.Dispose(); // 记得释放 NativeArray

    }
}