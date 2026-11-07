using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class RunningJobTabStateTests
{
    [Fact]
    public void IsRunning_TracksStatusAndNotifiesTheTab()
    {
        var viewModel = new RunningJobViewModel();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.Status = RunningJobStatus.Running;

        Assert.True(viewModel.IsRunning);
        Assert.Contains(nameof(RunningJobViewModel.IsRunning), notifications);

        viewModel.Status = RunningJobStatus.Finished;

        Assert.False(viewModel.IsRunning);
    }

    [Fact]
    public void HasPendingInteractiveInput_TracksTheCurrentRequestAndNotifiesTheTab()
    {
        var viewModel = new RunningJobViewModel();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.CurrentInteractiveInputs.Add(new InteractiveInputItem());

        Assert.True(viewModel.HasPendingInteractiveInput);
        Assert.Contains(nameof(RunningJobViewModel.HasPendingInteractiveInput), notifications);

        notifications.Clear();
        viewModel.CurrentInteractiveInputs.Clear();

        Assert.False(viewModel.HasPendingInteractiveInput);
        Assert.Contains(nameof(RunningJobViewModel.HasPendingInteractiveInput), notifications);
    }
}
