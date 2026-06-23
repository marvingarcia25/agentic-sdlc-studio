using AgenticSdlcStudio.Models;

namespace AgenticSdlcStudio.Services;

/// <summary>Tracks where the viewer is in the walkthrough and drives navigation.</summary>
public class JourneyState(JourneyService journey)
{
    private readonly JourneyService _journey = journey;

    public int CurrentIndex { get; private set; }

    public IReadOnlyList<Stage> Stages => _journey.Stages;
    public Stage Current => _journey.Stages[CurrentIndex];
    public bool IsFirst => CurrentIndex == 0;
    public bool IsLast => CurrentIndex == _journey.Stages.Count - 1;

    public event Action? Changed;

    public void GoTo(int index)
    {
        if (index < 0 || index >= _journey.Stages.Count || index == CurrentIndex) return;
        CurrentIndex = index;
        Changed?.Invoke();
    }

    /// <summary>Advance one stage; from the last stage, loop back to Intake.</summary>
    public void Advance() => GoTo(IsLast ? 0 : CurrentIndex + 1);

    public void Back() => GoTo(CurrentIndex - 1);
}
