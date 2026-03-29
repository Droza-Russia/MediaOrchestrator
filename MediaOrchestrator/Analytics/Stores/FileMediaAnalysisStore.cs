using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MediaOrchestrator.Analytics.Models;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator.Analytics.Stores
{
    internal sealed class FileMediaAnalysisStore : IMediaAnalysisStore, IDisposable
    {
        private readonly string _directoryPath;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly int _bufferSize;
        private readonly bool _enableCompression;
        private volatile bool _isDisposed;

        private static readonly ConcurrentDictionary<string, string> _hashCache = new ConcurrentDictionary<string, string>();
        private const int MaxHashCacheSize = 10000;
        private static readonly object _hashCacheGate = new object();

        public FileMediaAnalysisStore(string directoryPath, bool enableCompression = false)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Store directory path must be provided.", nameof(directoryPath));
            }

            _directoryPath = Path.GetFullPath(directoryPath);
            _bufferSize = 81920;
            _enableCompression = enableCompression;

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<MediaAnalysisRecord> GetAsync(string analysisKey, CancellationToken cancellationToken = default)
        {
            if (_isDisposed || string.IsNullOrWhiteSpace(analysisKey))
            {
                return null;
            }

            string path = GetRecordPath(analysisKey);
            if (!File.Exists(path))
            {
                return null;
            }

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json;
                if (_enableCompression && IsCompressedFile(path))
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, useAsync: true))
                    using (var decompressStream = new GZipStream(stream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(decompressStream, Encoding.UTF8))
                    {
                        json = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, useAsync: true))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        json = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }

                return JsonSerializer.Deserialize<MediaAnalysisRecord>(json, _jsonSerializerOptions);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<IReadOnlyCollection<MediaAnalysisRecord>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed || !Directory.Exists(_directoryPath))
            {
                return Array.Empty<MediaAnalysisRecord>();
            }

            var jsonFiles = Directory.GetFiles(_directoryPath, "*.json", SearchOption.AllDirectories);
            var gzFiles = _enableCompression
                ? Directory.GetFiles(_directoryPath, "*.json.gz", SearchOption.AllDirectories)
                : Array.Empty<string>();

            var files = jsonFiles.Concat(gzFiles).ToArray();
            var result = new List<MediaAnalysisRecord>(files.Length);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var path in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string json;
                    if (_enableCompression && IsCompressedFile(path))
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, useAsync: true))
                        using (var decompressStream = new GZipStream(stream, CompressionMode.Decompress))
                        using (var reader = new StreamReader(decompressStream, Encoding.UTF8))
                        {
                            json = await reader.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, useAsync: true))
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            json = await reader.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }

                    var record = JsonSerializer.Deserialize<MediaAnalysisRecord>(json, _jsonSerializerOptions);
                    if (record != null)
                    {
                        result.Add(record);
                    }
                }
            }
            finally
            {
                _gate.Release();
            }

            return result;
        }

        public async Task SaveAsync(MediaAnalysisRecord record, CancellationToken cancellationToken = default)
        {
            if (_isDisposed || record == null || string.IsNullOrWhiteSpace(record.AnalysisKey))
            {
                return;
            }

            Directory.CreateDirectory(_directoryPath);
            string path = GetRecordPath(record.AnalysisKey);
            string json = JsonSerializer.Serialize(record, _jsonSerializerOptions);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string tempPath = path + ".tmp";

                if (_enableCompression)
                {
                    using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true))
                    using (var compressStream = new GZipStream(stream, CompressionLevel.Optimal))
                    using (var writer = new StreamWriter(compressStream, Encoding.UTF8))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await writer.WriteAsync(json).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await writer.WriteAsync(json).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                    }
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);

                string oldPath = _enableCompression
                    ? Path.ChangeExtension(path, null)
                    : path + ".gz";
                FileHelper.SafeDeleteFile(oldPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                FileHelper.SafeDeleteTempFiles(path);
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Directory.Exists(_directoryPath))
                {
                    Directory.Delete(_directoryPath, recursive: true);
                }

                _hashCache.Clear();
            }
            finally
            {
                _gate.Release();
            }
        }

        private string GetRecordPath(string analysisKey)
        {
            string hash = GetCachedHash(analysisKey);
            string shardLevelOne = hash.Substring(0, 2);
            string shardLevelTwo = hash.Substring(2, 2);
            string directory = Path.Combine(_directoryPath, shardLevelOne, shardLevelTwo);
            Directory.CreateDirectory(directory);
            string extension = _enableCompression ? ".json.gz" : ".json";
            return Path.Combine(directory, hash + extension);
        }

        private static string GetCachedHash(string value)
        {
            EnsureHashCacheSizeLimit();
            return _hashCache.GetOrAdd(value, v => ComputeHashInternal(v));
        }

        private static string GetCachedHash(string value, bool useCompression)
        {
            EnsureHashCacheSizeLimit();
            var key = useCompression ? value + "_compressed" : value;
            return _hashCache.GetOrAdd(key, k => ComputeHashInternal(k.Replace("_compressed", "")));
        }

        private static void EnsureHashCacheSizeLimit()
        {
            if (_hashCache.Count > MaxHashCacheSize)
            {
                lock (_hashCacheGate)
                {
                    if (_hashCache.Count > MaxHashCacheSize)
                    {
                        var keysToRemove = _hashCache.Keys.Take(_hashCache.Count / 2).ToList();
                        foreach (var key in keysToRemove)
                        {
                            _hashCache.TryRemove(key, out _);
                        }
                    }
                }
            }
        }

        private static string ComputeHashInternal(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static bool IsCompressedFile(string path)
        {
            return path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _gate.Dispose();
            _hashCache.Clear();
        }
    }
}