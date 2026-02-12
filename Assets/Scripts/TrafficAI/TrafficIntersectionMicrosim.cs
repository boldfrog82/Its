using UnityEngine;

public class TrafficIntersectionMicrosim : MonoBehaviour
{
    [SerializeField] private TrafficConflictMatrix conflictMatrix;
    [SerializeField] private float baseAcceptedGapSeconds = 1.6f;
    [SerializeField] private float leftTurnPenalty = 0.5f;
    [SerializeField] private float uTurnPenalty = 0.9f;
    [SerializeField] private float highSeverityConflictPenalty = 0.8f;

    public bool HasConflict(TrafficTurnType existingTurn, TrafficTurnType incomingTurn)
    {
        return GetConflictSeverity(existingTurn, incomingTurn) > 0f;
    }

    public float GetConflictSeverity(TrafficTurnType existingTurn, TrafficTurnType incomingTurn)
    {
        if (existingTurn == incomingTurn)
        {
            return 0f;
        }

        if (existingTurn == TrafficTurnType.Right && incomingTurn == TrafficTurnType.Right)
        {
            return 0f;
        }

        if (conflictMatrix != null)
        {
            return conflictMatrix.GetSeverity(existingTurn, incomingTurn);
        }

        if (existingTurn == TrafficTurnType.UTurn || incomingTurn == TrafficTurnType.UTurn)
        {
            return 1f;
        }

        if ((existingTurn == TrafficTurnType.Through && incomingTurn == TrafficTurnType.Left) ||
            (existingTurn == TrafficTurnType.Left && incomingTurn == TrafficTurnType.Through))
        {
            return 0.9f;
        }

        if ((existingTurn == TrafficTurnType.Right && incomingTurn == TrafficTurnType.Left) ||
            (existingTurn == TrafficTurnType.Left && incomingTurn == TrafficTurnType.Right))
        {
            return 0.6f;
        }

        return 0.7f;
    }

    public bool AcceptGap(float estimatedTimeToConflict, TrafficTurnType incomingTurn, float driverAggression, float conflictSeverity)
    {
        float required = baseAcceptedGapSeconds;

        if (incomingTurn == TrafficTurnType.Left)
        {
            required += leftTurnPenalty;
        }
        else if (incomingTurn == TrafficTurnType.UTurn)
        {
            required += uTurnPenalty;
        }

        if (conflictSeverity >= 0.85f)
        {
            required += highSeverityConflictPenalty;
        }

        float aggressionReduction = Mathf.Clamp01(driverAggression) * 0.6f;
        required = Mathf.Max(0.5f, required - aggressionReduction);

        return estimatedTimeToConflict >= required;
    }
}
