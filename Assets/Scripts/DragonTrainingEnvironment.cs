using UnityEngine;
using System.Collections.Generic;

public class DragonTrainingEnvironment : MonoBehaviour
{
    [Header("Environment Bounds")]
    [SerializeField] private Vector3 environmentSize = new Vector3(100f, 50f, 100f);
    [SerializeField] private float minAltitude = 5f;
    [SerializeField] private float maxAltitude = 45f;

    [Header("Target Settings")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Transform target;
    [SerializeField] private float targetMinDistance = 30f;
    [SerializeField] private float targetMaxDistance = 70f;

    [Header("Obstacle Settings")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private int minObstacles = 3;
    [SerializeField] private int maxObstacles = 8;
    [SerializeField] private float obstacleMinSize = 2f;
    [SerializeField] private float obstacleMaxSize = 8f;
    [SerializeField] private float obstacleAvoidanceRadius = 10f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform dragonSpawnPoint;
    [SerializeField] private float spawnHeight = 10f;

    [Header("Obstacle Course Settings")]
    [SerializeField] private bool useObstacleCourse = false;
    [SerializeField] private float courseLength = 80f;
    [SerializeField] private int numberOfSections = 5;
    [SerializeField] private float sectionSpacing = 15f;

    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<Collider> obstacleColliders = new List<Collider>();

    private void Start()
    {
        Application.runInBackground = true;

        if (target == null && targetPrefab != null)
        {
            GameObject targetObj = Instantiate(targetPrefab, transform);
            target = targetObj.transform;
        }

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            CreateDefaultObstaclePrefabs();
        }

        if (useObstacleCourse)
        {
            CreateObstacleCourse();
        }
    }

    [ContextMenu("Reset")]
    public void ResetEnvironment()
    {
        ClearObstacles();

        if (useObstacleCourse)
        {
            CreateObstacleCourse();
        }
        else
        {
            RandomizeTarget();
            SpawnObstacles();
        }
    }

    public void CreateObstacleCourse()
    {
        ClearObstacles();

        Vector3 startPos = dragonSpawnPoint != null ? dragonSpawnPoint.localPosition : Vector3.zero;
        Vector3 currentPos = startPos;

        for (int section = 0; section < numberOfSections; section++)
        {
            currentPos += Vector3.forward * sectionSpacing;

            switch (section % 5)
            {
                case 0:
                    CreateRingSection(currentPos);
                    break;
                case 1:
                    CreateSlalomSection(currentPos);
                    break;
                case 2:
                    CreateTunnelSection(currentPos);
                    break;
                case 3:
                    CreatePillarMazeSection(currentPos);
                    break;
                case 4:
                    CreateWaveSection(currentPos);
                    break;
            }
        }

        Vector3 targetPos = currentPos + Vector3.forward * sectionSpacing;
        targetPos.y = Random.Range(minAltitude + 5f, maxAltitude - 5f);

        if (target != null)
        {
            target.localPosition = targetPos;

            if (target.GetComponent<Renderer>() != null)
            {
                target.GetComponent<Renderer>().material.color = Color.green;
            }
        }

        Debug.Log($"Obstacle Course Created: {activeObstacles.Count} obstacles across {numberOfSections} sections");
    }

    private void CreateRingSection(Vector3 centerPos)
    {
        int ringSegments = 8;
        float ringRadius = 8f;
        float height = Random.Range(minAltitude + 5f, maxAltitude - 5f);

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
            Vector3 pos = centerPos + new Vector3(
                Mathf.Cos(angle) * ringRadius,
                height,
                Mathf.Sin(angle) * ringRadius
            );

            SpawnObstacle(pos, Vector3.one * 2f);
        }
    }

    private void CreateSlalomSection(Vector3 startPos)
    {
        int pillars = 4;
        float spacing = 3f;
        float offset = 5f;

        for (int i = 0; i < pillars; i++)
        {
            float side = (i % 2 == 0) ? offset : -offset;
            Vector3 pos = startPos + new Vector3(
                side,
                Random.Range(minAltitude + 3f, maxAltitude - 3f),
                i * spacing
            );

            SpawnObstacle(pos, new Vector3(3f, 6f, 3f));
        }
    }

    private void CreateTunnelSection(Vector3 startPos)
    {
        int segments = 3;
        float tunnelWidth = 12f;
        float tunnelHeight = 10f;

        for (int i = 0; i < segments; i++)
        {
            Vector3 segmentPos = startPos + Vector3.forward * (i * 4f);
            float height = (minAltitude + maxAltitude) / 2f;

            SpawnObstacle(segmentPos + new Vector3(-tunnelWidth / 2, height, 0), new Vector3(2f, tunnelHeight, 3f));
            SpawnObstacle(segmentPos + new Vector3(tunnelWidth / 2, height, 0), new Vector3(2f, tunnelHeight, 3f));
            SpawnObstacle(segmentPos + new Vector3(0, height + tunnelHeight / 2, 0), new Vector3(tunnelWidth, 2f, 3f));
            SpawnObstacle(segmentPos + new Vector3(0, height - tunnelHeight / 2, 0), new Vector3(tunnelWidth, 2f, 3f));
        }
    }

