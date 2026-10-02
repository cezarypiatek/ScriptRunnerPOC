using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class CategoryFilterOptionTests
{
    [Fact]
    public void SelectedFilterIsHighlightedAndOtherFiltersAreDimmed()
    {
        var selected = new CategoryFilterOption("Deploy", "Deploy", false);
        var other = new CategoryFilterOption("Build", "Build", false);

        selected.UpdateState("Deploy");
        other.UpdateState("Deploy");

        Assert.True(selected.IsSelected);
        Assert.False(selected.IsDimmed);
        Assert.False(other.IsSelected);
        Assert.True(other.IsDimmed);
    }

    [Fact]
    public void AllFilterClearsDimming()
    {
        var category = new CategoryFilterOption("Deploy", "Deploy", false);

        category.UpdateState("Deploy");
        category.UpdateState(MainWindowViewModel.AllCategoryFilter);

        Assert.False(category.IsSelected);
        Assert.False(category.IsDimmed);
    }
}
