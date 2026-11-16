using System.ComponentModel;
using System.Diagnostics;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Infrastructure.RobloxIntegration;

public sealed class RobloxProcessInspector : IClientProcessInspector
{
    private const string PlayerProcessName = "RobloxPlayerBeta";

    public int CountRunningRobloxPlayers()
    {
        var runningCount = 0;
        foreach (var process in Process.GetProcessesByName(PlayerProcessName))
        {
            using (process)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        runningCount++;
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process exited while the process list was being inspected.
                }
                catch (Win32Exception)
                {
                    runningCount++;
                }
            }
        }

        return runningCount;
    }

    public ProcessInspectionResult Inspect(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return new ProcessInspectionResult(ProcessInspectionStatus.Exited);
            }

            try
            {
                var executablePath = process.MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return new ProcessInspectionResult(
                        ProcessInspectionStatus.Unavailable,
                        Error: "The operating system did not provide the process executable path.");
                }

                return new ProcessInspectionResult(
                    ProcessInspectionStatus.Running,
                    new ClientProcessSnapshot(
                        process.Id,
                        process.StartTime.ToUniversalTime(),
                        Path.GetFullPath(executablePath)));
            }
            catch (Win32Exception)
            {
                return new ProcessInspectionResult(
                    ProcessInspectionStatus.Unavailable,
                    Error: "The operating system denied access to process details.");
            }
        }
        catch (ArgumentException)
        {
            return new ProcessInspectionResult(ProcessInspectionStatus.Exited);
        }
        catch (InvalidOperationException)
        {
            return new ProcessInspectionResult(ProcessInspectionStatus.Exited);
        }
        catch (Win32Exception)
        {
            return new ProcessInspectionResult(
                ProcessInspectionStatus.Unavailable,
                Error: "The operating system denied access to process details.");
        }
    }

    public bool HasUnmanagedRobloxPlayer(IReadOnlyCollection<int> managedProcessIds)
    {
        foreach (var process in Process.GetProcessesByName(PlayerProcessName))
        {
            using (process)
            {
                try
                {
                    if (!process.HasExited && !managedProcessIds.Contains(process.Id))
                    {
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process exited while the process list was being inspected.
                }
                catch (Win32Exception)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool Terminate(ClientProcessSnapshot expectedProcess)
    {
        ArgumentNullException.ThrowIfNull(expectedProcess);

        var inspection = Inspect(expectedProcess.ProcessId);
        if (inspection.Status != ProcessInspectionStatus.Running ||
            inspection.Snapshot is null ||
            !Matches(inspection.Snapshot, expectedProcess))
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(expectedProcess.ProcessId);
            if (process.HasExited)
            {
                return true;
            }

            var executablePath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            var currentIdentity = new ClientProcessSnapshot(
                process.Id,
                process.StartTime.ToUniversalTime(),
                Path.GetFullPath(executablePath));
            if (!Matches(currentIdentity, expectedProcess))
            {
                return false;
            }

            process.Kill(entireProcessTree: false);
            process.WaitForExit(5000);
            return process.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    public static bool Matches(ClientProcessSnapshot actual, ClientProcessSnapshot expected) =>
        actual.ProcessId == expected.ProcessId &&
        actual.StartedAtUtc == expected.StartedAtUtc &&
        string.Equals(
            Path.GetFullPath(actual.ExecutablePath),
            Path.GetFullPath(expected.ExecutablePath),
            StringComparison.OrdinalIgnoreCase);
}