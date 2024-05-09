﻿using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;
using System.Text;

namespace BossMod;

// a lot of boss fights can be modeled as state machines
// by far the most common state has a single transition to a neighbouring state, and by far the most common transition is spell cast/finish by boss
// some bosses have multiple "phases"; when phase condition is triggered, initial state of the next phase is activated
// typical phase condition is boss reaching specific hp %
public class StateMachine
{
    [Flags]
    public enum StateHint
    {
        None = 0,
        GroupWithNext = 1 << 0, // this state is a logical 'substate' - it should be grouped together with next one for visualization
        BossCastStart = 1 << 1, // state ends when boss starts some cast
        BossCastEnd = 1 << 2, // state ends when boss ends a cast
        Tankbuster = 1 << 3, // state end is a tankbuster event - tank has to press some save
        Raidwide = 1 << 4, // state end is a raidwide damage event - someone better press some save
        Knockback = 1 << 5, // state end is a knockback event - it's a good time to use arm's length, or otherwise avoid being knocked into voidzones/walls/etc.
        DowntimeStart = 1 << 6, // at state end downtime starts - there are no targets to damage
        DowntimeEnd = 1 << 7, // at state end downtime ends
        PositioningStart = 1 << 8, // at state end a phase with movement or precise positioning requirements starts - we should be careful with charges etc.
        PositioningEnd = 1 << 9, // at state end positioning requirements are relaxed
        VulnerableStart = 1 << 10, // at state end some target becomes vulnerable and takes extra damage
        VulnerableEnd = 1 << 11, // at state end vulnerability phase ends
    }

    public class State
    {
        public uint ID = 0;
        public float Duration = 0; // estimated state duration
        public string Name = ""; // if name is empty, state is "hidden" from UI
        public string Comment = "";
        public List<Action> Enter = new(); // callbacks executed when state is activated
        public List<Action> Exit = new(); // callbacks executed when state is deactivated; note that this can happen unexpectedly, e.g. due to external reset or phase change
        public Func<float, int>? Update; // callback executed every frame when state is active; should return target state index for transition or -1 to remain in current state; argument = time since activation
        public State[]? NextStates; // potential successor states
        public StateHint EndHint = StateHint.None; // special flags for state end (used for visualization, autorotation, etc.)
    }

    public class Phase
    {
        public State InitialState;
        public string Name;
        public float ExpectedDuration; // if >= 0, this is 'expected' phase duration (for use by CD planner etc); <0 means 'calculate from state tree max time'
        public List<Action> Enter = new(); // callbacks executed when phase is activated
        public List<Action> Exit = new(); // callbacks executed when phase is deactivated; note that this can happen unexpectedly, e.g. due to external reset
        public Func<bool>? Update; // callback executed every frame when phase is active; should return whether transition to next phase should happen

        public Phase(State initialState, string name, float expectedDuration = -1)
        {
            InitialState = initialState;
            Name = name;
            ExpectedDuration = expectedDuration;
        }
    }

    public List<Phase> Phases { get; private init; }

    private DateTime _curTime;
    private DateTime _activation;
    private DateTime _phaseEnter;
    private DateTime _lastTransition;
    public float PrepullTimer; // TODO: reconsider...
    public float TimeSinceActivation => (float)(_curTime - _activation).TotalSeconds;
    public float TimeSincePhaseEnter => (float)(_curTime - _phaseEnter).TotalSeconds;
    public float TimeSinceTransition => (float)(_curTime - _lastTransition).TotalSeconds;
    public float TimeSinceTransitionClamped => MathF.Min(TimeSinceTransition, ActiveState?.Duration ?? 0);

    public int ActivePhaseIndex { get; private set; } = -1;
    public Phase? ActivePhase => Phases.ElementAtOrDefault(ActivePhaseIndex);
    public State? ActiveState { get; private set; }

    public StateMachine(List<Phase> phases)
    {
        Phases = phases;
    }

    public void Start(DateTime now)
    {
        _activation = _curTime = now;
        if (Phases.Count > 0)
            TransitionToPhase(0);
    }

    public void Reset()
    {
        TransitionToPhase(-1);
    }

