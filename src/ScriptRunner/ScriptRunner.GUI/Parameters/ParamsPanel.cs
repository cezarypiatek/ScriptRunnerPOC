using System.Collections.Generic;
using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class ParamsPanel
{
    public Panel Panel { get; set; }

    public IEnumerable<IControlRecord> ControlRecords { get; set; }
}