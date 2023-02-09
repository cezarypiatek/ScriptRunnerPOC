using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ViewModels
{
    public class SearchBoxViewModel : ReactiveObject
    {
        private readonly List<ScriptConfig> _allActions;

        public string SearchFilter
        {
            get => _searchFilter;
            set => this.RaiseAndSetIfChanged(ref _searchFilter, value);
        }

        private string _searchFilter;

        private readonly ObservableAsPropertyHelper<IEnumerable<ScriptConfig>> _filteredActionList;
        public IEnumerable<ScriptConfig> FilteredActionList => _filteredActionList.Value;


        public SearchBoxViewModel(List<ScriptConfig> allActions)
        {
            _allActions = allActions;

            this.WhenAnyValue(x=>x.SearchFilter)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(text =>
                {
                    if (string.IsNullOrWhiteSpace(text) == false)
                    {
                        text = text.Trim();
                        return _allActions.Where(x => x.Name.ToUpper().Contains(text.ToUpper()));
                    }

                    return Array.Empty<ScriptConfig>();
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredActionList, out _filteredActionList);
        }
    }
}
