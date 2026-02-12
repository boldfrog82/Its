using System.Collections.Generic;
using UnityEngine;

public class TrafficNode : MonoBehaviour
{
    public List<TrafficNode> nextNodes = new List<TrafficNode>();
    public bool isStopLine;
    public float speedLimit = 50f;

    [Header("Lane/Intersection Metadata")]
    public TrafficLane lane;
    public TrafficIntersection intersection;
    public TrafficTurnType turnType = TrafficTurnType.Through;
    public string signalGroup = "";

    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.35f);

        if (nextNodes == null)
        {
            return;
        }

        Gizmos.color = Color.white;
        foreach (TrafficNode node in nextNodes)
        {
            if (node == null)
            {
                continue;
            }

            Gizmos.DrawLine(transform.position, node.transform.position);
        }
    }
}