    public void Update(DateTime now)
    {
        _curTime = now;
        while (ActivePhase != null)
        {
            bool transition = ActivePhase.Update?.Invoke() ?? false;
            if (!transition)
                break;
            Service.Log($"[StateMachine] Phase transition from {ActivePhaseIndex} '{ActivePhase.Name}', time={TimeSincePhaseEnter:f2}");
            TransitionToPhase(ActivePhaseIndex + 1);
        }
        while (ActiveState != null)
        {
            var transition = ActiveState.Update?.Invoke(TimeSinceTransition) ?? -1;
            var nextState = ActiveState.NextStates != null && transition >= 0 && transition < ActiveState.NextStates.Length ? ActiveState.NextStates[transition] : null;
            if (nextState == null)
                break;
            Service.Log($"[StateMachine] State transition from {ActiveState.ID:X} '{ActiveState.Name}' to {nextState.ID:X} '{nextState.Name}', overdue={TimeSinceTransition:f2}-{ActiveState.Duration:f2}={TimeSinceTransition - ActiveState.Duration:f2}");
            TransitionToState(nextState);
        }
    }

    public void Draw()
    {
        //(var activeName, var next) = ActiveState != null ? BuildComplexStateNameAndDuration(ActiveState, TimeSinceTransition, true) : ("Inactive", null);
        //ImGui.TextUnformatted($"Cur: {activeName}");

        //var future = BuildStateChain(next, " ---> ");
        //if (future.Length == 0)
        //{
        //    ImGui.TextUnformatted("");
        //}
        //else
        //{
        //    ImGui.TextUnformatted($"Then: {future}");
        //}

        var activePhase = ActivePhase;
        if (activePhase == null)
        {
            ImGui.TextUnformatted("Inactive");
            return;
        }
        var timeSincePhase = TimeSincePhaseEnter;
        DrawProgressBarPhase(activePhase, timeSincePhase);

        var activeState = ActiveState;
        var timeSinceTransition = TimeSinceTransition;
        if (activeState == null)
        {
            return;
        }
        DrawProgressBarState(activeState, timeSinceTransition);
    }

    private void DrawProgressBarState(State activeState, float timeSinceTransition)
    {
        float durationTotal = activeState.Duration;
        float durationRemaining = activeState.Duration - timeSinceTransition;
        if (durationRemaining < 0)
            durationRemaining = 0;
        string name = activeState.Name;
        DrawProgressBar(name, durationTotal, durationRemaining);
        foreach (var state in activeState.NextStates ?? Array.Empty<State>())
        {
            DrawNextState(state);
        }
    }

    private void DrawNextState(State state)
    {
        var name = state.Name;
        ImGui.Indent();
        ImGui.TextUnformatted($"Next: {(string.IsNullOrWhiteSpace(name) ? "Unknown" : name)}");
    }

    private void DrawProgressBarPhase(Phase activePhase, float timeSincePhase)
    {
        float durationTotal = activePhase.ExpectedDuration;
        float durationRemaining = activePhase.ExpectedDuration - timeSincePhase;
        if (durationRemaining < 0)
            durationRemaining = 0;
        string name = activePhase.Name;
        DrawProgressBar(name, durationTotal, durationRemaining);
    }

