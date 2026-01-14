using System.Numerics;
using NumpyDotNet;
using SignalCore.Storage;

namespace SignalCore.GuiState;

/// <summary>
/// Represents the core state of a GUI signal instance without any GUI-specific dependencies
/// </summary>
public class GuiSignalState
{
    public string ObjectName { get; set; } = "";
    
    public ComputedSignal? ComputedSignal { get; set; }
    
    public (string name, float stat)[]? SignalStatistics { get; set; }
    
    public int CompletedPercent { get; set; } = 0;
    
    public IEnumerable<ObjectFactory> Sources { get; set; } = [];
    
    public string Expression { get; set; } = "";
    
    public IEnumerable<(bool visible, ObjectFactory factory)> Filters { get; set; } = [];
    
    public ObjectFactory? SignalParams { get; set; }

    public GuiSignalState() { }
    
    public GuiSignalState(
        string objectName,
        ComputedSignal? computedSignal = null,
        (string name, float stat)[]? signalStatistics = null,
        int completedPercent = 0,
        IEnumerable<ObjectFactory>? sources = null,
        string expression = "",
        IEnumerable<(bool visible, ObjectFactory factory)>? filters = null,
        ObjectFactory? signalParams = null)
    {
        ObjectName = objectName;
        ComputedSignal = computedSignal;
        SignalStatistics = signalStatistics;
        CompletedPercent = completedPercent;
        Sources = sources ?? [];
        Expression = expression;
        Filters = filters ?? [];
        SignalParams = signalParams;
    }

    public GuiSignalState Clone()
    {
        return new GuiSignalState(
            ObjectName,
            ComputedSignal?.Clone(),
            SignalStatistics?.ToArray(),
            CompletedPercent,
            Sources?.Select(f => f.Clone()).ToArray(),
            Expression,
            Filters?.Select(f => (f.visible, f.factory.Clone())).ToArray(),
            SignalParams?.Clone()
        );
    }
}