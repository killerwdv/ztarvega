using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ZtarVega.Services
{
    /// <summary>
    /// Loads images on demand, scales them down to the requested size and keeps the most recently
    /// used ones in memory, up to a byte budget. The cache owns the images and disposes them when
    /// they are evicted, so callers must never dispose them. The budget must comfortably exceed
    /// the total size of the images that are on screen at the same time.
    /// Not thread-safe: use it from the UI thread only.
    /// </summary>
    internal sealed class ImageCache : IDisposable
    {
        private const int BytesPerPixel = 4;

        private readonly long _maxBytes;
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _entries =
            new Dictionary<string, LinkedListNode<CacheEntry>>(StringComparer.OrdinalIgnoreCase);

        // The first node is the most recently used one.
        private readonly LinkedList<CacheEntry> _usageOrder = new LinkedList<CacheEntry>();

        private long _currentBytes;
        private bool _isDisposed;

        public ImageCache(long maxBytes)
        {
            if (maxBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBytes));
            }

            _maxBytes = maxBytes;
        }

        /// <summary>
        /// Returns the image at the given path, scaled down to fit inside the given size, or null when
        /// the path is empty or the file cannot be read as an image.
        /// </summary>
        public Bitmap Get(string path, int maxWidth, int maxHeight)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string key = path + "|" + maxWidth + "x" + maxHeight;
            LinkedListNode<CacheEntry> node;

            if (_entries.TryGetValue(key, out node))
            {
                _usageOrder.Remove(node);
                _usageOrder.AddFirst(node);
                return node.Value.Bitmap;
            }

            Bitmap bitmap = TryLoadScaled(path, maxWidth, maxHeight);

            if (bitmap == null)
            {
                return null;
            }

            var entry = new CacheEntry(key, bitmap);
            _entries[key] = _usageOrder.AddFirst(entry);
            _currentBytes += entry.SizeInBytes;
            EvictIfNeeded();

            return bitmap;
        }

        /// <summary>
        /// Forgets every cached version of the image at the given path. The caller must detach the image
        /// from any control first, because the cached bitmaps are disposed.
        /// </summary>
        public void Invalidate(string path)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string prefix = path + "|";
            var keysToRemove = new List<string>();

            foreach (string key in _entries.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                LinkedListNode<CacheEntry> node = _entries[key];
                _entries.Remove(key);
                _usageOrder.Remove(node);
                _currentBytes -= node.Value.SizeInBytes;
                node.Value.Bitmap.Dispose();
            }
        }

        /// <summary>
        /// Disposes and forgets every cached image. The caller must detach the images from any control first.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            foreach (CacheEntry entry in _usageOrder)
            {
                entry.Bitmap.Dispose();
            }

            _usageOrder.Clear();
            _entries.Clear();
            _currentBytes = 0;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (CacheEntry entry in _usageOrder)
            {
                entry.Bitmap.Dispose();
            }

            _usageOrder.Clear();
            _entries.Clear();
            _currentBytes = 0;
            _isDisposed = true;
        }

        internal static Bitmap TryLoadScaled(string path, int maxWidth, int maxHeight)
        {
            try
            {
                // Read through a stream so the file is not kept locked on disk.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (Image source = Image.FromStream(stream, false, true))
                {
                    return CreateScaledCopy(source, maxWidth, maxHeight);
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (OutOfMemoryException)
            {
                // GDI+ reports a file that is not a valid image with this exception.
                return null;
            }
        }

        private static Bitmap CreateScaledCopy(Image source, int maxWidth, int maxHeight)
        {
            // Never enlarge: the scale is capped at 1.
            double scale = Math.Min(1.0, Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));

            // Premultiplied alpha is the pixel format GDI+ draws the fastest.
            var result = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, 0, 0, width, height);
            }

            return result;
        }

        private void EvictIfNeeded()
        {
            // The newest entry is always kept, even if it alone exceeds the budget.
            while (_currentBytes > _maxBytes && _usageOrder.Count > 1)
            {
                LinkedListNode<CacheEntry> leastRecentlyUsed = _usageOrder.Last;
                _usageOrder.RemoveLast();
                _entries.Remove(leastRecentlyUsed.Value.Key);
                _currentBytes -= leastRecentlyUsed.Value.SizeInBytes;
                leastRecentlyUsed.Value.Bitmap.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ImageCache));
            }
        }

        private sealed class CacheEntry
        {
            public CacheEntry(string key, Bitmap bitmap)
            {
                Key = key;
                Bitmap = bitmap;
                SizeInBytes = (long)bitmap.Width * bitmap.Height * BytesPerPixel;
            }

            public string Key { get; }

            public Bitmap Bitmap { get; }

            public long SizeInBytes { get; }
        }
    }
}