using ScriptRunner.GUI.ViewModels;
using System.Reactive.Linq;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class TroubleshootingAlertTests
{
    [Fact]
    public void DismissTroubleshootingAlert_RemovesOnlySelectedAlert()
    {
        var viewModel = new RunningJobViewModel();
        var dismissed = new TroubleshootingAlertViewModel(
            "Dismiss me",
            TroubleShootingSeverity.Warning,
            DateTimeOffset.Now,
            viewModel.DismissTroubleshootingAlert);
        var remaining = new TroubleshootingAlertViewModel(
            "Keep me",
            TroubleShootingSeverity.Info,
            DateTimeOffset.Now,
            viewModel.DismissTroubleshootingAlert);

        viewModel.Alerts.Add(dismissed);
        viewModel.Alerts.Add(remaining);

        dismissed.DismissCommand.Execute().Subscribe(_ => { });

        Assert.DoesNotContain(dismissed, viewModel.Alerts);
        Assert.Contains(remaining, viewModel.Alerts);
    }

    [Fact]
    public void Alerts_ShowAndHideNotificationRailWithoutLosingResizedWidth()
    {
        var viewModel = new RunningJobViewModel();
        var alert = new TroubleshootingAlertViewModel(
            "Resize me",
            TroubleShootingSeverity.Info,
            DateTimeOffset.Now,
            viewModel.DismissTroubleshootingAlert);

        Assert.Equal(0, viewModel.NotificationRailWidth.Value);

        viewModel.Alerts.Add(alert);
        Assert.Equal(320, viewModel.NotificationRailWidth.Value);

        viewModel.NotificationRailWidth = new Avalonia.Controls.GridLength(420);
        alert.DismissCommand.Execute().Subscribe(_ => { });

        Assert.Equal(0, viewModel.NotificationRailWidth.Value);

        viewModel.Alerts.Add(new TroubleshootingAlertViewModel(
            "Show again",
            TroubleShootingSeverity.Success,
            DateTimeOffset.Now,
            viewModel.DismissTroubleshootingAlert));

        Assert.Equal(420, viewModel.NotificationRailWidth.Value);
    }

    [Fact]
    public void TimestampText_IsHumanReadableForCurrentDay()
    {
        var detectedAt = DateTimeOffset.Now;
        var alert = new TroubleshootingAlertViewModel(
            "Timestamped",
            TroubleShootingSeverity.Info,
            detectedAt,
            _ => { });

        Assert.Equal($"Today at {detectedAt.ToLocalTime():HH:mm:ss}", alert.TimestampText);
    }
}
