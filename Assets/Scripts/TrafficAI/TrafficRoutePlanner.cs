using System.Collections.Generic;
using UnityEngine;

public class TrafficRoutePlanner : MonoBehaviour
{
    [SerializeField] private int maxSearchDepth = 128;

    public bool BuildRouteToDestination(TrafficNode fromNode, TrafficDestination destination, List<TrafficNode> outputRoute)
    {
        if (outputRoute == null || fromNode == null || destination == null || destination.entryNode == null)
        {
            return false;
        }

        outputRoute.Clear();

        Dictionary<TrafficNode, TrafficNode> cameFrom = new Dictionary<TrafficNode, TrafficNode>();
        Queue<TrafficNode> frontier = new Queue<TrafficNode>();
        HashSet<TrafficNode> visited = new HashSet<TrafficNode>();

        frontier.Enqueue(fromNode);
        visited.Add(fromNode);

        int depth = 0;
        bool found = false;

        while (frontier.Count > 0 && depth++ < maxSearchDepth)
        {
            TrafficNode current = frontier.Dequeue();
            if (current == destination.entryNode)
            {
                found = true;
                break;
            }

            if (current.nextNodes == null)
            {
                continue;
            }

            for (int i = 0; i < current.nextNodes.Count; i++)
            {
                TrafficNode next = current.nextNodes[i];
                if (next == null || visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!found)
        {
            return false;
        }

        TrafficNode walker = destination.entryNode;
        outputRoute.Add(walker);

        while (cameFrom.TryGetValue(walker, out TrafficNode prev))
        {
            walker = prev;
            outputRoute.Add(walker);
            if (walker == fromNode)
            {
                break;
            }
        }

        outputRoute.Reverse();
        return outputRoute.Count > 1;
    }
}
