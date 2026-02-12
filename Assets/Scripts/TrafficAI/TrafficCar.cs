using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficCar : MonoBehaviour
{
    public TrafficNode targetNode;
    public float currentSpeed;
    public float maxSpeed = 14f;

    [Header("Behavior")]
    [SerializeField] private TrafficDriverProfile driverProfile;
    [SerializeField] private DriverBehaviorMode fallbackBehavior = DriverBehaviorMode.Normal;
    [SerializeField] private TrafficPlaytestTuning playtestTuning;

    [Header("Strategic Routing")]
    [SerializeField] private TrafficRoutePlanner routePlanner;
    [SerializeField] private TrafficDestination destination;

    [Header("Pathing")]
    [SerializeField] private float nodeReachDistance = 2.0f;
    [SerializeField] private float turnSpeed = 4f;
    [SerializeField] private float laneChangeCooldown = 2.5f;
    [SerializeField] private float minLaneStickTime = 3.0f;
    [SerializeField] private int tacticalLookAheadNodes = 4;

    [Header("Following / Anti-Jitter")]
    [SerializeField] private float baseStopDistance = 4.5f;
    [SerializeField] private float timeHeadway = 0.55f;
    [SerializeField] private float stopHysteresis = 1.5f;
    [SerializeField] private float minStopHoldTime = 0.35f;

    [Header("Sensors")]
    [SerializeField] private float sensorDistance = 25f;
    [SerializeField] private float obstaclePredictionLead = 0.4f;

    [Header("Recovery")]
    [SerializeField] private float stuckSpeedThreshold = 0.45f;
    [SerializeField] private float stuckDuration = 3f;
    [SerializeField] private float deadlockDuration = 7f;

    [Header("Input Rates")]
    [SerializeField] private float throttleResponse = 2f;
    [SerializeField] private float brakeResponse = 5f;

    private int carLayer;
    private int obstacleLayer;
    private Rigidbody rb;
    private TrafficVehiclePhysics vehiclePhysics;
    private TrafficSimulationScheduler simulationScheduler;

    private bool isHardStopped;
    private float stopReleaseTime;
    private float throttleState;
    private float brakeState;
    private float stuckTimer;
    private float stoppedTimer;
    private float laneChangeUnlockTime;
    private float lastLaneChangeTime;
    private TrafficLane previousLane;

    private readonly List<TrafficNode> strategicRoute = new List<TrafficNode>();
    private int strategicRouteIndex;

    public TrafficLane CurrentLane { get; private set; }
    public float CurrentSpeed => currentSpeed;
    public float AggressionFactor
    {
        get
        {
            float tuneAggression = playtestTuning != null ? playtestTuning.globalAggressionScale : 1f;
            return Mathf.Clamp01(driverProfile != null ? driverProfile.laneChangeBias * 2f * tuneAggression : 0.5f);
        }
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        vehiclePhysics = GetComponent<TrafficVehiclePhysics>();

        carLayer = LayerMask.NameToLayer("Car");
        obstacleLayer = LayerMask.NameToLayer("Obstacle");

        if (driverProfile == null)
        {
            driverProfile = TrafficDriverProfile.CreateRuntimeDefault(fallbackBehavior);
        }

        if (TrafficManager.Instance != null)
        {
            simulationScheduler = TrafficManager.Instance.Scheduler;
        }

        if (routePlanner == null)
        {
            routePlanner = FindObjectOfType<TrafficRoutePlanner>();
        }
    }

    private void FixedUpdate()
    {
        if (simulationScheduler != null && !simulationScheduler.ShouldSimulateThisFrame(this))
        {
            return;
        }

        if (targetNode == null)
        {
            ApplyControl(0f, 1f, 0f);
            currentSpeed = rb.velocity.magnitude;
            return;
        }

        CurrentLane = targetNode.lane;

        float tuneSpeedScale = playtestTuning != null ? playtestTuning.globalSpeedScale : 1f;
        float tuneCaution = playtestTuning != null ? playtestTuning.intersectionCautionScale : 1f;
        float tuneLaneChange = playtestTuning != null ? playtestTuning.laneChangeFrequencyScale : 1f;
        float tuneSensor = playtestTuning != null ? playtestTuning.sensorDistanceScale : 1f;

        maxSpeed = targetNode.speedLimit * driverProfile.speedMultiplier * tuneSpeedScale;

        Vector3 toNode = targetNode.Position - transform.position;
        toNode.y = 0f;
        float distanceToNode = toNode.magnitude;

        Vector3 desiredDirection = distanceToNode > 0.001f ? toNode.normalized : transform.forward;
        float steer = CalculateSteer(desiredDirection);

        float effectiveSensor = sensorDistance * tuneSensor;
        bool obstacleAhead = PredictObstacle(effectiveSensor, out float obstacleDistance, out float relativeClosingSpeed);
        bool shouldYieldIntersection = ShouldYieldAtIntersection(distanceToNode, tuneCaution);

        TacticalLanePlanning(distanceToNode, obstacleAhead, tuneLaneChange);
        TryLaneChangeOrOvertake(obstacleAhead, tuneLaneChange);

        bool mustStop = obstacleAhead || shouldYieldIntersection;

        float dynamicSafeDistance = baseStopDistance + Mathf.Max(0f, currentSpeed * timeHeadway * driverProfile.reactionTimeMultiplier);
        float predictedClosing = Mathf.Max(0f, relativeClosingSpeed) * obstaclePredictionLead;
        dynamicSafeDistance += predictedClosing;

        if (obstacleAhead && obstacleDistance > dynamicSafeDistance + stopHysteresis)
        {
            mustStop = false;
        }

        ApplyMistakeModel(ref mustStop);
        HandleStopState(mustStop);

        float targetNormalizedSpeed = Mathf.Clamp01(maxSpeed <= 0.01f ? 0f : maxSpeed / 50f);
        float throttle = isHardStopped ? 0f : targetNormalizedSpeed;
        float brake = isHardStopped ? 1f : 0f;

        ApplyControl(throttle, brake, steer);
        UpdateFacing(desiredDirection);

        currentSpeed = rb.velocity.magnitude;
        TrackStuckAndRecover(mustStop);

        if (distanceToNode <= nodeReachDistance)
        {
            AdvanceToNextNode();
        }
    }

    public void SetDestination(TrafficDestination newDestination)
    {
        destination = newDestination;
        strategicRoute.Clear();
        strategicRouteIndex = 0;
    }

    private bool CanSwitchToLane(TrafficLane candidate)
    {
        if (candidate == null || candidate == CurrentLane)
        {
            return false;
        }

        if (Time.time < laneChangeUnlockTime)
        {
            return false;
        }

        if (previousLane != null && candidate == previousLane && Time.time - lastLaneChangeTime < minLaneStickTime)
        {
            return false;
        }

        return true;
    }

    private void TacticalLanePlanning(float distanceToNode, bool obstacleAhead, float tuneLaneChange)
    {
        if (CurrentLane == null || targetNode == null || distanceToNode > 40f || obstacleAhead)
        {
            return;
        }

        TrafficTurnType upcomingTurn = PreviewUpcomingTurn(targetNode, tacticalLookAheadNodes);
        if (upcomingTurn == TrafficTurnType.Through || TrafficLane.LaneSupportsTurn(CurrentLane, upcomingTurn))
        {
            return;
        }

        TrafficLane plannedLane = CurrentLane.GetBestAdjacentForTurn(upcomingTurn);
        if (!CanSwitchToLane(plannedLane))
        {
            return;
        }

        TrafficNode plannedNode = FindClosestNodeOnLane(plannedLane);
        if (plannedNode == null)
        {
            return;
        }

        previousLane = CurrentLane;
        targetNode = plannedNode;
        lastLaneChangeTime = Time.time;
        laneChangeUnlockTime = Time.time + laneChangeCooldown / Mathf.Max(0.2f, tuneLaneChange);
    }

    private TrafficTurnType PreviewUpcomingTurn(TrafficNode fromNode, int lookahead)
    {
        TrafficNode node = fromNode;
        for (int i = 0; i < lookahead; i++)
        {
            if (node == null)
            {
                break;
            }

            if (node.turnType != TrafficTurnType.Through)
            {
                return node.turnType;
            }

            node = node.nextNodes != null && node.nextNodes.Count > 0 ? node.nextNodes[0] : null;
        }

        return TrafficTurnType.Through;
    }

    private void ApplyMistakeModel(ref bool mustStop)
    {
        float tuneAggression = playtestTuning != null ? playtestTuning.globalAggressionScale : 1f;
        float probability = driverProfile.mistakeProbability * tuneAggression;
        if (Random.value < probability * Time.fixedDeltaTime)
        {
            mustStop = !mustStop;
        }
    }

    private void TryLaneChangeOrOvertake(bool obstacleAhead, float tuneLaneChange)
    {
        if (!obstacleAhead || CurrentLane == null)
        {
            return;
        }

        if (Random.value > driverProfile.laneChangeBias * tuneLaneChange)
        {
            return;
        }

        TrafficLane adjacent = CurrentLane.GetBestAdjacentForTurn(TrafficTurnType.Through);
        if (!CanSwitchToLane(adjacent))
        {
            return;
        }

        TrafficNode closest = FindClosestNodeOnLane(adjacent);
        if (closest == null)
        {
            return;
        }

        previousLane = CurrentLane;
        targetNode = closest;
        lastLaneChangeTime = Time.time;
        laneChangeUnlockTime = Time.time + laneChangeCooldown / Mathf.Max(0.2f, tuneLaneChange);
    }

    private TrafficNode FindClosestNodeOnLane(TrafficLane lane)
    {
        if (lane == null || lane.nodes == null)
        {
            return null;
        }

        TrafficNode best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < lane.nodes.Count; i++)
        {
            TrafficNode node = lane.nodes[i];
            if (node == null)
            {
                continue;
            }

            float dist = Vector3.SqrMagnitude(node.Position - transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = node;
            }
        }

        return best;
    }

    private void TrackStuckAndRecover(bool mustStop)
    {
        if (currentSpeed < stuckSpeedThreshold)
        {
            stoppedTimer += Time.fixedDeltaTime;
            if (!mustStop)
            {
                stuckTimer += Time.fixedDeltaTime;
            }
        }
        else
        {
            stoppedTimer = 0f;
            stuckTimer = 0f;
        }

        if (stuckTimer > stuckDuration)
        {
            RerouteFallback();
            stuckTimer = 0f;
        }

        if (stoppedTimer > deadlockDuration)
        {
            DeadlockBreaker();
            stoppedTimer = 0f;
        }
    }

    private void RerouteFallback()
    {
        if (CurrentLane == null)
        {
            return;
        }

        TrafficLane adjacent = CurrentLane.GetBestAdjacentForTurn(TrafficTurnType.Through);
        TrafficNode rerouteNode = adjacent != null ? FindClosestNodeOnLane(adjacent) : null;
        if (rerouteNode != null)
        {
            targetNode = rerouteNode;
        }
    }

    private void DeadlockBreaker()
    {
        isHardStopped = false;
        stopReleaseTime = Time.time;

        if (targetNode != null && targetNode.intersection != null)
        {
            targetNode.intersection.ReleaseRightOfWay(this);
        }
    }

    private void HandleStopState(bool mustStop)
    {
        if (mustStop)
        {
            isHardStopped = true;
            stopReleaseTime = Time.time + minStopHoldTime;
            return;
        }

        if (Time.time >= stopReleaseTime)
        {
            isHardStopped = false;
        }
    }

    private void ApplyControl(float throttle, float brake, float steer)
    {
        if (vehiclePhysics != null)
        {
            throttleState = Mathf.Lerp(throttleState, throttle, Time.fixedDeltaTime * throttleResponse);
            brakeState = Mathf.Lerp(brakeState, brake, Time.fixedDeltaTime * brakeResponse);
            vehiclePhysics.SetInputs(throttleState, brakeState, steer);
            return;
        }

        Vector3 forwardVelocity = transform.forward * (throttle * maxSpeed);
        if (brake > 0.5f)
        {
            forwardVelocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * brakeResponse);
        }

        rb.velocity = new Vector3(forwardVelocity.x, rb.velocity.y, forwardVelocity.z);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(transform.forward), Time.fixedDeltaTime * turnSpeed));
    }

    private float CalculateSteer(Vector3 desiredDirection)
    {
        Vector3 localDir = transform.InverseTransformDirection(desiredDirection);
        return Mathf.Clamp(localDir.x, -1f, 1f);
    }

    private void UpdateFacing(Vector3 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.fixedDeltaTime * turnSpeed);
    }

    private bool ShouldYieldAtIntersection(float distanceToNode, float tuneCaution)
    {
        if (targetNode == null || !targetNode.isStopLine || targetNode.intersection == null)
        {
            return false;
        }

        if (distanceToNode > baseStopDistance * 1.5f * tuneCaution)
        {
            return false;
        }

        bool granted = targetNode.intersection.RequestRightOfWay(this, CurrentLane, targetNode.turnType, targetNode.signalGroup);
        return !granted;
    }

    private bool PredictObstacle(float sensorRange, out float obstacleDistance, out float relativeClosingSpeed)
    {
        obstacleDistance = sensorRange;
        relativeClosingSpeed = 0f;

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 direction = transform.forward;

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, sensorRange))
        {
            return false;
        }

        int hitLayer = hit.collider.gameObject.layer;
        bool hitCar = carLayer >= 0 && hitLayer == carLayer;
        bool hitObstacle = obstacleLayer >= 0 && hitLayer == obstacleLayer;
        if (!hitCar && !hitObstacle)
        {
            return false;
        }

        obstacleDistance = hit.distance;

        Rigidbody otherBody = hit.rigidbody;
        if (otherBody != null)
        {
            relativeClosingSpeed = Vector3.Dot(rb.velocity - otherBody.velocity, direction);
        }

        float safeDistance = baseStopDistance + Mathf.Max(0f, currentSpeed * timeHeadway * driverProfile.reactionTimeMultiplier);
        float projectedDistance = obstacleDistance - Mathf.Max(0f, relativeClosingSpeed) * obstaclePredictionLead;

        return projectedDistance <= safeDistance;
    }

    private void AdvanceToNextNode()
    {
        if (targetNode == null || targetNode.nextNodes == null || targetNode.nextNodes.Count == 0)
        {
            if (targetNode != null && targetNode.intersection != null)
            {
                targetNode.intersection.ReleaseRightOfWay(this);
            }

            targetNode = null;
            return;
        }

        if (targetNode.intersection != null)
        {
            targetNode.intersection.ReleaseRightOfWay(this);
        }

        TrafficNode routed = GetStrategicRouteNextNode();
        targetNode = routed ?? SelectPreferredNextNode(targetNode);
    }

    private TrafficNode GetStrategicRouteNextNode()
    {
        if (destination == null || routePlanner == null || targetNode == null)
        {
            return null;
        }

        if (strategicRoute.Count < 2)
        {
            if (!routePlanner.BuildRouteToDestination(targetNode, destination, strategicRoute))
            {
                return null;
            }

            strategicRouteIndex = 1;
        }

        if (strategicRouteIndex >= 0 && strategicRouteIndex < strategicRoute.Count)
        {
            return strategicRoute[strategicRouteIndex++];
        }

        return null;
    }

    private TrafficNode SelectPreferredNextNode(TrafficNode fromNode)
    {
        TrafficNode fallback = null;

        for (int i = 0; i < fromNode.nextNodes.Count; i++)
        {
            int index = Random.Range(0, fromNode.nextNodes.Count);
            TrafficNode candidate = fromNode.nextNodes[index];
            if (candidate == null)
            {
                continue;
            }

            fallback = candidate;
            if (CurrentLane != null && candidate.lane == CurrentLane)
            {
                return candidate;
            }
        }

        return fallback;
    }
}
