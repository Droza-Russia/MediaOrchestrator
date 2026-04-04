using System;
using System.Collections.Generic;
using System.Threading;

namespace MediaOrchestrator.Analytics.Stores
{
    /// <summary>
    /// Thread-safe LRU (Least Recently Used) cache with TTL (Time To Live) support.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    internal sealed class LruCache<TKey, TValue>
    {
        private sealed class CacheNode
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public DateTimeOffset ExpiresAtUtc { get; set; }
            public CacheNode Prev { get; set; }
            public CacheNode Next { get; set; }

            public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
        }

        private readonly int _capacity;
        private readonly TimeSpan? _defaultTtl;
        private readonly Dictionary<TKey, CacheNode> _map;
        private CacheNode _head; // Most recently used
        private CacheNode _tail; // Least recently used
        private readonly ReaderWriterLockSlim _lock;

        public LruCache(int capacity, TimeSpan? defaultTtl = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            _capacity = capacity;
            _defaultTtl = defaultTtl;
            _map = new Dictionary<TKey, CacheNode>(capacity);
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        public bool TryGet(TKey key, out TValue value)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_map.TryGetValue(key, out var node))
                {
                    if (node.IsExpired)
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            if (_map.TryGetValue(key, out var expiredNode) && expiredNode.IsExpired)
                            {
                                RemoveNode(expiredNode);
                                _map.Remove(key);
                            }
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                        value = default;
                        return false;
                    }

                    _lock.EnterWriteLock();
                    try
                    {
                        if (_map.TryGetValue(key, out var validNode) && !validNode.IsExpired)
                        {
                            MoveToHead(validNode);
                            value = validNode.Value;
                            return true;
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                    value = default;
                    return false;
                }

                value = default;
                return false;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void Put(TKey key, TValue value, TimeSpan? ttl = null)
        {
            _lock.EnterWriteLock();
            try
            {
                // Calculate expiration time
                var expiresAt = ttl.HasValue ?
                    DateTimeOffset.UtcNow.Add(ttl.Value) :
                    (_defaultTtl.HasValue ?
                        DateTimeOffset.UtcNow.Add(_defaultTtl.Value) :
                        DateTimeOffset.MaxValue);

                if (_map.TryGetValue(key, out var existingNode))
                {
                    // Update existing node
                    existingNode.Value = value;
                    existingNode.ExpiresAtUtc = expiresAt;
                    MoveToHead(existingNode);
                }
                else
                {
                    // Add new node
                    var newNode = new CacheNode
                    {
                        Key = key,
                        Value = value,
                        ExpiresAtUtc = expiresAt
                    };

                    _map[key] = newNode;
                    AddToHead(newNode);

                    // Remove least recently used if over capacity
                    if (_map.Count > _capacity)
                    {
                        RemoveTail();
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Remove(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_map.TryGetValue(key, out var node))
                {
                    RemoveNode(node);
                    _map.Remove(key);
                    return true;
                }
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _map.Clear();
                _head = null;
                _tail = null;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _map.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetAll()
        {
            _lock.EnterReadLock();
            try
            {
                var result = new List<KeyValuePair<TKey, TValue>>(_map.Count);
                var current = _head;
                while (current != null)
                {
                    if (!current.IsExpired)
                    {
                        result.Add(new KeyValuePair<TKey, TValue>(current.Key, current.Value));
                    }
                    current = current.Next;
                }
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void ForEach(Action<TKey, TValue> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            _lock.EnterReadLock();
            try
            {
                var current = _head;
                while (current != null)
                {
                    if (!current.IsExpired)
                    {
                        action(current.Key, current.Value);
                    }
                    current = current.Next;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void AddToHead(CacheNode node)
        {
            node.Next = _head;
            node.Prev = null;

            if (_head != null)
            {
                _head.Prev = node;
            }

            _head = node;

            if (_tail == null)
            {
                _tail = node;
            }
        }

        private void RemoveNode(CacheNode node)
        {
            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }
            else
            {
                // Node is head
                _head = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }
            else
            {
                // Node is tail
                _tail = node.Prev;
            }

            node.Prev = null;
            node.Next = null;
        }

        private void MoveToHead(CacheNode node)
        {
            if (node == _head)
                return;

            RemoveNode(node);
            AddToHead(node);
        }

        private void RemoveTail()
        {
            if (_tail != null)
            {
                var tailKey = _tail.Key;
                RemoveNode(_tail);
                _map.Remove(tailKey);
            }
        }

        // Periodic cleanup of expired entries
        public void ExpireExpiredEntries()
        {
            _lock.EnterWriteLock();
            try
            {
                var current = _head;
                while (current != null)
                {
                    var next = current.Next; // Save next before potentially removing current

                    if (current.IsExpired)
                    {
                        RemoveNode(current);
                        _map.Remove(current.Key);
                    }

                    current = next;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}