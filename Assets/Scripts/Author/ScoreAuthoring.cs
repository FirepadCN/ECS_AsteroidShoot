using UnityEngine;
using Unity.Entities;

public class ScoreAuthoring : MonoBehaviour {}

public class ScoreBaker : Baker<ScoreAuthoring>
{
    public override void Bake(ScoreAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Score { Value = 0 });
    }
}
