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
        private readonly List<ScriptConfigWithArgumentSet> _allActions;

        public string SearchFilter
        {
            get => _searchFilter;
            set => this.RaiseAndSetIfChanged(ref _searchFilter, value);
        }

        private string _searchFilter;

        private readonly ObservableAsPropertyHelper<IEnumerable<ScriptConfigWithArgumentSet>> _filteredActionList;
        public IEnumerable<ScriptConfigWithArgumentSet> FilteredActionList => _filteredActionList.Value;


        public SearchBoxViewModel(List<ScriptConfig> allActions)
        {
            _allActions = allActions.SelectMany(x=> x.PredefinedArgumentSets.Select(p=> new ScriptConfigWithArgumentSet()
            {
                Config = x,
                ArgumentSet = p,
                FullName = p.Description == "<default>"? x.FullName : $"{x.FullName} - {p.Description}"
            })).ToList();

            this.WhenAnyValue(x=>x.SearchFilter)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(text =>
                {
                    if (string.IsNullOrWhiteSpace(text) == false)
                    {
                        text = text.Trim();
                        var tokens = text.ToUpper().Split(' ');
                        return _allActions.Select(a =>
                        {
                            var score = tokens.Sum(p => a.FullName.ToUpper().Contains(p) ? p.Length : -1000);
                            return (score, a);
                        })
                            .Where(p => p.score > 0)
                            .OrderByDescending(p => p.score)
                            .Select(p=>p.a);

                        //return _allActions.Where(x =>
                        //{
                        //    if (x.ArgumentSet.Description == "<default>")
                        //    {
                        //        return x.Config.Name.ToUpper().Contains(text.ToUpper());
                        //    }

                        //    return x.ArgumentSet.Description.ToUpper().Contains(text.ToUpper());
                        //});
                    }

                    return Enumerable.Empty<ScriptConfigWithArgumentSet>();
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredActionList, out _filteredActionList);
        }
    }

    public class ScriptConfigWithArgumentSet
    {
        public ScriptConfig Config { get; set; }
        public ArgumentSet ArgumentSet{ get; set; }
        public string FullName { get; set; }
        
    }
}
