using System;
using System.Threading.Tasks;

namespace ScriptRunner.GUI.ViewModels;

public class RealTimeScheduler
{
    private readonly TimeSpan _interval;
    private readonly TimeSpan _checkingInterval;
    private readonly Func<Task> _action;

    public RealTimeScheduler(TimeSpan interval, TimeSpan checkingInterval, Func<Task> action)
    {
        _interval = interval;
        _checkingInterval = checkingInterval;
        _action = action;
        _lastExecution = DateTime.MinValue;
    }

    private DateTime _lastExecution;

    public Task Run()
    {
        return Task.Run(async () =>
        {
            while (true)
            {
                var dateTime = DateTime.Now;

                try
                {
                    if (dateTime - _lastExecution > _interval)
                    {
                        await _action();
                    }
                }
                catch (Exception e)
                {

                }
                finally
                {
                    _lastExecution = dateTime;
                }

                await Task.Delay(_checkingInterval);
            }
        });
    }
}