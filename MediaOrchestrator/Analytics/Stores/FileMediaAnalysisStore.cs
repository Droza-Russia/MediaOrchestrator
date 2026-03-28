using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator.Analytics.Stores
{
    internal sealed class FileMediaAnalysisStore : IMediaAnalysisStore
    {
        private readonly string _directoryPath;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public FileMediaAnalysisStore(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Store directory path must be provided.", nameof(directoryPath));
            }

            _directoryPath = Path.GetFullPath(directoryPath);
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<MediaAnalysisRecord> GetAsync(string analysisKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(analysisKey))
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
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    json = await reader.ReadToEndAsync().ConfigureAwait(false);
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
            if (!Directory.Exists(_directoryPath))
            {
                return Array.Empty<MediaAnalysisRecord>();
            }

            string[] files = Directory.GetFiles(_directoryPath, "*.json", SearchOption.AllDirectories);
            var result = new List<MediaAnalysisRecord>(files.Length);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var path in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string json;
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        json = await reader.ReadToEndAsync().ConfigureAwait(false);
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
            if (record == null || string.IsNullOrWhiteSpace(record.AnalysisKey))
            {
                return;
            }

            Directory.CreateDirectory(_directoryPath);
            string path = GetRecordPath(record.AnalysisKey);
            string json = JsonSerializer.Serialize(record, _jsonSerializerOptions);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteAsync(json).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
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
            }
            finally
            {
                _gate.Release();
            }
        }

        private string GetRecordPath(string analysisKey)
        {
            string hash = ComputeHash(analysisKey);
            string shardLevelOne = hash.Substring(0, 2);
            string shardLevelTwo = hash.Substring(2, 2);
            string directory = Path.Combine(_directoryPath, shardLevelOne, shardLevelTwo);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, hash + ".json");
        }

        private static string ComputeHash(string value)
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
    }
}
