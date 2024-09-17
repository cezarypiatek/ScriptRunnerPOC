using ReactiveUI;

namespace ScriptRunner.GUI.BackgroundTasks;

public class OutdatedRepositoryModel:ReactiveObject
{
    public string Path { get; set; }
    public string BranchName { get; set; }

    private bool _isPulling;

    public bool IsPulling
    {
        get => _isPulling;
        set => this.RaiseAndSetIfChanged(ref _isPulling, value);
    }
}