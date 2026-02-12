using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrafficIntersection : MonoBehaviour
{
    [SerializeField] private float reservationDuration = 2.5f;
    [SerializeField] private bool allowMultipleIfSameLane;
    [SerializeField] private TrafficSignalController signalController;
    [SerializeField] private TrafficIntersectionMicrosim microsim;

    private readonly Queue<TrafficCar> waitingCars = new Queue<TrafficCar>();
    private readonly Dictionary<TrafficCar, ReservationData> reservations = new Dictionary<TrafficCar, ReservationData>();

    private struct ReservationData
    {
        public float expiry;
        public TrafficTurnType turnType;
        public TrafficLane lane;
        public int lanePriority;
        public float etaToConflict;
    }

    public bool RequestRightOfWay(TrafficCar car, TrafficLane lane, TrafficTurnType turnType, string signalGroup)
    {
        CleanupExpiredReservations();

        if (car == null)
        {
            return false;
        }

        if (signalController != null)
        {
            signalController.RegisterDemand(signalGroup);
            TrafficSignalState state = signalController.GetGroupState(signalGroup);
            if (state == TrafficSignalState.Red)
            {
                Enqueue(car);
                return false;
            }

            if (state == TrafficSignalState.Yellow && car.CurrentSpeed > 4f)
            {
                Enqueue(car);
                return false;
            }
        }

        if (reservations.ContainsKey(car))
        {
            return true;
        }

        float eta = EstimateEta(car);

        if (HasConflictingReservation(lane, turnType, eta, car.AggressionFactor))
        {
            Enqueue(car);
            return false;
        }

        if (reservations.Count > 0 && !allowMultipleIfSameLane)
        {
            Enqueue(car);
            return false;
        }

        if (waitingCars.Count > 0 && waitingCars.Peek() != car)
        {
            Enqueue(car);
            return false;
        }

        if (waitingCars.Count > 0 && waitingCars.Peek() == car)
        {
            waitingCars.Dequeue();
        }

        GrantReservation(car, lane, turnType, eta);
        return true;
    }

    public void ReleaseRightOfWay(TrafficCar car)
    {
        if (car == null)
        {
            return;
        }

        reservations.Remove(car);
        CleanupQueue();
    }

    private bool HasConflictingReservation(TrafficLane lane, TrafficTurnType turnType, float incomingEta, float incomingAggression)
    {
        int incomingPriority = lane != null ? lane.priority : 0;

        foreach (ReservationData other in reservations.Values)
        {
            if (allowMultipleIfSameLane && lane != null && other.lane == lane)
            {
                continue;
            }

            float conflictSeverity = microsim != null
                ? microsim.GetConflictSeverity(other.turnType, turnType)
                : (DefaultConflicts(other.turnType, turnType) ? 1f : 0f);

            if (conflictSeverity <= 0f)
            {
                continue;
            }

            if (incomingPriority > other.lanePriority + 1)
            {
                continue;
            }

            float gap = Mathf.Abs(other.etaToConflict - incomingEta);
            if (microsim != null && microsim.AcceptGap(gap, turnType, incomingAggression, conflictSeverity))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool DefaultConflicts(TrafficTurnType a, TrafficTurnType b)
    {
        if (a == b)
        {
            return false;
        }

        if (a == TrafficTurnType.Right && b == TrafficTurnType.Right)
        {
            return false;
        }

        return true;
    }

    private float EstimateEta(TrafficCar car)
    {
        float speed = Mathf.Max(1f, car.CurrentSpeed);
        float distance = Vector3.Distance(car.transform.position, transform.position);
        return distance / speed;
    }

    private void GrantReservation(TrafficCar car, TrafficLane lane, TrafficTurnType turnType, float eta)
    {
        ReservationData data = new ReservationData
        {
            expiry = Time.time + reservationDuration,
            turnType = turnType,
            lane = lane,
            lanePriority = lane != null ? lane.priority : 0,
            etaToConflict = eta
        };

        reservations[car] = data;
    }

    private void Enqueue(TrafficCar car)
    {
        if (!waitingCars.Contains(car))
        {
            waitingCars.Enqueue(car);
        }
    }

    private void CleanupExpiredReservations()
    {
        List<TrafficCar> expired = new List<TrafficCar>();

        foreach (KeyValuePair<TrafficCar, ReservationData> pair in reservations)
        {
            if (pair.Key == null || Time.time >= pair.Value.expiry)
            {
                expired.Add(pair.Key);
            }
        }

        for (int i = 0; i < expired.Count; i++)
        {
            reservations.Remove(expired[i]);
        }

        CleanupQueue();
    }

    private void CleanupQueue()
    {
        while (waitingCars.Count > 0 && waitingCars.Peek() == null)
        {
            waitingCars.Dequeue();
        }
    }
}
