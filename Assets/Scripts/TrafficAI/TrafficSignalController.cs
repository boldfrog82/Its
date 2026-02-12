using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrafficSignalController : MonoBehaviour
{
    [Serializable]
    public class SignalPhase
    {
        public string name = "Phase";
        public float minDuration = 8f;
        public float maxDuration = 16f;
        public List<string> greenGroups = new List<string>();
        public List<string> yellowGroups = new List<string>();
    }

    [SerializeField] private List<SignalPhase> phases = new List<SignalPhase>();
    [SerializeField] private bool autoCycle = true;
    [SerializeField] private float yellowClearance = 2f;

    private int currentPhaseIndex;
    private float phaseTimer;
    private bool inYellowClearance;
    private readonly Dictionary<string, int> demandByGroup = new Dictionary<string, int>();

    public event Action<int> OnPhaseChanged;

    private void Start()
    {
        if (phases.Count == 0)
        {
            phases.Add(new SignalPhase());
        }

        phaseTimer = phases[0].minDuration;
    }

    private void Update()
    {
        if (!autoCycle || phases.Count == 0)
        {
            return;
        }

        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
        {
            return;
        }

        if (!inYellowClearance)
        {
            inYellowClearance = true;
            phaseTimer = yellowClearance;
            return;
        }

        inYellowClearance = false;
        MoveToNextPhaseByDemand();
    }

    public void RegisterDemand(string group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return;
        }

        if (!demandByGroup.ContainsKey(group))
        {
            demandByGroup[group] = 0;
        }

        demandByGroup[group]++;
    }

    public TrafficSignalState GetGroupState(string group)
    {
        if (string.IsNullOrEmpty(group) || phases.Count == 0)
        {
            return TrafficSignalState.Green;
        }

        SignalPhase phase = phases[currentPhaseIndex];

        if (inYellowClearance)
        {
            if (phase.greenGroups.Contains(group) || phase.yellowGroups.Contains(group))
            {
                return TrafficSignalState.Yellow;
            }

            return TrafficSignalState.Red;
        }

        if (phase.greenGroups.Contains(group))
        {
            return TrafficSignalState.Green;
        }

        if (phase.yellowGroups.Contains(group))
        {
            return TrafficSignalState.Yellow;
        }

        return TrafficSignalState.Red;
    }

    private void MoveToNextPhaseByDemand()
    {
        int selectedIndex = (currentPhaseIndex + 1) % phases.Count;
        int bestDemand = -1;

        for (int i = 0; i < phases.Count; i++)
        {
            int phaseDemand = CalculatePhaseDemand(i);
            if (phaseDemand > bestDemand)
            {
                bestDemand = phaseDemand;
                selectedIndex = i;
            }
        }

        currentPhaseIndex = selectedIndex;
        SignalPhase phase = phases[currentPhaseIndex];
        float normalizedDemand = Mathf.Clamp01(bestDemand / 8f);
        phaseTimer = Mathf.Lerp(phase.minDuration, phase.maxDuration, normalizedDemand);

        ClearServedDemands(phase);
        OnPhaseChanged?.Invoke(currentPhaseIndex);
    }

    private int CalculatePhaseDemand(int phaseIndex)
    {
        SignalPhase phase = phases[phaseIndex];
        int demand = 0;

        for (int i = 0; i < phase.greenGroups.Count; i++)
        {
            string group = phase.greenGroups[i];
            if (demandByGroup.TryGetValue(group, out int count))
            {
                demand += count;
            }
        }

        return demand;
    }

    private void ClearServedDemands(SignalPhase phase)
    {
        for (int i = 0; i < phase.greenGroups.Count; i++)
        {
            string group = phase.greenGroups[i];
            if (demandByGroup.ContainsKey(group))
            {
                demandByGroup[group] = 0;
            }
        }
    }
}
