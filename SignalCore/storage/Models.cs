// TODO: add tests that each object is properly created/saved/fetched and recreated
// via ObjectFactory and database
using System.Numerics;
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


public class SignalStatistic
{
    public string Name{get;set;} = "";
    public float Statistic{get;set;}
}
// sessions Table
[DataModel]
public class SessionModel
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    /// <summary>
    /// Session Name
    /// </summary>
    [Unique]
    public string Name { get; set; } = "";
    /// <summary>
    /// Expression that was used to combine sources
    /// </summary>
    public string Expression { get; set; } = "";
    /// <summary>
    /// How many points for generation was used
    /// </summary>
    public int ComputePoints { get; set; } = 1024;

    [ForeignKey(typeof(NDarrayBinaryDataModel))]
    public long SignalXId { get; set; }
    /// <summary>
    /// Generated signal X value
    /// </summary>
    [ManyToOne("SignalXId")]
    public NDarrayBinaryDataModel SignalX{get;set;} = new();
    
    [ForeignKey(typeof(NDarrayBinaryDataModel))]
    public long SignalYId { get; set; }
    /// <summary>
    /// Generated signal Y value
    /// </summary>
    [ManyToOne("SignalYId")]
    public NDarrayBinaryDataModel SignalY{get;set;} = new();

    [TextBlob("SignalStatsBlobbed")]
    public List<SignalStatistic> SignalStatistics{get;set;} = [];
    public string SignalStatsBlobbed{get;set;} = "[]";
}

/// <summary>
/// Base class for models that defines ManyToOne relationship with session
/// </summary>
public abstract class ManyToSession
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    public string VarName { get; set; } = "";
    [ForeignKey(typeof(SessionModel))]
    public long SessionId { get; set; }
    [ManyToOne(CascadeOperations = CascadeOperation.All)]
    public SessionModel? Session { get; set; }
}

// Relation table session_generators
[DataModel]
public class SessionGenerators : ManyToSession
{
    [ForeignKey(typeof(GenerationModel))]
    public long GenerationId { get; set; }
    [ManyToOne(CascadeOperations = CascadeOperation.All)]
    public GenerationModel Generation { get; set; } = new();
}

/// <summary>
/// Operation that transforms signal
/// </summary>
public abstract class OperationManyToSession : ManyToSession
{
    /// <summary>
    /// Whether operation is enabled in computation
    /// </summary>
    /// <value></value>
    public bool Enabled{get;set;}
}

// Relation table session_transforms
[DataModel]
public class SessionTransforms : OperationManyToSession
{
    [ForeignKey(typeof(TransformModel))]
    public long TransformId { get; set; }
    [ManyToOne(CascadeOperations = CascadeOperation.All)]
    public TransformModel Transform { get; set; } = new();
}

// Relation table session_filters
[DataModel]
public class SessionFilters : OperationManyToSession
{
    [ForeignKey(typeof(FilterModel))]
    public long FilterId { get; set; }
    [ManyToOne(CascadeOperations = CascadeOperation.All)]
    public FilterModel Filter { get; set; } = new();
}

// Relation table session_normalizations
[DataModel]
public class SessionNormalization : OperationManyToSession
{
    [ForeignKey(typeof(NormalizationModel))]
    public long NormalizationId { get; set; }
    [ManyToOne(CascadeOperations = CascadeOperation.All)]
    public NormalizationModel Normalization { get; set; }=new();
}

// Composite object (not a table)
public class SessionStateModel{
    public SessionStateModel(
        SessionModel session,
        SessionGenerators[] generations,
        SessionFilters[] filters,
        SessionTransforms[] transforms,
        SessionNormalization[] normalizations)
    {
        Session = session;
        Generations = generations;
        Filters = filters;
        Transforms = transforms;
        Normalizations = normalizations;
    }
    public SessionModel Session{get;set;} = new();
    public SessionGenerators[] Generations{get;set;}=[];
    public SessionFilters[] Filters{get;set;}=[];
    public SessionTransforms[] Transforms{get;set;}=[];
    public SessionNormalization[] Normalizations{get;set;}=[];
};
