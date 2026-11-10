namespace ZtarVega.Services
{
    /// <summary>The result of requesting one image from a website.</summary>
    internal sealed class ImageFetchResult
    {
        public ImageFetchResult(ImageFetchStatus status, string url, byte[] content, string detail)
        {
            Status = status;
            Url = url ?? string.Empty;
            Content = content;
            Detail = detail;
        }

        public ImageFetchStatus Status { get; }

        /// <summary>The address that was requested.</summary>
        public string Url { get; }

        /// <summary>The downloaded image. Only present when the status is <see cref="ImageFetchStatus.Success"/>.</summary>
        public byte[] Content { get; }

        /// <summary>A technical detail about a failure, or null.</summary>
        public string Detail { get; }
    }
}