    private void DrawProgressBar(string name, float durationTotal, float durationRemaining)
    {
        // Calculate the progress ratio
        float progress = durationRemaining / durationTotal;

        // Set up the dimensions and position for the progress bar
        Vector2 startPos = ImGui.GetCursorScreenPos(); // Top-left of the progress bar
        float progressBarWidth = ImGui.GetContentRegionAvail().X; // Width based on the available space in the layout
        float progressBarHeight = 20; // Height of the progress bar

        // Background color and filled color
        var bgColor = ConvertVector4ToUint(ImGuiColors.DalamudGrey);
        var fgColor = ConvertVector4ToUint(ImGuiColors.DalamudRed);
        var textColor = ConvertVector4ToUint(new Vector4(0,0,0,1));

        // Draw background
        ImGui.GetWindowDrawList().AddRectFilled(startPos, new Vector2(startPos.X + progressBarWidth, startPos.Y + progressBarHeight), bgColor);

        // Draw filled portion
        ImGui.GetWindowDrawList().AddRectFilled(startPos, new Vector2(startPos.X + progressBarWidth * progress, startPos.Y + progressBarHeight), fgColor);

        // Text formatting and positioning
        string remainingTimeText = $"{durationRemaining:F2}s / {durationTotal:F2}s";
        if (durationTotal <= 0) remainingTimeText = "";
        string fullText = $"{name} {remainingTimeText}";
        Vector2 text_size = ImGui.CalcTextSize(fullText);

        // Position text centered in the progress bar
        Vector2 textPos = new Vector2(
            startPos.X + (progressBarWidth - text_size.X) * 0.5f,
            startPos.Y + (progressBarHeight - text_size.Y) * 0.5f
        );

        // Overlay the text onto the progress bar
        ImGui.GetWindowDrawList().AddText(textPos, textColor, fullText);

        // Advance cursor position below the progress bar
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + progressBarHeight + ImGui.GetStyle().ItemSpacing.Y);
    }
    public static uint ConvertVector4ToUint(Vector4 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        byte a = (byte)(color.W * 255);

        // Pack into uint: each component is shifted left into its position
        // R is shifted 24 bits left, G 16 bits, B 8 bits, A stays at the least significant bits
        return ((uint)r) | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
    }
    public string BuildStateChain(State? start, string sep, int maxCount = 5)
    {
        int count = 0;
        var res = new StringBuilder();
        while (start != null && count < maxCount)
        {
            (var name, var next) = BuildComplexStateNameAndDuration(start, 0, false);
            if (name.Length > 0)
            {
                if (res.Length > 0)
                    res.Append(sep);
                res.Append(name);
                ++count;
            }
            start = next;
        }
        return res.ToString();
    }

    public DateTime NextTransitionWithFlag(StateHint flag)
    {
        var time = _lastTransition;
        var next = ActiveState;
        while (next != null)
        {
            time = time.AddSeconds(next.Duration);
            if (next.EndHint.HasFlag(flag))
                return time;
            next = next.NextStates?.Length == 1 ? next.NextStates[0] : null;
        }
        return DateTime.MaxValue;
    }

    private (string, State?) BuildComplexStateNameAndDuration(State start, float timeActive, bool writeTime)
    {
        var res = new StringBuilder(start.Name);
        var timeLeft = MathF.Max(0, start.Duration - timeActive);
        if (writeTime && res.Length > 0)
        {
            res.Append($" in {timeLeft:f1}s");
            timeLeft = 0;
        }

        while (start.EndHint.HasFlag(StateHint.GroupWithNext) && start.NextStates?.Length == 1)
        {
            start = start.NextStates[0];
            timeLeft += MathF.Max(0, start.Duration);
            if (start.Name.Length > 0)
            {
                if (res.Length > 0)
                    res.Append(" + ");
                res.Append(start.Name);

                if (writeTime && timeLeft > 0)
                {
                    res.Append($" in {timeLeft:f1}s");
                    timeLeft = 0;
                }
            }
        }

        if (writeTime && timeLeft > 0)
        {
            if (res.Length == 0)
                res.Append("???");
            res.Append($" in {timeLeft:f1}s");
        }

        return (res.ToString(), start.NextStates?.Length == 1 ? start.NextStates[0] : null);
    }

    private void TransitionToPhase(int nextIndex)
    {
        if (ActivePhase != null)
        {
            TransitionToState(null);
            foreach (var cb in ActivePhase.Exit)
                cb();
        }

        ActivePhaseIndex = nextIndex;
        _phaseEnter = _curTime;

        if (ActivePhase != null)
        {
            foreach (var cb in ActivePhase.Enter)
                cb();
            TransitionToState(ActivePhase.InitialState);
        }
    }

    private void TransitionToState(State? nextState)
    {
        if (ActiveState != null)
            foreach (var cb in ActiveState.Exit)
                cb();

        ActiveState = nextState;
        _lastTransition = _curTime;

        if (ActiveState != null)
            foreach (var cb in ActiveState.Enter)
                cb();
    }
}
