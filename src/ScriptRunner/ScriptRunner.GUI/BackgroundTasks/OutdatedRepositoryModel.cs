namespace ScriptRunner.GUI.BackgroundTasks;

public class OutdatedRepositoryModel
{
    public string Path { get; set; }

    public OutdatedRepositoryModel(string path)
    {
        Path = path;
    }
}