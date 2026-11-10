namespace ZtarVega.Services
{
    /// <summary>The outcome of requesting one image from a website.</summary>
    internal enum ImageFetchStatus
    {
        Success,
        NotFound,
        NotAnImage,
        TooLarge,
        NetworkError,
        InvalidUrl,
        NoChecksum,
        Cancelled
    }
}