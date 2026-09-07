using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    private enum RangeType
    {
        Box,
        Sphere
    }

    private enum SpawnType
    {
        Random,
        Fixed
    }

    private enum SpawnWhat
    {
        Assassin,
        Citizen
    }

    [SerializeField] private RangeType rangeType = RangeType.Box;

    [SerializeField] private float radius = 3f;

    [SerializeField] private GameObject[] npc;
    [SerializeField] private SpawnWhat spawnWhat = SpawnWhat.Citizen;

    [Header("Spwan")]
    [SerializeField] private SpawnType spawnType = SpawnType.Random;

    [SerializeField] private float randomin, randommax;
    [SerializeField] private float spawnTime;
    private float spawnTimer;

    [Header("Setting")]
    [SerializeField] private int spawnCount;

    [SerializeField] private int spawnNum;

    private void Start()
    {
    }

    private void FixedUpdate()
    {
        ItemSpawnFunc();
    }

    private void ItemSpawnFunc()
    {
        spawnTimer += Time.fixedDeltaTime;
        if (spawnTimer > spawnTime)
        {
            switch (spawnType)
            {
                case SpawnType.Fixed:
                    break;

                case SpawnType.Random:
                    spawnTime = Random.Range(randomin, randommax);
                    break;
            }
            spawnTimer = 0;
            switch (spawnWhat)
            {
                case SpawnWhat.Citizen:
                    Instantiate(npc[0], SpawnPosition(), Quaternion.identity);
                    break;

                case SpawnWhat.Assassin:
                    // taskManager.Current_Sum_EnemyNumberAdd();
                    if (spawnCount <= 0)
                    {
                        break;
                    }
                    spawnCount -= 1;
                    /// Instantiate(npc[0], SpawnPosition(), Quaternion.identity);
                    var clone = Instantiate(npc[0], SpawnPosition(), Quaternion.identity);
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (rangeType)
        {
            case RangeType.Box:
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, new Vector3(radius * 2, 0, radius * 2));
                break;

            case RangeType.Sphere:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, radius);
                break;
        }
    }

    private Vector3 SpawnPosition()
    {
        var pos = new Vector3();
        switch (rangeType)
        {
            case RangeType.Box:
                var xBox = transform.position.x + Random.Range(-radius, radius);
                //Random.InitState((int)xBox);
                var zBox = transform.position.z + Random.Range(-radius, radius);
                pos = new Vector3(xBox, transform.position.y, zBox);
                break;

            case RangeType.Sphere:
                var angle = Random.Range(-180f, 180f);
                var xSphere = transform.position.x + Mathf.Cos(angle) * Random.Range(-radius, radius);//Random.Range(-radius, radius);
                var zSphere = transform.position.z + Mathf.Sin(angle) * Random.Range(-radius, radius);//Random.Range(-radius, radius);
                pos = new Vector3(xSphere, transform.position.y, zSphere);
                break;
        }
        return pos;
    }
}