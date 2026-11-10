using System;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Holds the settings of the running application and saves them to disk.</summary>
    internal sealed class SettingsSession
    {
        private readonly SettingsService _service;

        public SettingsSession(SettingsService service, AppSettings settings)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Current = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The settings in use.</summary>
        public AppSettings Current { get; private set; }

        /// <summary>Uses new settings, for example the ones edited in the settings window.</summary>
        public void Replace(AppSettings settings)
        {
            Current = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Saves the settings. Throws IOException or UnauthorizedAccessException when they cannot be written.</summary>
        public void Save()
        {
            _service.Save(Current);
        }
    }
}