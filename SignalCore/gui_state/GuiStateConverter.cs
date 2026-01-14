using System.Numerics;
using NumpyDotNet;
using SignalCore.Storage;
using SignalCore.Parameters;
using SQLiteNetExtensions.Extensions;

namespace SignalCore.GuiState;

public static class GuiStateConverter
{
    public static GuiSignalState LoadFromDB(SignalStorage signalStorage, string objectName)
    {
        var s = signalStorage.db.Table<SessionModel>().FirstOrDefault(v => v.Name == objectName);
        if (s is null)
            throw new KeyNotFoundException($"Cannot find database signal instance with name {objectName}");

        s = signalStorage.db.GetWithChildren<SessionModel>(s.Id);
        // Explicitly reload the signal data models to ensure they are properly loaded
        s.SignalX = signalStorage.db.Get<NDarrayBinaryDataModel>(s.SignalXId);
        s.SignalY = signalStorage.db.Get<NDarrayBinaryDataModel>(s.SignalYId);

        var sFields = signalStorage.GetSessionState(s.Id);

        var computedSignal = new ComputedSignal(
            s.SignalX.GetNdarray(),
            s.SignalY.GetNdarray(),
            s.SignalStatistics.Select(v => (v.Statistic, v.Name)).ToArray()
        );

        var filters = sFields.Filters.Select(v => (
            visible: v.Enabled,
            name: v.VarName,
            factory: v.Filter.Factory
        ));

        var transforms = sFields.Transforms.Select(v => (
            visible: v.Enabled,
            name: v.VarName,
            factory: v.Transform.Factory
        ));

        var generations = sFields.Generations.Select(v => (
            name: v.VarName,
            factory: v.Generation.Factory
        ));

        var norms = sFields.Normalizations.Select(v => (
            visible: v.Enabled,
            name: v.VarName,
            factory: v.Normalization.Factory
        ));

        var ops = new[] { filters, transforms, norms }
            .SelectMany(v => v)
            .OrderBy(v => v.name)
            .Select(v => (v.visible, v.factory))
            .ToArray() ?? throw new Exception();

        return new GuiSignalState(
            objectName: s.Name,
            computedSignal: computedSignal,
            completedPercent: s.CompletedPercent,
            expression: s.Expression,
            filters: ops,
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [
                    ("computePoints", s.ComputePoints),
                    ("renderPoints", 512)
                ]
            ),
            signalStatistics: s.SignalStatistics.Select(v => (v.Name, v.Statistic)).ToArray(),
            sources: generations.Select(v => v.factory).ToArray()
        );
    }
    
    public static SessionStateModel ToSessionStateModel(GuiSignalState state)
    {
        // Create the main session model
        var sessionModel = new SessionModel
        {
            Name = state.ObjectName,
            Expression = state.Expression,
            ComputePoints = state.SignalParams?.ConstructorArguments.ContainsKey("computePoints") ?? false
                ? Convert.ToInt32(state.SignalParams?.ConstructorArguments["computePoints"].Instance ?? 1024)
                : 1024,
            CompletedPercent = state.CompletedPercent,
            SignalX = new NDarrayBinaryDataModel(),
            SignalY = new NDarrayBinaryDataModel(),
            SignalStatistics = state.SignalStatistics?.Select(v => new SignalStatistic { Name = v.name, Statistic = v.stat }).ToList() ?? []
        };

        sessionModel.SignalX.SetNdarray(0.ToNdarray());
        sessionModel.SignalY.SetNdarray(0.ToNdarray());

        // Set signal data if ComputedSignal is available
        if (state.ComputedSignal != null)
        {
            // Handle ImageData first (2D signals like wavelet transforms)
            var imd = state.ComputedSignal.ImageData;
            if (imd is not null)
            {
                // For 2D image data, store it in SignalY and leave X as initialization value
                sessionModel.SignalY.SetNdarray(imd);
            }
            // Otherwise handle 1D signals
            else if (state.ComputedSignal.X != null && state.ComputedSignal.Y != null)
            {
                var xArray = np.array(state.ComputedSignal.X, np.Float32);
                // For complex signals, we need to handle the YImag part
                ndarray yArray;
                if (state.ComputedSignal.YImag != null)
                {
                    // Create complex array from real and imaginary parts
                    var complexValues = new System.Numerics.Complex[state.ComputedSignal.Y.Length];
                    for (int i = 0; i < state.ComputedSignal.Y.Length; i++)
                    {
                        complexValues[i] = new System.Numerics.Complex(state.ComputedSignal.Y[i], state.ComputedSignal.YImag[i]);
                    }
                    yArray = np.array(complexValues, np.Complex);
                }
                else
                {
                    yArray = np.array(state.ComputedSignal.Y, np.Float32);
                }

                sessionModel.SignalX.SetNdarray(xArray);
                sessionModel.SignalY.SetNdarray(yArray);
            }
        }

        // Create relation models for generations (sources)
        var generations = state.Sources?.Select((factory, index) => new SessionGenerators
        {
            Session = sessionModel,
            VarName = $"source_{index}",
            Generation = new GenerationModel
            {
                Factory = factory
            }
        }).ToArray() ?? Array.Empty<SessionGenerators>();

        // Separate the mixed operations (filters, transforms, normalizations) from the Filters property
        var allOperations = state.Filters?.Select((v, ind) => (op: v, ind: ind)) ?? [];

        var extractedFilters = allOperations
            .Where(v => IsFilterOperation(v.op))
            .Select(opWithInd => new SessionFilters
            {
                Session = sessionModel,
                VarName = $"operation_{opWithInd.ind}",
                Enabled = opWithInd.op.visible,
                Filter = new FilterModel
                {
                    Factory = opWithInd.op.factory
                }
            }).ToArray();

        var extractedTransforms = allOperations
            .Where(v => IsTransformOperation(v.op))
            .Select(v => new SessionTransforms
            {
                Session = sessionModel,
                VarName = $"operation_{v.ind}",
                Enabled = v.op.visible,
                Transform = new TransformModel
                {
                    Factory = v.op.factory
                }
            }).ToArray();

        var extractedNorms = allOperations
            .Where(v => IsNormalizationOperation(v.op))
            .Select(v => new SessionNormalization
            {
                Session = sessionModel,
                VarName = $"operation_{v.ind}",
                Enabled = v.op.visible,
                Normalization = new NormalizationModel
                {
                    Factory = v.op.factory
                }
            }).ToArray();

        return new SessionStateModel(
            sessionModel,
            generations,
            extractedFilters,
            extractedTransforms,
            extractedNorms
        );
    }

    // Helper methods to determine the type of operation
    private static bool IsFilterOperation((bool visible, ObjectFactory factory) op)
    {
        // Check if the factory's object type is related to filtering
        // This could be based on interface implementation or naming convention
        var typeName = op.factory.Type.Name.ToLower();
        // Check if it's specifically a filter but not a transform or normalization
        return (typeName.Contains("filter") || typeName.Contains("noise")) &&
               !typeName.Contains("transform") &&
               !typeName.Contains("normalize");
    }

    private static bool IsTransformOperation((bool visible, ObjectFactory factory) op)
    {
        // Check if the factory's object type is related to transformation
        var typeName = op.factory.Type.Name.ToLower();
        return typeName.Contains("transform") || typeName.Contains("fft") || typeName.Contains("fwt");
    }

    private static bool IsNormalizationOperation((bool visible, ObjectFactory factory) op)
    {
        // Check if the factory's object type is related to normalization
        var typeName = op.factory.Type.Name.ToLower();
        return typeName.Contains("normalize") || typeName.Contains("normalization");
    }
}