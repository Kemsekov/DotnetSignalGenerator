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
        CreateAllTables();
    }
    public void CreateAllTables()
    {
        // create tables for each type
        foreach(var m in DataModel.FindModels())
            db.CreateTable(m);
    }
    public void AddSessionState(SessionStateModel m)
    {
        // if we encounter object with same name just replace it
        try
        {m.Session.Id=db.Find<SessionModel>(v=>v.Name==m.Session.Name).Id;}
        catch{}

        db.InsertOrReplaceWithChildren(m.Session.SignalX);
        db.InsertOrReplaceWithChildren(m.Session.SignalY);
        
        db.InsertOrReplaceAllWithChildren(m.Transforms.Select(v=>v.Transform),recursive:true);
        db.InsertOrReplaceAllWithChildren(m.Normalizations.Select(v=>v.Normalization),recursive:true);
        db.InsertOrReplaceAllWithChildren(m.Filters.Select(v=>v.Filter),recursive:true);
        db.InsertOrReplaceAllWithChildren(m.Generations.Select(v=>v.Generation),recursive:true);

        db.InsertOrReplaceWithChildren(m.Session,recursive:true);

        db.InsertOrReplaceAllWithChildren(m.Transforms,recursive:true);
        db.InsertOrReplaceAllWithChildren(m.Normalizations,recursive:true);
        db.InsertOrReplaceAllWithChildren(m.Filters,recursive:true);
        db.InsertOrReplaceAllWithChildren(m.Generations,recursive:true);
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
        System.Console.WriteLine($"SessionId {sessionId}");
        var generations = db
            .GetAllWithChildren<SessionGenerators>(v=>v.SessionId==sessionId,recursive:true)
            .ToArray();
        System.Console.WriteLine($"generations {generations.Length}");
        var filters =  db
            .GetAllWithChildren<SessionFilters>(v=>v.SessionId==sessionId,recursive:true)
            .ToArray();
        var transforms =  db
            .GetAllWithChildren<SessionTransforms>(v=>v.SessionId==sessionId,recursive:true)
            .ToArray();
        var normalizations =  db
            .GetAllWithChildren<SessionNormalization>(v=>v.SessionId==sessionId,recursive:true)
            .ToArray();
        return new(
            session,
            generations,
            filters,
            transforms,
            normalizations
        );
    }

    /// <summary>
    /// Delete a session and all related objects to properly handle cascade deletion
    /// </summary>
    public void DeleteSession(long id)
    {
        // First get the session state to identify all related objects
        var sessionState = GetSessionState(id);

        // Delete in reverse order of creation/relation to avoid foreign key constraint violations

        // Delete generator, filter, transform, and normalization relationship objects first
        db.DeleteAll(sessionState.Filters);
        db.DeleteAll(sessionState.Generations);
        db.DeleteAll(sessionState.Transforms);
        db.DeleteAll(sessionState.Normalizations);

        db.DeleteAll(sessionState.Filters.Select(v=>v.Filter));
        db.DeleteAll(sessionState.Generations.Select(v=>v.Generation));
        db.DeleteAll(sessionState.Transforms.Select(v=>v.Transform));
        db.DeleteAll(sessionState.Normalizations.Select(v=>v.Normalization));
      
        var session = sessionState.Session;
        if (session is not null)
        {
            // Delete associated signals if they exist
            db.Delete<NDarrayBinaryDataModel>(session.SignalXId);
            db.Delete<NDarrayBinaryDataModel>(session.SignalYId);

            // Finally delete the session
            db.Delete<SessionModel>(id);
        }
    }

    public void Dispose()
    {
        db.Dispose();
    }
}
