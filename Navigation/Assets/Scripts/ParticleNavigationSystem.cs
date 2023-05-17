using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ParticleNavigationSystem : MonoBehaviour
{
    //Needed: navmesh agent
    //terrain
    //check every x seconds to see if corners of the nav mesh agent have moved
    //delete the existing "segments" -> gameobjects that contain the particles
    //create and move new segments at the begining of the list

    NavMeshAgent agent;

    [Header("Player")]
    public GameObject player;

    [Header("Destination")]
    [Tooltip("The object that needs to be reached")]
    public Transform target;

    [Header("The map")]
    public Terrain map;

    [Header("Particle")]
    [Tooltip("Is the particle prefab that will spawn on the ground")]
    public GameObject particlePrefab;

    [Header("Properties of the system")]
    [Tooltip("How far apart from eachother the particles need to be")]
    [Range(0.000001f, 1f)]
    public float particleSpacing = 0.5f;

    [Tooltip("The time at which the path is updated")]
    public float pathCheckInterval = 0.3f;

    [Tooltip("How much should it go throgh the obstacles")]
    public ObstacleAvoidanceType obstacleAvoidance = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

    List<Vector3> pathWaypoints = new List<Vector3>();
    List<Vector3> newPathWaypoints = new List<Vector3>();
    GameObject particlesContainer;
    List<GameObject> segments = new List<GameObject>();

    Coroutine coroutine;
    bool destinationReached = false;

    private void Start()
    {
        //Check if player has already a NavMeshAgent
        //If not create one with no movement
        if(!player.TryGetComponent<NavMeshAgent>(out agent))
        {
            agent = player.AddComponent<NavMeshAgent>();  
        }
        agent.agentTypeID = AgentTypeID.GetAgenTypeIDByName("NavigationSystemAgent");
        agent.speed = 0;
        agent.angularSpeed = 0;
        agent.acceleration = 0;
        agent.stoppingDistance = 0;
        agent.radius = 0.5f;
        agent.height = 0.5f;
        agent.obstacleAvoidanceType = obstacleAvoidance;

        // Create a container to hold the segments of particles
        particlesContainer = new GameObject("ParticlesContainer");

        foreach(var corner in agent.path.corners)
        {
            pathWaypoints.Add(corner);
        }
        Debug.Log(pathWaypoints[0]);
        
        SpawnSegments(pathWaypoints.Count);

        SetDestination(target);

        // Start checking for path updates
        //coroutine = StartCoroutine(CheckForPathUpdate(pathCheckInterval));
    }

    public void SetDestination(Transform target)
    {
        if(coroutine != null)
        StopCoroutine(coroutine);
        agent.destination = (target.position);
        coroutine = StartCoroutine(CheckForPathUpdate(pathCheckInterval));
    }
    IEnumerator CheckForPathUpdate(float timeOfFrequency)
    {
        if(destinationReached)
        {
            if(segments.Count > 0)
            {
                ClearSegments(segments.Count);
            }
            yield break;
        }
        foreach (var corner in agent.path.corners)
        {
            newPathWaypoints.Add(corner);
        }
        
        
        int numberOfCornersChanged = GetNumberOfCornersChanged(newPathWaypoints);

        if(numberOfCornersChanged > 0)
        {
            pathWaypoints = newPathWaypoints;
            ClearSegments(numberOfCornersChanged);
            SpawnSegments(numberOfCornersChanged);
        }
        yield return new WaitForSeconds(timeOfFrequency);
        coroutine = StartCoroutine(CheckForPathUpdate(timeOfFrequency));
        yield break;
    }

    private int GetNumberOfCornersChanged(List<Vector3> newPathWaypoints)
    {
        int changes = 0;

        if (pathWaypoints.Count - newPathWaypoints.Count > 0)
        {
            for (int i = 0; i < pathWaypoints.Count - newPathWaypoints.Count; i++)
            {
                pathWaypoints.RemoveAt(i);
                Destroy(segments[i]);
            }
        }

        for (int i = 0; i < pathWaypoints.Count; i++)
        {
            if(pathWaypoints[i] != newPathWaypoints[i])
            {
                changes++;
            }
        }

        Debug.Log("Changes: " + changes);
        return changes;
    }

    private void ClearSegments(int amountToDestroy)
    {
        for (int i = 0; i < amountToDestroy; i++)
        {
            Destroy(segments[i]);
        }
    }

    private void SpawnSegments(int amountToSpawn)
    {
        Debug.Log("Spawned segments");
        for (int i = 0; i < amountToSpawn - 1; i++)
        {
            Vector3 currentWaypoint = pathWaypoints[i];
            Vector3 nextWaypoint = pathWaypoints[i + 1];

            float segmentDistance = Vector3.Distance(currentWaypoint, nextWaypoint);
            int numParticles = Mathf.CeilToInt(segmentDistance / particleSpacing);
            float stepSize = 1f / numParticles;

            GameObject segment = new GameObject("Segment" + i);
            segment.transform.parent = particlesContainer.transform;
            segments.Add(segment);
            
            for (int j = 0; j < numParticles; j++)
            {
                float t = j * stepSize;
                Vector3 particlePosition = Vector3.Lerp(currentWaypoint, nextWaypoint, t);

                GameObject particle = Instantiate(particlePrefab, new Vector3(particlePosition.x, 
                    GetTerrainHeight(particlePosition) + 0.5f, particlePosition.z), Quaternion.identity);
                particle.transform.parent = segment.transform;
            }
        }

        if(amountToSpawn == 1)
        {
            Vector3 currentWaypoint = pathWaypoints[0];
            Vector3 nextWaypoint = target.position;
            float segmentDistance = Vector3.Distance(currentWaypoint, nextWaypoint);
            int numParticles = Mathf.CeilToInt(segmentDistance / particleSpacing);
            float stepSize = 1f / numParticles;

            GameObject segment = new GameObject("Segment" + 0);
            segment.transform.parent = particlesContainer.transform;
            segments.Add(segment);

            for (int j = 0; j < numParticles; j++)
            {
                float t = j * stepSize;
                Vector3 particlePosition = Vector3.Lerp(currentWaypoint, nextWaypoint, t);

                GameObject particle = Instantiate(particlePrefab, new Vector3(particlePosition.x,
                    GetTerrainHeight(particlePosition) + 0.5f, particlePosition.z), Quaternion.identity);
                particle.transform.parent = segment.transform;
            }
        }
        segments.Sort();
    }

    float GetTerrainHeight(Vector3 position)
    {
        if (map != null)
        {
            // Convert world position to terrain local position
            Vector3 terrainLocalPos = position - map.transform.position;

            // Get normalized terrain coordinates (0-1 range)
            Vector3 normalizedPos = new Vector3(terrainLocalPos.x / map.terrainData.size.x, 0f, terrainLocalPos.z / map.terrainData.size.z);

            // Get the height at the normalized position
            float terrainHeight = map.terrainData.GetInterpolatedHeight(
                normalizedPos.x,
                normalizedPos.z
            );

            return terrainHeight;
        }
        else
        {
            Debug.LogError("No terrain has been assigned for the navigation");
            return 0;
        }
    }
}
