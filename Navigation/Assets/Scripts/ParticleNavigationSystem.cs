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
    public List<GameObject> segments = new List<GameObject>();

    Coroutine coroutine;
    bool destinationReached = false;

    NavMeshPath nav;

    private void Start()
    {
        nav = new NavMeshPath();
        //Check if player has already a NavMeshAgent
        //If not create one with no movement
        if (!player.TryGetComponent<NavMeshAgent>(out agent))
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
        agent.velocity = Vector3.zero;
        agent.updatePosition = false;
        agent.updateRotation = false;


        // Create a container to hold the segments of particles
        particlesContainer = new GameObject("ParticlesContainer");

        pathWaypoints = new List<Vector3>(agent.path.corners);
        //Debug.Log(pathWaypoints[0]);
        
        //SpawnSegments(pathWaypoints.Count);

        SetDestination(target);

        // Start checking for path updates
        //coroutine = StartCoroutine(CheckForPathUpdate(pathCheckInterval));
    }

    public void SetDestination(Transform target)
    {
        ClearSegments(segments.Count);
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

        /*if (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete)
            
       yield return null;*/

        yield return NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, nav);
        newPathWaypoints = new List<Vector3>(nav.corners);
        //yield return null;
        int numberOfCornersChanged = GetNumberOfCornersChanged(newPathWaypoints);

        if(numberOfCornersChanged > 0)
        {
            pathWaypoints.Clear();
            pathWaypoints.AddRange(newPathWaypoints);
            yield return ClearSegments(numberOfCornersChanged);
            SpawnSegments(numberOfCornersChanged);
        }
        yield return new WaitForSeconds(timeOfFrequency);
        coroutine = StartCoroutine(CheckForPathUpdate(timeOfFrequency));
        yield return null;
    }

    private int GetNumberOfCornersChanged(List<Vector3> newPathWaypoints)
    {
        int changes = 0;

        int i = pathWaypoints.Count - 1;
        int j = newPathWaypoints.Count - 1;

        while(i >= 0 && j >= 0)
        {
            if(!pathWaypoints[i].Equals(newPathWaypoints[j]) && Mathf.Abs(pathWaypoints[i].magnitude - newPathWaypoints[j].magnitude) > 0.5f)
            {
                //Debug.Log("Current path point at " + i + "is " + pathWaypoints[i]);
                //Debug.Log("New path point at " + j + "is " + newPathWaypoints[j]);

                changes = Mathf.Max(j, i) + 1;
                break;
            }
            j--;
            i--;
        }
        //Debug.Log("Changes: " + changes);
        return changes;
    }

    private int ClearSegments(int amountToDestroy)
    {
        if(amountToDestroy > segments.Count)
        {
            amountToDestroy = segments.Count;
        }

        if (segments.Count == 2 || segments.Count > pathWaypoints.Count)
        {
            amountToDestroy = segments.Count;
        }
        for (int i = 0; i < amountToDestroy; i++)
        {
            Debug.Log("Destroyed " + segments[segments.Count - 1].name);
            Destroy(segments[segments.Count - 1]);
            segments.RemoveAt(segments.Count - 1);
        }
        return 0;
    }

    private void SpawnSegments(int amountToSpawn)
    {
        //Debug.Log("Spawned segments");
        if (amountToSpawn > pathWaypoints.Count)
        {
            amountToSpawn = pathWaypoints.Count;
        }
        
        //if(amountToSpawn >= 1 && pathWaypoints.Count >= 2)
        if(pathWaypoints.Count == 2)
        {
            amountToSpawn = pathWaypoints.Count;
        }
        for (int i = amountToSpawn - 1; i >= 0; i--)
        {
            Debug.Log("Spawned segment with number" + i);
            Vector3 currentWaypoint = new Vector3();
            Vector3 nextWaypoint = new Vector3();
            if (i > 0)
            {
                currentWaypoint = pathWaypoints[i];
                nextWaypoint = pathWaypoints[i - 1];
            }
            else
            {
                currentWaypoint = pathWaypoints[i];
                nextWaypoint = pathWaypoints[i];
            }

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
                particlePosition.y, particlePosition.z), Quaternion.identity);
                particle.transform.parent = segment.transform;
            }
            /*GameObject segment = new GameObject("Segment" + i);
            segment.transform.parent = particlesContainer.transform;
            segments.Add(segment);
            GameObject particle = Instantiate(particlePrefab, pathWaypoints[i], Quaternion.identity);
            particle.transform.parent = segment.transform;*/
        }
        //segments.Sort();
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
