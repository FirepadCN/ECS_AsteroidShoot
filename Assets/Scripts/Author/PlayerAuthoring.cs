using Unity.Entities;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 10f;
    public Transform LeftGun;
    public Transform RightGun;
    public Transform Head;
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        //TransformUsageFlags.dynamic is used to create a dynamic entity
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        
        AddComponent(entity, new PlayerTag());
        AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
        AddComponent(entity, new GunPoints
        {
            Left = GetEntity(authoring.LeftGun.gameObject),
            Right = GetEntity(authoring.RightGun.gameObject),
            Head = GetEntity(authoring.Head.gameObject)
        });
    }
}

public struct PlayerTag : IComponentData {}
public struct GunPoints : IComponentData
{
    public Entity Left;
    public Entity Right;
    public Entity Head;
}