using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GetTarget : MonoBehaviour
{
    public SpatialHash SpatialHash;
    public GameObject Prefab;
    public float NearbyRadius = 2;
    public int SpawnCount = 50;
    public int SpawnRadius = 5;
    private RaycastHit hit;
    private Ray ray;

    private List<Transform> OutCadicates = new List<Transform>();
    

    void Start()
    {
        StartCoroutine(LoadData());
        SpatialHash = new SpatialHash(2);
        Spawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            RayCheck();
    }

    void Spawn()
    {
        for (int i = 0; i < SpawnCount; i++)
        {
            int spawnX = Random.Range(-SpawnRadius, SpawnRadius);
            int spawnZ = Random.Range(-SpawnRadius, SpawnRadius);
            
            GameObject obj = Instantiate(Prefab, new Vector3(spawnX, 0, spawnZ), Quaternion.identity);
            SpatialHash.Register(obj.transform);
        }
    }

    void RayCheck()
    {
        ray= Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.green,3f);
        if (Physics.Raycast(ray, out hit))
        {
            var pos = hit.point;
            Debug.DrawLine(pos+Vector3.up*0.5f, pos, Color.red, 3f);
            SpatialHash.QueryNearbyNonAlloc(pos, NearbyRadius,OutCadicates);

            Transform nearest = OutCadicates.Count > 0? OutCadicates[0] : null;
            foreach (var o in OutCadicates)
            {
                if (nearest == null || Vector3.Distance(pos, o.position) < Vector3.Distance(pos, nearest.position))
                {
                    nearest = o;
                    
                }
            }

            if (nearest != null)
            {
                nearest.GetComponent<Renderer>().material.color = Color.red;
                Debug.DrawLine(pos, nearest.position, Color.red, 3f);
            }
            
            OutCadicates.Clear();
        }
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (SpatialHash == null) return;
        foreach (var kv in SpatialHash.grid)
        {
            var c = kv.Key;
            var center = new Vector3((c.x + 0.5f) * SpatialHash.cellSize, (c.y + 0.5f) * SpatialHash.cellSize, (c.z + 0.5f) * SpatialHash.cellSize);
            Gizmos.DrawWireCube(center, Vector3.one * SpatialHash.cellSize);
        }
    }
#endif

    [ContextMenu("Test Switch Main Thread")]
    void TestSwitchMainThread()
    {
        SynchronizationContext context = SynchronizationContext.Current;
        Task.Run(() =>
        {
            var result= Calculate();
            context.Post(_ =>
            {
                ApplyToUnityMainThread(result.Result);
            }, null);
        });
    }

    private void ApplyToUnityMainThread(int result)
    {
        transform.position = new Vector3(0, result, 0);
    }

    private async Task<int> Calculate()
    {
        await Task.Delay(5000);
        return 10;
    }
    
    IEnumerator LoadData() {
        var task = Task.Run(LoadFromDisk);
        yield return new WaitUntil(() => task.IsCompleted);
        Debug.Log(task.Result);
    }

    private async Task<string> LoadFromDisk()
    {
        await Task.Delay(3000);
        return "Data";
    }
}
