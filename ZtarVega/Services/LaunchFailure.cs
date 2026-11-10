namespace ZtarVega.Services
{
    /// <summary>The reason why a game could not be launched.</summary>
    internal enum LaunchFailure
    {
        None,
        EmulatorNotConfigured,
        RomsFolderNotConfigured,
        RomNotFound,
        StartFailed
    }
}