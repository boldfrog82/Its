using UnityEngine;

public enum TrafficTurnType
{
    Through,
    Left,
    Right,
    UTurn
}

public enum TrafficSignalState
{
    Red,
    Yellow,
    Green
}

public enum DriverBehaviorMode
{
    Cautious,
    Normal,
    Aggressive
}

public enum LaneRole
{
    General,
    LeftOnly,
    ThroughOnly,
    RightOnly,
    Bus,
    Emergency
}
