using UnityEngine;

[CreateAssetMenu(fileName = "TrafficDriverProfile", menuName = "TrafficAI/Driver Profile")]
public class TrafficDriverProfile : ScriptableObject
{
    public DriverBehaviorMode behaviorMode = DriverBehaviorMode.Normal;

    [Range(0.5f, 2f)] public float speedMultiplier = 1f;
    [Range(0.3f, 2f)] public float reactionTimeMultiplier = 1f;
    [Range(0f, 0.2f)] public float mistakeProbability = 0.02f;
    [Range(0f, 0.5f)] public float laneChangeBias = 0.1f;

    public static TrafficDriverProfile CreateRuntimeDefault(DriverBehaviorMode mode)
    {
        TrafficDriverProfile profile = CreateInstance<TrafficDriverProfile>();
        profile.behaviorMode = mode;

        switch (mode)
        {
            case DriverBehaviorMode.Cautious:
                profile.speedMultiplier = 0.82f;
                profile.reactionTimeMultiplier = 1.3f;
                profile.mistakeProbability = 0.01f;
                profile.laneChangeBias = 0.03f;
                break;
            case DriverBehaviorMode.Aggressive:
                profile.speedMultiplier = 1.15f;
                profile.reactionTimeMultiplier = 0.8f;
                profile.mistakeProbability = 0.08f;
                profile.laneChangeBias = 0.25f;
                break;
            default:
                profile.speedMultiplier = 1f;
                profile.reactionTimeMultiplier = 1f;
                profile.mistakeProbability = 0.02f;
                profile.laneChangeBias = 0.1f;
                break;
        }

        return profile;
    }
}
