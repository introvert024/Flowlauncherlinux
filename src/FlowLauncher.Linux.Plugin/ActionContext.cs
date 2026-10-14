namespace FlowLauncher.Linux.Plugin;

public class ActionContext
{
    public bool SpecialKeyState { get; init; }
    public bool CtrlPressed { get; init; }
    public bool ShiftPressed { get; init; }
    public bool AltPressed { get; init; }
}
