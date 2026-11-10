using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ZtarVega.Services
{
    /// <summary>Appends diagnostic messages to a log file. Logging itself never throws.</summary>
    internal sealed class ErrorLogger
    {
        private const long MaxFileSizeInBytes = 512 * 1024;
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

        private readonly object _syncRoot = new object();

        public ErrorLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("The log file path cannot be empty.", nameof(filePath));
            }

            FilePath = filePath;
        }

        public string FilePath { get; }

        /// <summary>Writes a message and, when given, the full details of an exception.</summary>
        public void Log(string message, Exception exception)
        {
            try
            {
                lock (_syncRoot)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    RotateIfTooBig();

                    var builder = new StringBuilder();
                    builder.Append(DateTime.Now.ToString(TimestampFormat, CultureInfo.InvariantCulture));
                    builder.Append(" | ");
                    builder.AppendLine(message);

                    if (exception != null)
                    {
                        builder.AppendLine(exception.ToString());
                    }

                    builder.AppendLine();
                    File.AppendAllText(FilePath, builder.ToString(), Encoding.UTF8);
                }
            }
            catch (IOException)
            {
                // A failure while logging must never cause another failure.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void RotateIfTooBig()
        {
            var file = new FileInfo(FilePath);

            if (!file.Exists || file.Length <= MaxFileSizeInBytes)
            {
                return;
            }

            string previousPath = FilePath + ".old";

            if (File.Exists(previousPath))
            {
                File.Delete(previousPath);
            }

            File.Move(FilePath, previousPath);
        }
    }
}