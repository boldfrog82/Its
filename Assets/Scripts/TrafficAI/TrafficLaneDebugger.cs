using UnityEngine;

public class TrafficLaneDebugger : MonoBehaviour
{
    [SerializeField] private Color laneColor = Color.cyan;
    [SerializeField] private bool drawArrows = true;

    private TrafficLane lane;

    private void Awake()
    {
        lane = GetComponent<TrafficLane>();
    }

    private void OnDrawGizmos()
    {
        if (lane == null)
        {
            lane = GetComponent<TrafficLane>();
        }

        if (lane == null || lane.nodes == null || lane.nodes.Count < 2)
        {
            return;
        }

        Gizmos.color = laneColor;

        for (int i = 0; i < lane.nodes.Count - 1; i++)
        {
            TrafficNode a = lane.nodes[i];
            TrafficNode b = lane.nodes[i + 1];
            if (a == null || b == null)
            {
                continue;
            }

            Gizmos.DrawLine(a.Position, b.Position);

            if (drawArrows)
            {
                Vector3 mid = Vector3.Lerp(a.Position, b.Position, 0.5f);
                Vector3 dir = (b.Position - a.Position).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir) * 0.5f;
                Gizmos.DrawLine(mid, mid - dir + right);
                Gizmos.DrawLine(mid, mid - dir - right);
            }
        }
    }
}
