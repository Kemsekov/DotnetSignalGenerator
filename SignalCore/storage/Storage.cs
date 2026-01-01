using SQLite;
using SQLiteNetExtensions.Extensions;
namespace SignalCore.Storage;

/// <summary>
/// Attribute that helps identify sqlite database classes at runtime
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DataModel : Attribute
{
    public static List<Type> FindModels()
    {
        // Get the assembly where the classes are defined
        // Assembly.GetExecutingAssembly() works if the classes and this method are in the same project.
        // You might use Assembly.GetEntryAssembly() or typeof(SomeClassInTargetAssembly).Assembly otherwise.
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        // Find all types in the assembly that meet the criteria:
        IEnumerable<Type> discoverableTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                           type.GetCustomAttributes(typeof(DataModel), false).Any());

        return discoverableTypes.ToList();
    }
}

public class SignalStorage : IDisposable
{
    public SQLiteConnection db;
    public SignalStorage(string dbPath)
    {
        // Establish connection
        db = new SQLiteConnection(dbPath);
        // create tables for each type
        foreach(var m in DataModel.FindModels())
            db.CreateTable(m);
    }
    /// <summary>
    /// Fetch full session state from Db
    /// </summary>
    public SessionStateModel GetSessionState(long sessionId)
    {
        var session = db.Table<SessionModel>().FirstOrDefault(v=>v.Id==sessionId);
        if(session is null)
        {
            throw new ArgumentException($"Session with Id={sessionId} not found");
        }
        var generations = db.GetAllWithChildren<SessionGenerators>(v=>v.SessionId==sessionId).ToArray();
        var filters =  db.GetAllWithChildren<SessionFilters>(v=>v.SessionId==sessionId).ToArray();
        var transforms =  db.GetAllWithChildren<SessionTransforms>(v=>v.SessionId==sessionId).ToArray();
        var normalizations =  db.GetAllWithChildren<SessionNormalization>(v=>v.SessionId==sessionId).ToArray();
        var signalInstances =  db.GetAllWithChildren<SessionSignalInstance>(v=>v.SessionId==sessionId).ToArray();
        return new(
            session,
            generations,
            filters,
            transforms,
            normalizations,
            signalInstances
        );
    }

    public void Dispose()
    {
        db.Dispose();
    }
}