    private void CreatePillarMazeSection(Vector3 centerPos)
    {
        int gridSize = 3;
        float spacing = 5f;
        float height = Random.Range(minAltitude + 5f, maxAltitude - 5f);

        for (int x = -gridSize / 2; x <= gridSize / 2; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (Random.value > 0.6f) continue;

                Vector3 pos = centerPos + new Vector3(
                    x * spacing,
                    height + Random.Range(-3f, 3f),
                    z * spacing
                );

                SpawnObstacle(pos, new Vector3(2f, 8f, 2f));
            }
        }
    }

    private void CreateWaveSection(Vector3 startPos)
    {
        int obstacles = 6;
        float waveAmplitude = 6f;
        float waveFrequency = 1f;

        for (int i = 0; i < obstacles; i++)
        {
            float t = i / (float)obstacles;
            float x = Mathf.Sin(t * Mathf.PI * 2f * waveFrequency) * waveAmplitude;
            float y = Mathf.Cos(t * Mathf.PI * waveFrequency) * 4f + (minAltitude + maxAltitude) / 2f;

            Vector3 pos = startPos + new Vector3(
                x,
                y,
                i * 3f
            );

            SpawnObstacle(pos, Vector3.one * 3f);
        }
    }

    private void SpawnObstacle(Vector3 position, Vector3 scale)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacle = Instantiate(prefab, transform);
        obstacle.transform.localPosition = position;
        obstacle.transform.localScale = scale;
        obstacle.transform.rotation = Random.rotation;

        Collider col = obstacle.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        obstacle.tag = "Obstacle";

        activeObstacles.Add(obstacle);
        obstacleColliders.AddRange(obstacle.GetComponentsInChildren<Collider>());
    }

    private void RandomizeTarget()
    {
        if (target == null) return;

        float distance = Random.Range(targetMinDistance, targetMaxDistance);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float height = Random.Range(minAltitude, maxAltitude);

        Vector3 randomPos = new Vector3(
            Mathf.Cos(angle) * distance,
            height,
            Mathf.Sin(angle) * distance
        );

        target.localPosition = randomPos;

        if (target.GetComponent<Renderer>() != null)
        {
            target.GetComponent<Renderer>().material.color = Color.green;
        }
    }

    private void SpawnObstacles()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        int obstacleCount = Random.Range(minObstacles, maxObstacles + 1);

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 randomPos = GetValidObstaclePosition();

            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            GameObject obstacle = Instantiate(prefab, transform);
            obstacle.transform.localPosition = randomPos;
            obstacle.transform.rotation = Random.rotation;

            float scale = Random.Range(obstacleMinSize, obstacleMaxSize);
            obstacle.transform.localScale = Vector3.one * scale;

            Collider col = obstacle.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            activeObstacles.Add(obstacle);
            obstacleColliders.AddRange(obstacle.GetComponentsInChildren<Collider>());
        }
    }

    private Vector3 GetValidObstaclePosition()
    {
        int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-environmentSize.x / 2, environmentSize.x / 2),
                Random.Range(minAltitude, maxAltitude),
                Random.Range(-environmentSize.z / 2, environmentSize.z / 2)
            );

            if (dragonSpawnPoint != null &&
                Vector3.Distance(randomPos, dragonSpawnPoint.localPosition) < 15f)
                continue;

            if (target != null &&
                Vector3.Distance(randomPos, target.localPosition) < obstacleAvoidanceRadius)
                continue;

            bool tooClose = false;
            foreach (GameObject obstacle in activeObstacles)
            {
                if (Vector3.Distance(randomPos, obstacle.transform.localPosition) < obstacleAvoidanceRadius)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) return randomPos;
        }

        return new Vector3(0, Random.Range(minAltitude, maxAltitude), 0);
    }

    private void ClearObstacles()
    {
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }

        activeObstacles.Clear();
        obstacleColliders.Clear();
    }

    public bool CheckCollision(Vector3 position, float radius = 1f)
    {
        foreach (Collider col in obstacleColliders)
        {
            if (col == null) continue;

            if (col.bounds.Intersects(new Bounds(position, Vector3.one * radius * 2)))
            {
                return true;
            }
        }

        return false;
    }

    public Transform GetTarget()
    {
        return target;
    }

    public Vector3 GetNearestObstacleDirection(Vector3 fromPosition, float detectionRadius)
    {
        Vector3 avoidanceDirection = Vector3.zero;
        float closestDistance = detectionRadius;

        foreach (Collider col in obstacleColliders)
        {
            if (col == null) continue;

            Vector3 toObstacle = col.bounds.center - fromPosition;
            float distance = toObstacle.magnitude;

            if (distance < detectionRadius)
            {
                float weight = 1f - (distance / detectionRadius);
                avoidanceDirection -= toObstacle.normalized * weight;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
        }

        return avoidanceDirection.normalized;
    }

    private void CreateDefaultObstaclePrefabs()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.GetComponent<Renderer>().material.color = Color.red;
        cube.name = "ObstacleCube";

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.GetComponent<Renderer>().material.color = Color.red;
        sphere.name = "ObstacleSphere";

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.GetComponent<Renderer>().material.color = Color.red;
        cylinder.name = "ObstacleCylinder";

        obstaclePrefabs = new GameObject[] { cube, sphere, cylinder };

        cube.SetActive(false);
        sphere.SetActive(false);
        cylinder.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, environmentSize);

        if (dragonSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(dragonSpawnPoint.position, 2f);
        }

        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, 3f);
        }
    }
}