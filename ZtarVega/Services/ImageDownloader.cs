using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Downloads the images of the games from a website chosen by the user. The requests are made one by one with a
    /// short pause, and everything that arrives is checked before it is saved.
    /// </summary>
    internal sealed class ImageDownloader
    {
        private const int TimeoutMilliseconds = 20000;
        private const int PauseBetweenRequestsMilliseconds = 120;
        private const int MaxRedirections = 5;
        private const long MaxImageSizeInBytes = 10L * 1024 * 1024;
        private const int CopyBufferSize = 16 * 1024;

        // After this many connection failures in a row, the download stops instead of insisting.
        private const int MaxConsecutiveConnectionFailures = 5;

        private readonly AssetLocator _locator;
        private readonly ImageImporter _imageImporter;
        private readonly string _temporaryFolder;
        private readonly string _reportPath;
        private readonly string _userAgent;

        static ImageDownloader()
        {
            // Most websites no longer accept the older security protocols, and older versions of Windows do not
            // use TLS 1.2 unless the application asks for it.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public ImageDownloader(AssetLocator locator, ImageImporter imageImporter, string temporaryFolder, string reportPath)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _imageImporter = imageImporter ?? throw new ArgumentNullException(nameof(imageImporter));
            _temporaryFolder = temporaryFolder ?? throw new ArgumentNullException(nameof(temporaryFolder));
            _reportPath = reportPath ?? throw new ArgumentNullException(nameof(reportPath));

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            _userAgent = "ZtarVega/" + version.ToString(3);
        }

        /// <summary>
        /// Downloads the image of every game on a background thread. When cancelled, the images already saved are
        /// kept and the result says so.
        /// </summary>
        public Task<ImageDownloadResult> DownloadAsync(
            IReadOnlyList<Game> games,
            ImageTarget target,
            string urlTemplate,
            bool replaceExisting,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Download(games, target, urlTemplate, replaceExisting, progress, cancellationToken));
        }

        /// <summary>Requests the image of a single game without saving it, to check that the address is right.</summary>
        public Task<ImageFetchResult> TestAsync(string urlTemplate, Game game, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                string url = ImageUrlTemplate.Build(urlTemplate, game);

                return url == null
                    ? new ImageFetchResult(ImageFetchStatus.NoChecksum, string.Empty, null, null)
                    : Fetch(url, cancellationToken);
            });
        }

        private ImageDownloadResult Download(
            IReadOnlyList<Game> games,
            ImageTarget target,
            string urlTemplate,
            bool replaceExisting,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            if (games == null)
            {
                throw new ArgumentNullException(nameof(games));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (ImageUrlTemplate.Validate(urlTemplate) != TemplateProblem.None)
            {
                throw new ArgumentException("The address template is not valid.", nameof(urlTemplate));
            }

            Directory.CreateDirectory(_temporaryFolder);

            var log = new DownloadLog();
            int downloaded = 0;
            int alreadyHad = 0;
            int noChecksum = 0;
            int consecutiveConnectionFailures = 0;
            bool wasCancelled = false;
            bool wasStopped = false;

            for (int index = 0; index < games.Count; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                Game game = games[index];
                progress?.Report(new ScanProgress(index, games.Count, game.DisplayTitle));

                if (!replaceExisting && _locator.FindImage(target.FolderName, game, target.ScreenshotNumber) != null)
                {
                    alreadyHad++;
                    continue;
                }

                string url = ImageUrlTemplate.Build(urlTemplate, game);

                if (url == null)
                {
                    noChecksum++;
                    continue;
                }

                ImageFetchResult fetched = Fetch(url, cancellationToken);

                switch (fetched.Status)
                {
                    case ImageFetchStatus.Success:
                        consecutiveConnectionFailures = 0;

                        if (TrySave(fetched, game, target, log))
                        {
                            downloaded++;
                        }

                        break;

                    case ImageFetchStatus.NotFound:
                        consecutiveConnectionFailures = 0;
                        log.NotFound.Add(fetched.Url);
                        break;

                    case ImageFetchStatus.NotAnImage:
                        consecutiveConnectionFailures = 0;
                        log.NotAnImage.Add(fetched.Url);
                        break;

                    case ImageFetchStatus.Cancelled:
                        wasCancelled = true;
                        break;

                    case ImageFetchStatus.NetworkError:
                        log.Failures.Add(fetched.Url + "  :  " + fetched.Detail);
                        consecutiveConnectionFailures++;
                        wasStopped = consecutiveConnectionFailures >= MaxConsecutiveConnectionFailures;
                        break;

                    default:
                        // The image is too large, or the address could not be built into a valid one.
                        consecutiveConnectionFailures = 0;
                        log.Failures.Add(fetched.Url + "  :  " + fetched.Status);
                        break;
                }

                if (wasCancelled || wasStopped)
                {
                    break;
                }

                // A short pause between requests, so the website is not flooded. It ends early when cancelled.
                if (cancellationToken.WaitHandle.WaitOne(PauseBetweenRequestsMilliseconds))
                {
                    wasCancelled = true;
                    break;
                }
            }

            string reportPath = WriteReport(
                target, urlTemplate, replaceExisting, games.Count, downloaded, alreadyHad, noChecksum, wasCancelled, wasStopped, log);

            return new ImageDownloadResult(
                games.Count,
                downloaded,
                alreadyHad,
                log.NotFound.Count,
                log.NotAnImage.Count,
                noChecksum,
                log.Failures.Count,
                wasCancelled,
                wasStopped,
                log.NotFound.Count > 0 ? log.NotFound[0] : null,
                reportPath);
        }

        /// <summary>Saves a downloaded image as the image of the game, reduced to the size used for that kind of image.</summary>
        private bool TrySave(ImageFetchResult fetched, Game game, ImageTarget target, DownloadLog log)
        {
            string temporaryPath = Path.Combine(_temporaryFolder, Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                File.WriteAllBytes(temporaryPath, fetched.Content);

                _imageImporter.Import(
                    temporaryPath,
                    target.FolderName,
                    game,
                    target.ScreenshotNumber,
                    target.MaxSize.Width,
                    target.MaxSize.Height);

                return true;
            }
            catch (InvalidDataException exception)
            {
                log.Failures.Add(fetched.Url + "  :  " + exception.Message);
                return false;
            }
            catch (IOException exception)
            {
                log.Failures.Add(fetched.Url + "  :  " + exception.Message);
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                log.Failures.Add(fetched.Url + "  :  " + exception.Message);
                return false;
            }
            catch (ExternalException exception)
            {
                // GDI+ reports its failures with this exception.
                log.Failures.Add(fetched.Url + "  :  " + exception.Message);
                return false;
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    // A temporary file that cannot be deleted is harmless.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private ImageFetchResult Fetch(string url, CancellationToken cancellationToken)
        {
            Uri uri;

            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return new ImageFetchResult(ImageFetchStatus.InvalidUrl, url, null, null);
            }

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.UserAgent = _userAgent;
            request.Timeout = TimeoutMilliseconds;
            request.ReadWriteTimeout = TimeoutMilliseconds;
            request.MaximumAutomaticRedirections = MaxRedirections;

            // Cancelling aborts the request that is in flight.
            using (cancellationToken.Register(request.Abort))
            {
                try
                {
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (response.ContentLength > MaxImageSizeInBytes)
                        {
                            return new ImageFetchResult(ImageFetchStatus.TooLarge, url, null, null);
                        }

                        byte[] content = ReadLimited(stream);

                        if (content == null)
                        {
                            return new ImageFetchResult(ImageFetchStatus.TooLarge, url, null, null);
                        }

                        return IsValidImage(content)
                            ? new ImageFetchResult(ImageFetchStatus.Success, url, content, null)
                            : new ImageFetchResult(ImageFetchStatus.NotAnImage, url, null, null);
                    }
                }
                catch (WebException exception)
                {
                    return cancellationToken.IsCancellationRequested
                        ? new ImageFetchResult(ImageFetchStatus.Cancelled, url, null, null)
                        : ClassifyWebException(url, exception);
                }
                catch (IOException exception)
                {
                    return cancellationToken.IsCancellationRequested
                        ? new ImageFetchResult(ImageFetchStatus.Cancelled, url, null, null)
                        : new ImageFetchResult(ImageFetchStatus.NetworkError, url, null, exception.Message);
                }
                catch (ObjectDisposedException)
                {
                    // Aborting a request can also close the stream that was being read.
                    return new ImageFetchResult(ImageFetchStatus.Cancelled, url, null, null);
                }
            }
        }

        private static ImageFetchResult ClassifyWebException(string url, WebException exception)
        {
            var errorResponse = exception.Response as HttpWebResponse;

            if (errorResponse == null)
            {
                return new ImageFetchResult(ImageFetchStatus.NetworkError, url, null, exception.Message);
            }

            int statusCode = (int)errorResponse.StatusCode;
            errorResponse.Dispose();

            // Some websites answer 403 for a file that does not exist.
            if (statusCode == 403 || statusCode == 404 || statusCode == 410)
            {
                return new ImageFetchResult(ImageFetchStatus.NotFound, url, null, "HTTP " + statusCode);
            }

            return new ImageFetchResult(ImageFetchStatus.NetworkError, url, null, "HTTP " + statusCode);
        }

        /// <summary>Reads the whole response, or returns null when it is larger than an image can be.</summary>
        private static byte[] ReadLimited(Stream stream)
        {
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[CopyBufferSize];
                int read;

                while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    if (buffer.Length + read > MaxImageSizeInBytes)
                    {
                        return null;
                    }

                    buffer.Write(chunk, 0, read);
                }

                return buffer.ToArray();
            }
        }

        /// <summary>A website can answer "200 OK" with an error page, so the content must really be an image.</summary>
        private static bool IsValidImage(byte[] content)
        {
            try
            {
                using (var stream = new MemoryStream(content))
                using (Image.FromStream(stream, false, true))
                {
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (OutOfMemoryException)
            {
                // GDI+ reports a file that is not a valid image with this exception.
                return false;
            }
        }

        /// <summary>Writes a text report with every request that did not end in an image. Returns its path, or null if it could not be written.</summary>
        private string WriteReport(
            ImageTarget target,
            string urlTemplate,
            bool replaceExisting,
            int gamesInSelection,
            int downloaded,
            int alreadyHad,
            int noChecksum,
            bool wasCancelled,
            bool wasStopped,
            DownloadLog log)
        {
            var builder = new StringBuilder();
            builder.AppendLine("ZtarVega image download report");
            builder.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("Image type: " + target.DisplayName);
            builder.AppendLine("Address: " + urlTemplate);
            builder.AppendLine("Mode: " + (replaceExisting ? "replace existing images" : "fill only the games without an image"));
            builder.AppendLine(wasCancelled
                ? "The download was cancelled before it finished."
                : wasStopped ? "The download stopped after several connection failures in a row." : "The download finished.");
            builder.AppendLine();
            builder.AppendLine("Games in the selection: " + gamesInSelection);
            builder.AppendLine("Images downloaded: " + downloaded);
            builder.AppendLine("Games that already had the image: " + alreadyHad);
            builder.AppendLine("Games without a checksum (skipped): " + noChecksum);
            builder.AppendLine();

            AppendSection(builder, "NOT FOUND (the website says they do not exist)", log.NotFound);
            AppendSection(builder, "NOT AN IMAGE (the website answered with something else)", log.NotAnImage);
            AppendSection(builder, "FAILED", log.Failures);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_reportPath));
                File.WriteAllText(_reportPath, builder.ToString(), Encoding.UTF8);
                return _reportPath;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static void AppendSection(StringBuilder builder, string title, List<string> lines)
        {
            if (lines.Count == 0)
            {
                return;
            }

            builder.AppendLine("== " + title + " (" + lines.Count + ") ==");

            foreach (string line in lines)
            {
                builder.AppendLine(line);
            }

            builder.AppendLine();
        }

        /// <summary>The requests that did not end in a saved image, grouped by the reason.</summary>
        private sealed class DownloadLog
        {
            public List<string> NotFound { get; } = new List<string>();

            public List<string> NotAnImage { get; } = new List<string>();

            public List<string> Failures { get; } = new List<string>();
        }
    }
}