using UnityEngine;

[CreateAssetMenu(fileName = "TrafficPlaytestTuning", menuName = "TrafficAI/Playtest Tuning")]
public class TrafficPlaytestTuning : ScriptableObject
{
    [Range(0.5f, 2f)] public float globalSpeedScale = 1f;
    [Range(0.5f, 2f)] public float globalDensityScale = 1f;
    [Range(0f, 2f)] public float globalAggressionScale = 1f;
    [Range(0f, 3f)] public float intersectionCautionScale = 1f;
    [Range(0f, 2f)] public float laneChangeFrequencyScale = 1f;
    [Range(0.2f, 2f)] public float sensorDistanceScale = 1f;
}
