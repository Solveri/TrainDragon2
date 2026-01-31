using UnityEngine;

public class ObstacleCourseGenerator : MonoBehaviour
{
    [Header("Course Settings")]
    public GameObject obstaclePrefab;   // The object to spawn (Cube, Sphere, Wall)
    public int numberOfObstacles = 50;  // How many objects to spawn
    public float courseLength = 200f;   // How long the course is in meters
    public Vector2 tunnelSize = new Vector2(20f, 15f); // Width (X) and Height (Y)

    [Header("Positioning")]
    public float startOffset = 20f;     // How far from the start the first obstacle appears
    public Vector3 spawnDirection = Vector3.forward; // Direction of the course (usually Forward Z)

    void Start()
    {
        //GenerateCourse();
    }
    [ContextMenu("Generate")]
    void GenerateCourse()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogError("No Obstacle Prefab assigned!");
            return;
        }

        for (int i = 0; i < numberOfObstacles; i++)
        {
            // 1. Calculate how far along the track this obstacle is
            // We use a random distance so they aren't in perfect rows
            float distance = Random.Range(startOffset, courseLength);

            // 2. Calculate random X (Width) and Y (Height) position
            float randomX = Random.Range(-tunnelSize.x / 2, tunnelSize.x / 2);
            float randomY = Random.Range(-tunnelSize.y / 2, tunnelSize.y / 2);

            // 3. Combine into a final position
            // Start Position + (Direction * Distance) + Offset
            Vector3 spawnPos = transform.position + (spawnDirection * distance);
            spawnPos.x += randomX;
            spawnPos.y += randomY;

            // 4. Spawn the object
            GameObject newOb = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

            // Optional: Random rotation to make it look chaotic
            newOb.transform.rotation = Random.rotation;

            // Organize hierarchy (puts them all under this object to keep Inspector clean)
            newOb.transform.parent = this.transform;
        }
    }

    // Draws the course box in the editor so you can see where it will be
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + (spawnDirection * (courseLength / 2 + startOffset / 2));
        Gizmos.DrawWireCube(center, new Vector3(tunnelSize.x, tunnelSize.y, courseLength));
    }
}