using UnityEngine;

[CreateAssetMenu(fileName = "TrafficConflictMatrix", menuName = "TrafficAI/Conflict Matrix")]
public class TrafficConflictMatrix : ScriptableObject
{
    [SerializeField] private float[,] matrix = new float[4, 4]
    {
        { 0f, 0.9f, 0.4f, 1f },
        { 0.9f, 0f, 0.6f, 1f },
        { 0.4f, 0.6f, 0f, 0.8f },
        { 1f, 1f, 0.8f, 0f }
    };

    public float GetSeverity(TrafficTurnType a, TrafficTurnType b)
    {
        int i = Mathf.Clamp((int)a, 0, 3);
        int j = Mathf.Clamp((int)b, 0, 3);
        return matrix[i, j];
    }
}
