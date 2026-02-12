#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrafficToolingWindow : EditorWindow
{
    [MenuItem("Tools/TrafficAI/Validator & Bake")]
    private static void Open()
    {
        GetWindow<TrafficToolingWindow>("TrafficAI Tools");
    }

    private Vector2 scroll;
    private readonly List<string> issues = new List<string>();

    private void OnGUI()
    {
        GUILayout.Label("TrafficAI Tooling", EditorStyles.boldLabel);

        if (GUILayout.Button("Run Validation"))
        {
            RunValidation();
        }

        if (GUILayout.Button("Bake Lane Links"))
        {
            BakeLaneLinks();
        }

        if (GUILayout.Button("Run One-Click Check Pipeline"))
        {
            RunValidation();
            BakeLaneLinks();
            AssetDatabase.SaveAssets();
        }

        GUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string issue in issues)
        {
            EditorGUILayout.HelpBox(issue, MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
    }

    private void RunValidation()
    {
        issues.Clear();

        TrafficLane[] lanes = FindObjectsOfType<TrafficLane>();
        TrafficNode[] nodes = FindObjectsOfType<TrafficNode>();

        foreach (TrafficLane lane in lanes)
        {
            if (lane.nodes == null || lane.nodes.Count == 0)
            {
                issues.Add($"Lane '{lane.name}' has no nodes.");
            }

            for (int i = 0; i < lane.nodes.Count; i++)
            {
                TrafficNode node = lane.nodes[i];
                if (node == null)
                {
                    issues.Add($"Lane '{lane.name}' has null node at index {i}.");
                    continue;
                }

                if (node.lane != lane)
                {
                    issues.Add($"Node '{node.name}' lane mismatch. Expected '{lane.name}'.");
                }
            }

            if (lane.adjacentLanes != null)
            {
                for (int i = 0; i < lane.adjacentLanes.Count; i++)
                {
                    if (lane.adjacentLanes[i] == lane)
                    {
                        issues.Add($"Lane '{lane.name}' references itself as adjacent lane.");
                    }
                }
            }
        }

        foreach (TrafficNode node in nodes)
        {
            if (node.nextNodes == null || node.nextNodes.Count == 0)
            {
                issues.Add($"Node '{node.name}' has no next nodes.");
            }

            if (node.isStopLine && node.intersection == null)
            {
                issues.Add($"Stop line node '{node.name}' has no intersection reference.");
            }

            if (node.isStopLine && string.IsNullOrEmpty(node.signalGroup))
            {
                issues.Add($"Stop line node '{node.name}' has empty signalGroup.");
            }
        }

        TrafficDestination[] destinations = FindObjectsOfType<TrafficDestination>();
        TrafficRoutePlanner[] planners = FindObjectsOfType<TrafficRoutePlanner>();
        TrafficIntersection[] intersections = FindObjectsOfType<TrafficIntersection>();

        if (destinations.Length == 0)
        {
            issues.Add("No TrafficDestination objects found. Strategic routing will be disabled.");
        }

        if (planners.Length == 0)
        {
            issues.Add("No TrafficRoutePlanner found. Destination-based routing cannot build paths.");
        }

        for (int i = 0; i < intersections.Length; i++)
        {
            TrafficIntersectionMicrosim microsim = intersections[i].GetComponent<TrafficIntersectionMicrosim>();
            if (microsim == null)
            {
                issues.Add($"Intersection '{intersections[i].name}' has no TrafficIntersectionMicrosim component.");
            }
        }

        if (issues.Count == 0)
        {
            issues.Add("Validation passed with zero issues.");
        }
    }

    private void BakeLaneLinks()
    {
        TrafficLane[] lanes = FindObjectsOfType<TrafficLane>();

        foreach (TrafficLane lane in lanes)
        {
            if (lane.nodes == null)
            {
                continue;
            }

            for (int i = 0; i < lane.nodes.Count; i++)
            {
                TrafficNode node = lane.nodes[i];
                if (node == null)
                {
                    continue;
                }

                node.lane = lane;

                if (i + 1 < lane.nodes.Count)
                {
                    TrafficNode next = lane.nodes[i + 1];
                    if (next != null && !node.nextNodes.Contains(next))
                    {
                        node.nextNodes.Add(next);
                        EditorUtility.SetDirty(node);
                    }
                }
            }

            EditorUtility.SetDirty(lane);
        }

        issues.Add("Bake complete: lane-node references and forward links synchronized.");
    }
}
#endif
