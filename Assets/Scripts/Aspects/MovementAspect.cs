using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

//Aspect is a struct that contains the data required to perform a specific action on an entity.
public readonly partial struct MovementAspect : IAspect
{
    //RefRW is a reference type that allows us to read and write to the entity's components.
    public readonly RefRW<LocalTransform> Transform;
    
    //RefRO is a reference type that allows us to read the entity's components.
    public readonly RefRO<MoveSpeed> Speed;
    public readonly RefRO<Heading> Dir;

    public void Move(float dt)
    {
        //We use the ValueRW property to read and write to the entity's components.
        var t = Transform.ValueRW;
        t.Position += Dir.ValueRO.Value * Speed.ValueRO.Value * dt;
        Transform.ValueRW = t;
    }
}