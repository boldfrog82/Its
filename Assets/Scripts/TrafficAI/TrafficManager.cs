using System.Collections.Generic;
using UnityEngine;

public class TrafficManager : MonoBehaviour
{
    public static TrafficManager Instance { get; private set; }
    public TrafficSimulationScheduler Scheduler => simulationScheduler;

    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private int poolSize = 50;
    [SerializeField] private float respawnCheckInterval = 2f;
    [SerializeField] private float despawnDistance = 150f;
    [SerializeField] private float spawnDistance = 80f;

    [Header("Streaming / Optimization")]
    [SerializeField] private TrafficWorldPartitionBridge worldPartitionBridge;
    [SerializeField] private TrafficSimulationScheduler simulationScheduler;
    [SerializeField] private TrafficDensityJobSystem densityJobSystem;
    [SerializeField] private TrafficPlaytestTuning playtestTuning;

    public List<GameObject> pool = new List<GameObject>();
    public int ActiveCarCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].activeSelf)
                {
                    count++;
                }
            }

            return count;
        }
    }

    private TrafficNode[] trafficNodes;
    private TrafficLane[] trafficLanes;
    private TrafficDestination[] destinations;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        trafficNodes = FindObjectsOfType<TrafficNode>();
        trafficLanes = FindObjectsOfType<TrafficLane>();
        destinations = FindObjectsOfType<TrafficDestination>();

        if (simulationScheduler == null)
        {
            simulationScheduler = GetComponent<TrafficSimulationScheduler>();
        }

        if (densityJobSystem == null)
        {
            densityJobSystem = GetComponent<TrafficDensityJobSystem>();
        }

        BuildPool();

        float densityScale = playtestTuning != null ? Mathf.Max(0.2f, playtestTuning.globalDensityScale) : 1f;
        float effectiveInterval = respawnCheckInterval / densityScale;
        InvokeRepeating(nameof(RunSpawnCycle), effectiveInterval, effectiveInterval);
    }

    private void BuildPool()
    {
        int carLayer = LayerMask.NameToLayer("Car");

        for (int i = 0; i < poolSize; i++)
        {
            GameObject vehicle = vehiclePrefab != null
                ? Instantiate(vehiclePrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            vehicle.name = $"TrafficCar_{i}";
            vehicle.transform.localScale = new Vector3(2f, 1f, 4f);

            if (carLayer >= 0)
            {
                vehicle.layer = carLayer;
            }

            if (vehicle.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = vehicle.AddComponent<Rigidbody>();
                rb.mass = 1200f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            if (vehicle.GetComponent<TrafficVehiclePhysics>() == null)
            {
                vehicle.AddComponent<TrafficVehiclePhysics>();
            }

            TrafficCar car = vehicle.GetComponent<TrafficCar>();
            if (car == null)
            {
                car = vehicle.AddComponent<TrafficCar>();
            }

            if (simulationScheduler != null)
            {
                simulationScheduler.Register(car);
            }

            vehicle.SetActive(false);
            pool.Add(vehicle);
        }
    }


    public void ForceSpawnOnce()
    {
        RunSpawnCycle();
    }

    private void RunSpawnCycle()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (trafficNodes == null || trafficNodes.Length == 0)
        {
            trafficNodes = FindObjectsOfType<TrafficNode>();
            if (trafficNodes == null || trafficNodes.Length == 0)
            {
                return;
            }
        }

        if (trafficLanes == null || trafficLanes.Length == 0)
        {
            trafficLanes = FindObjectsOfType<TrafficLane>();
        }

        Vector3 cameraPosition = mainCamera.transform.position;

        foreach (GameObject car in pool)
        {
            if (!car.activeSelf)
            {
                continue;
            }

            if (Vector3.Distance(car.transform.position, cameraPosition) > despawnDistance)
            {
                car.SetActive(false);
            }
        }

        TrafficNode spawnNode = FindSpawnNode(cameraPosition);
        if (spawnNode == null)
        {
            return;
        }

        GameObject inactiveCar = GetInactiveCar();
        if (inactiveCar == null)
        {
            return;
        }

        inactiveCar.transform.position = spawnNode.Position;
        inactiveCar.transform.rotation = Quaternion.LookRotation(spawnNode.transform.forward, Vector3.up);

        TrafficCar trafficCar = inactiveCar.GetComponent<TrafficCar>();
        trafficCar.targetNode = spawnNode;
        trafficCar.maxSpeed = spawnNode.speedLimit;
        trafficCar.currentSpeed = 0f;

        if (destinations != null && destinations.Length > 0)
        {
            TrafficDestination destination = destinations[Random.Range(0, destinations.Length)];
            trafficCar.SetDestination(destination);
        }

        inactiveCar.SetActive(true);
    }

    private GameObject GetInactiveCar()
    {
        foreach (GameObject car in pool)
        {
            if (!car.activeSelf)
            {
                return car;
            }
        }

        return null;
    }

    private TrafficNode FindSpawnNode(Vector3 cameraPosition)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        List<TrafficNode> offscreenCandidates = new List<TrafficNode>();
        List<TrafficNode> distanceCandidates = new List<TrafficNode>();

        if (trafficLanes != null && trafficLanes.Length > 0)
        {
            for (int i = 0; i < trafficLanes.Length; i++)
            {
                TrafficNode entry = trafficLanes[i] != null ? trafficLanes[i].EntryNode : null;
                TryAddCandidate(entry, cameraPosition, planes, offscreenCandidates, distanceCandidates);
            }
        }
        else
        {
            foreach (TrafficNode node in trafficNodes)
            {
                TryAddCandidate(node, cameraPosition, planes, offscreenCandidates, distanceCandidates);
            }
        }

        if (offscreenCandidates.Count > 0)
        {
            return DeterministicPick(offscreenCandidates, cameraPosition);
        }

        if (distanceCandidates.Count > 0)
        {
            return DeterministicPick(distanceCandidates, cameraPosition);
        }

        return null;
    }

    private TrafficNode DeterministicPick(List<TrafficNode> candidates, Vector3 cameraPosition)
    {
        if (worldPartitionBridge == null)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        Vector2Int cell = worldPartitionBridge.GetCell(cameraPosition);
        int hash = Mathf.Abs(worldPartitionBridge.DeterministicHash(cell));
        int idx = hash % candidates.Count;
        return candidates[idx];
    }

    private void TryAddCandidate(
        TrafficNode node,
        Vector3 cameraPosition,
        Plane[] planes,
        List<TrafficNode> offscreenCandidates,
        List<TrafficNode> distanceCandidates)
    {
        if (node == null)
        {
            return;
        }

        if (worldPartitionBridge != null)
        {
            Vector2Int cell = worldPartitionBridge.GetCell(node.Position);
            if (!worldPartitionBridge.IsCellLoaded(cell))
            {
                return;
            }
        }

        float distance = Vector3.Distance(node.Position, cameraPosition);
        if (distance > spawnDistance)
        {
            return;
        }

        distanceCandidates.Add(node);

        Bounds nodeBounds = new Bounds(node.Position, Vector3.one * 2f);
        bool isVisible = GeometryUtility.TestPlanesAABB(planes, nodeBounds);
        if (!isVisible)
        {
            offscreenCandidates.Add(node);
        }
    }
}
