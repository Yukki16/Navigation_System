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
        //////////////////////////////////////////////////////////////////////////////////////////////////
        
        // Create a container to hold the segments of particles
        particlesContainer = new GameObject("ParticlesContainer");

        ///////////////////////////////////////////////////////////////////////////////////////////////////
        
        // Add the initial corners
        foreach (var corner in agent.path.corners)
        {
            pathWaypoints.Add(corner);
        }
        //Add the target to the list
        pathWaypoints.Add(target.transform.position);

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        SpawnSegments(pathWaypoints.Count);

        SetDestination(target);

        // Start checking for path updates
        //coroutine = StartCoroutine(CheckForPathUpdate(pathCheckInterval));
    }

    public void SetDestination(Transform target)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        agent.destination = (target.position);
        coroutine = StartCoroutine(CheckForPathUpdate(pathCheckInterval));
    }
    IEnumerator CheckForPathUpdate(float timeOfFrequency)
    {
        /*if(agent.hasPath)
        {
            for(int i = 0; i < agent.path.corners.Length; i++)
            Debug.Log(agent.path.corners[i]);
        }*/
        if ((player.transform.position - agent.destination).magnitude < 1.5f)
        {
            destinationReached = true;
        }
        else
        {
            destinationReached = false;
        }

        if (destinationReached)
        {
            if (segments.Count > 0)
            {
                ClearSegments(segments.Count);
            }
            yield break;
        }

        ///////////////////////////////////////////////////////////////////////
        //Adding the new corners to the list
        newPathWaypoints.Clear();
        foreach (var corner in agent.path.corners)
        {
            newPathWaypoints.Add(corner);
        }
        //Add the target to the list
        newPathWaypoints.Add(target.transform.position);
        ////////////////////////////////////////////////////////////////////////

        //Debug info
        Debug.Log("Initial path waypoits: " + pathWaypoints.Count + " new path: " + newPathWaypoints.Count);
        for (int i = 0; i < newPathWaypoints.Count; i++)
        {
            Debug.Log(" new: " + newPathWaypoints[i]);
        }
        for (int i = 0; i < pathWaypoints.Count; i++)
        {
            Debug.Log("path: " + pathWaypoints[i]);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////


        //Check for changes
        int numberOfCornersChanged = GetNumberOfCornersChanged(newPathWaypoints);

        ////////////////////////////////////////////////////////////////////////////////////
            
        //If there are changes clear the changes amount of segments and spawn new amount = nr of changes
        if (numberOfCornersChanged > 0)
        {
            pathWaypoints = newPathWaypoints;
            ClearSegments(numberOfCornersChanged);
            SpawnSegments(numberOfCornersChanged);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        
        //Coroutine loop
        yield return new WaitForSeconds(timeOfFrequency);
        coroutine = StartCoroutine(CheckForPathUpdate(timeOfFrequency));
        yield return null;
    }

    private int GetNumberOfCornersChanged(List<Vector3> newPathWaypoints)
    {

        int changes = 0;
        int diff = Mathf.Abs(newPathWaypoints.Count - pathWaypoints.Count);
        Debug.Log("Diff: " + diff);
        
        if (newPathWaypoints.Count > pathWaypoints.Count)
        {
            for (int i = newPathWaypoints.Count - 2; i >= diff; i--)
            {
                //Debug.Log(i);
                if (!newPathWaypoints[i].Equals(pathWaypoints[i - diff]))
                {
                    changes = i + 1;
                    break;
                }
            }
        }
        else if (newPathWaypoints.Count < pathWaypoints.Count)
        {
            for (int i = pathWaypoints.Count - 2; i >= diff; i--)
            {
                if (!pathWaypoints[i].Equals(newPathWaypoints[i - diff]))
                {
                    changes += i + 1;
                    break;
                }
            }
        }
        else if(newPathWaypoints.Count == pathWaypoints.Count)
        {
            for (int i = pathWaypoints.Count - 2; i >= 0; i--)
            {
                if (!pathWaypoints[i].Equals(newPathWaypoints[i]))
                {
                    changes += i + 1;
                    break;
                }
            }
        }
        if (changes == 0)
            changes = diff;
        Debug.Log("Changes: " + changes);
        return changes;
    }

    private void ClearSegments(int amountToDestroy)
    {
        for (int i = 0; i < amountToDestroy - 1; i++)
        {
            if (0 == segments.Count)
                break;
            //Debug.Log(segments.Count);
            Destroy(segments[segments.Count - 1]);
            segments.RemoveAt(segments.Count - 1);
            /*if (i > segments.Count)
                break;
            for (int j = segments.Count - 1; j >= 0; j--)
            {
                //Debug.Log(segments.Count);
                //Debug.Log("J: " + j);
                //if(segments[j] != null)
                if (segments[j].name.Contains(i.ToString()))
                {
                    Destroy(segments[j]);
                    segments.RemoveAt(j);
                }
            }*/
        }
    }

    private void SpawnSegments(int amountToSpawn)
    {
        //Debug.Log("Spawned segments");
        for (int i = amountToSpawn - 1; i > 0; i--)
        {
            //Debug.Log(i);
            Vector3 currentWaypoint = pathWaypoints[i];
            Vector3 nextWaypoint = pathWaypoints[i - 1]; 

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
