using Unity.Entities;

//Baker is a generic class that takes a component data authoring
//class as a parameter and provides a Bake method to create an entity with the component data.
public class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        //TransformUsageFlags.None means that the entity will not be parented to any other entity.
        var entity = GetEntity(TransformUsageFlags.None);
        //TransformUsageFlags.Dynamic means that the entity will be parented to another entity.
        var prefabEntity = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);

        AddComponent(entity, new SpawnSettings {
            Prefab = prefabEntity,
            Count = authoring.Count,
            AreaSize = authoring.AreaSize
        });
    }
}