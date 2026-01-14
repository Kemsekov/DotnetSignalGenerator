using SignalCore.Storage;

namespace SignalCore.GuiState;

public class GuiStateManager
{
    private readonly SignalStorage _signalStorage;

    public GuiStateManager(SignalStorage signalStorage)
    {
        _signalStorage = signalStorage;
    }

    public GuiSignalState LoadState(string objectName)
    {
        return GuiStateConverter.LoadFromDB(_signalStorage, objectName);
    }

    public void SaveState(GuiSignalState state)
    {
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(state);
        _signalStorage.AddSessionState(sessionStateModel);
    }

    public void DeleteState(string objectName)
    {
        var dbSessionInstance = _signalStorage.db.Table<SessionModel>().FirstOrDefault(v => v.Name == objectName);
        if (dbSessionInstance is not null)
        {
            _signalStorage.DeleteSession(dbSessionInstance.Id);
        }
    }

    public GuiSignalState[] GetAllStates()
    {
        return _signalStorage.db
            .Table<SessionModel>()
            .Select(v => new GuiSignalState(v.Name))
            .ToArray();
    }
}