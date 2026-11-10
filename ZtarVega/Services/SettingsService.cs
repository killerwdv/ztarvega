using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Reads and writes the settings file (config.json).</summary>
    internal sealed class SettingsService
    {
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(AppSettings));

        private readonly string _filePath;

        public SettingsService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("The settings file path cannot be empty.", nameof(filePath));
            }

            _filePath = filePath;
        }

        /// <summary>Loads the settings. Returns default settings when the file is missing or unreadable.</summary>
        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    using (FileStream stream = File.OpenRead(_filePath))
                    {
                        var settings = Serializer.ReadObject(stream) as AppSettings;

                        if (settings != null)
                        {
                            settings.EnsureDefaults();
                            return settings;
                        }
                    }
                }
            }
            catch (SerializationException)
            {
                // A corrupt file must not prevent the application from starting.
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return CreateDefaultSettings();
        }

        /// <summary>Saves the settings. The file is written to a temporary file first so a failure cannot corrupt it.</summary>
        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string temporaryPath = _filePath + ".tmp";

            using (FileStream stream = File.Create(temporaryPath))
            using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                Serializer.WriteObject(writer, settings);
                writer.Flush();
            }

            if (File.Exists(_filePath))
            {
                File.Replace(temporaryPath, _filePath, null);
            }
            else
            {
                File.Move(temporaryPath, _filePath);
            }
        }

        private static AppSettings CreateDefaultSettings()
        {
            var settings = new AppSettings();
            settings.EnsureDefaults();
            return settings;
        }
    }
}