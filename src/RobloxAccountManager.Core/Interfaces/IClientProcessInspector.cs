using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Core.Interfaces;

public interface IClientProcessInspector
{
    ProcessInspectionResult Inspect(int processId);
    int CountRunningRobloxPlayers();
    bool HasUnmanagedRobloxPlayer(IReadOnlyCollection<int> managedProcessIds);
    bool Terminate(ClientProcessSnapshot expectedProcess);
}