namespace RobloxAccountManager.Core.Exceptions;

public sealed class ClientSessionConflictException : InvalidOperationException
{
    public ClientSessionConflictException()
        : base("A managed Roblox Player session is already starting or running.")
    {
    }
}