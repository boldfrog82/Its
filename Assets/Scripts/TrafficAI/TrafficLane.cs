using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrafficLane : MonoBehaviour
{
    [Header("Lane Metadata")]
    public string laneId = "Lane_A";
    public int laneIndex;
    public int priority;
    public float laneSpeedLimit = 50f;
    public bool oneWay = true;
    public LaneRole laneRole = LaneRole.General;

    [Header("Connectivity")]
    public List<TrafficLane> adjacentLanes = new List<TrafficLane>();
    public List<TrafficLane> preferredSuccessorLanes = new List<TrafficLane>();

    [Header("Lane Path")]
    public List<TrafficNode> nodes = new List<TrafficNode>();

    public TrafficNode EntryNode => nodes != null && nodes.Count > 0 ? nodes[0] : null;

    public TrafficLane GetBestAdjacentForTurn(TrafficTurnType desiredTurn)
    {
        if (adjacentLanes == null || adjacentLanes.Count == 0)
        {
            return null;
        }

        TrafficLane fallback = null;

        for (int i = 0; i < adjacentLanes.Count; i++)
        {
            TrafficLane lane = adjacentLanes[i];
            if (lane == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = lane;
            }

            if (LaneSupportsTurn(lane, desiredTurn))
            {
                return lane;
            }
        }

        return fallback;
    }

    public static bool LaneSupportsTurn(TrafficLane lane, TrafficTurnType turn)
    {
        if (lane == null)
        {
            return false;
        }

        switch (lane.laneRole)
        {
            case LaneRole.LeftOnly:
                return turn == TrafficTurnType.Left || turn == TrafficTurnType.UTurn;
            case LaneRole.RightOnly:
                return turn == TrafficTurnType.Right;
            case LaneRole.ThroughOnly:
                return turn == TrafficTurnType.Through;
            default:
                return true;
        }
    }

    private void OnValidate()
    {
        if (nodes == null)
        {
            nodes = new List<TrafficNode>();
            return;
        }

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] == null)
            {
                nodes.RemoveAt(i);
            }
            else
            {
                nodes[i].lane = this;
            }
        }
    }
}
