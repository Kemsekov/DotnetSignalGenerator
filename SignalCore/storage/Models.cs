// TODO: add tests that each object is properly created/saved/fetched and recreated
// via ObjectFactory and database
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace SignalCore.Storage;

/// <summary>
/// Base class for models that contains ObjectFactory class
/// </summary>
public abstract class FactoryModel
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    public string ObjectFactoryJson { get; set; } = "";
    [Ignore]
    public ObjectFactory Factory
    {
        get=> ObjectFactory.FromJson(ObjectFactoryJson);
        set=> ObjectFactoryJson=value.ToJson();
    }
}

// generations Table
[DataModel]
public class GenerationModel : FactoryModel
{
    public float TStart { get; set; }
    public float TEnd { get; set; }
    public int Points { get; set; }
    public float Amplitude { get; set; }
    public float Frequency { get; set; }
    public float Phase { get; set; }

}
// filters Table
[DataModel]
public class FilterModel : FactoryModel {}
// transforms Table
[DataModel]
public class TransformModel  : FactoryModel {}
// normalizations Table
[DataModel]
public class NormalizationModel  : FactoryModel {}
// sessions Table
[DataModel]
public class SessionModel
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

// signals_factory Table
[DataModel]
public class SignalFactoryModel
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Expression { get; set; } = "";
}

/// <summary>
/// Base class for models that defines ManyToOne relationship with session
/// </summary>
public abstract class ManyToSession
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    [ForeignKey(typeof(SessionModel))]
    public string VarName { get; set; } = "";
    public long SessionId { get; set; }
    [ManyToOne]
    public SessionModel? Session { get; set; }
}
// table session_signals_instance
[DataModel]
public class SignalInstanceModel : ManyToSession
{
    [ForeignKey(typeof(SignalFactoryModel))]
    public long SignalFactoryId { get; set; }
    [ManyToOne]
    public SignalFactoryModel? SignalFactory { get; set; }
    public byte[] DataRaw { get; set; } = [];
    public float SignalMin{get;set;}
    public float SignalMax{get;set;}
    public string DataShape{get;set;}="";
    ndarray? _data = null;
    // TODO: add test for this conversation
    /// <summary>
    /// Signal data, converted to UInt8 ndarray. Make sure to not forget
    /// </summary>
    [Ignore]
    public ndarray Data
    {
        get
        {
            if(_data is not null) return _data;
            if(SignalMin==SignalMax || DataShape=="")
                throw new ArgumentException("Object was not properly initialized");
            var newWidth = (SignalMax-SignalMin)/255f;
            var transformed = DataRaw.Select(v=>v*newWidth+SignalMin).ToArray();
            var shape = DataShape.Split(' ').Select(long.Parse).ToArray();
            _data = np.array(transformed,np.Float32,copy:false).reshape(shape);
            return _data;
        }
        set
        {
            DataShape = string.Join(' ',value.shape.iDims);
            SignalMin = np.min(value).single();
            SignalMax = np.max(value).single();
            var width = 1/(SignalMax-SignalMin)*255;
            DataRaw = value.AsFloatArray().Select(v=>(byte)((v-SignalMin)*width)).ToArray();
        }
    }
}

// Relation table session_generators
[DataModel]
public class SessionGenerators : ManyToSession
{
    [ForeignKey(typeof(GenerationModel))]
    public long GenerationId { get; set; }
    [ManyToOne]
    public GenerationModel? Generation { get; set; }
}

// Relation table session_transforms
[DataModel]
public class SessionTransforms : ManyToSession
{
    [ForeignKey(typeof(TransformModel))]
    public long TransformId { get; set; }
    [ManyToOne]
    public TransformModel? Transform { get; set; }
}

// Relation table session_filters
[DataModel]
public class SessionFilters : ManyToSession
{
    [ForeignKey(typeof(FilterModel))]
    public long FilterId { get; set; }
    [ManyToOne]
    public FilterModel? Filter { get; set; }
}

// Relation table session_normalizations
[DataModel]
public class SessionNormalization : ManyToSession
{
    [ForeignKey(typeof(NormalizationModel))]
    public long NormalizationId { get; set; }
    [ManyToOne]
    public NormalizationModel? Normalization { get; set; }
}

// Relation table session_signals_instance
[DataModel]
public class SessionSignalInstance : ManyToSession
{
    [ForeignKey(typeof(SignalInstanceModel))]
    public long SignalInstanceId { get; set; }

    [ManyToOne]
    public SignalInstanceModel? SignalInstance { get; set; }
}

// Composite object (not a table)
public record SessionStateModel(
    SessionModel Session,
    SessionGenerators[] Generations,
    SessionFilters[] Filters,
    SessionTransforms[] Transforms,
    SessionNormalization[] Normalizations,
    SessionSignalInstance[] CreatedSignals
);
