using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;

public class ScoreUI : MonoBehaviour
{
    public Text scoreText;

    private EntityManager em;
    private Entity scoreEntity;

    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(Score));
        
        if (query.CalculateEntityCount() > 0) 
            scoreEntity = query.GetSingletonEntity();
        else
            Debug.LogWarning("No Score entity found in the scene.");
    }

    void Update()
    {
        if (em.Exists(scoreEntity))
        {
            var score = em.GetComponentData<Score>(scoreEntity).Value;
            scoreText.text = $"Score: {score}";
        }
    }
}