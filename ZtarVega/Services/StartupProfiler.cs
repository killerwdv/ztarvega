using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace ZtarVega.Services
{
    /// <summary>
    /// Measures how long the application takes to start and where the time goes. It only works when the
    /// application is started with the "--profile" argument; otherwise every call does nothing.
    /// </summary>
    internal static class StartupProfiler
    {
        private static readonly List<KeyValuePair<string, long>> Milestones = new List<KeyValuePair<string, long>>();
        private static readonly List<KeyValuePair<string, long>> Measurements = new List<KeyValuePair<string, long>>();

        private static Stopwatch _clock;
        private static string _reportPath;
        private static TimeSpan _timeBeforeMain;
        private static bool _isReportWritten;

        public static bool IsEnabled
        {
            get { return _clock != null; }
        }

        /// <summary>Starts measuring. Call it as early as possible in Main.</summary>
        public static void Enable(string reportPath)
        {
            _reportPath = reportPath;
            _clock = Stopwatch.StartNew();

            // The time from the creation of the process until Main starts: it covers loading the .NET runtime.
            try
            {
                _timeBeforeMain = DateTime.Now - Process.GetCurrentProcess().StartTime;
            }
            catch (InvalidOperationException)
            {
                _timeBeforeMain = TimeSpan.Zero;
            }
            catch (NotSupportedException)
            {
                _timeBeforeMain = TimeSpan.Zero;
            }
            catch (Win32Exception)
            {
                _timeBeforeMain = TimeSpan.Zero;
            }
        }

        /// <summary>Records that a point of the startup was reached.</summary>
        public static void Mark(string name)
        {
            if (_clock != null)
            {
                Milestones.Add(new KeyValuePair<string, long>(name, _clock.ElapsedMilliseconds));
            }
        }

        /// <summary>Runs an action and records how long it took.</summary>
        public static void Measure(string name, Action action)
        {
            if (_clock == null)
            {
                action();
                return;
            }

            long start = _clock.ElapsedMilliseconds;
            action();
            Measurements.Add(new KeyValuePair<string, long>(name, _clock.ElapsedMilliseconds - start));
        }

        /// <summary>Writes the report. It is written once, and a failure to write it is ignored.</summary>
        public static void WriteReport()
        {
            if (_clock == null || _isReportWritten)
            {
                return;
            }

            _isReportWritten = true;

            var builder = new StringBuilder();
            builder.AppendLine("ZtarVega startup report");
            builder.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("Windows (approximate): " + Environment.OSVersion.VersionString);
            builder.AppendLine(".NET runtime: " + Environment.Version);
            builder.AppendLine("64-bit process: " + Environment.Is64BitProcess);
            builder.AppendLine("Processors: " + Environment.ProcessorCount);
#if DEBUG
            builder.AppendLine("Build: Debug (a Release build is faster)");
#else
            builder.AppendLine("Build: Release");
#endif
            builder.AppendLine();
            builder.AppendLine("Time before Main (loading the .NET runtime): " + (long)_timeBeforeMain.TotalMilliseconds + " ms");
            builder.AppendLine();
            builder.AppendLine("Milestones (milliseconds since Main started)");

            foreach (KeyValuePair<string, long> milestone in Milestones)
            {
                builder.AppendLine("  " + milestone.Value.ToString(CultureInfo.InvariantCulture).PadLeft(6) + "  " + milestone.Key);
            }

            if (Measurements.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Measurements (how long each operation took)");

                foreach (KeyValuePair<string, long> measurement in Measurements)
                {
                    builder.AppendLine("  " + measurement.Value.ToString(CultureInfo.InvariantCulture).PadLeft(6) + " ms  " + measurement.Key);
                }
            }

            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                builder.AppendLine();
                builder.AppendLine("Memory when the window appeared");
                builder.AppendLine("  Working set:   " + (process.WorkingSet64 / (1024 * 1024)) + " MB");
                builder.AppendLine("  Private bytes: " + (process.PrivateMemorySize64 / (1024 * 1024)) + " MB");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_reportPath));
                File.WriteAllText(_reportPath, builder.ToString(), Encoding.UTF8);
            }
            catch (IOException)
            {
                // The report is a diagnostic tool: failing to write it must never affect the application.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}