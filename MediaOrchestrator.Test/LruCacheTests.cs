using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Stores;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class LruCacheTests : IDisposable
    {
        public void Dispose()
        {
            // Clean up if needed
        }

        [Fact]
        public void LruCache_GetAndPut_BasicFunctionality()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 3);

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");

            // Assert
            Assert.True(cache.TryGet(1, out var value1));
            Assert.Equal("one", value1);

            Assert.True(cache.TryGet(2, out var value2));
            Assert.Equal("two", value2);

            Assert.False(cache.TryGet(3, out _));
        }

        [Fact]
        public void LruCache_LruEviction_WhenOverCapacity()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 2);

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three"); // This should evict key 1

            // Assert
            Assert.False(cache.TryGet(1, out _)); // Should be evicted
            Assert.True(cache.TryGet(2, out var value2));
            Assert.Equal("two", value2);
            Assert.True(cache.TryGet(3, out var value3));
            Assert.Equal("three", value3);
        }

        [Fact]
        public void LruCache_AccessOrder_MostRecentlyUsedStays()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 2);

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");

            // Access key 1 to make it most recently used
            cache.TryGet(1, out _);

            // Add third item, should evict key 2 (least recently used)
            cache.Put(3, "three");

            // Assert
            Assert.True(cache.TryGet(1, out var value1)); // Should still be here
            Assert.Equal("one", value1);

            Assert.False(cache.TryGet(2, out _)); // Should be evicted
            Assert.True(cache.TryGet(3, out var value3));
            Assert.Equal("three", value3);
        }

        [Fact]
        public void LruCache_TtlExpiration_RemovesExpiredEntries()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 10, TimeSpan.FromMilliseconds(100));

            // Act
            cache.Put(1, "one");

            // Wait for expiration
            Thread.Sleep(150);

            // Assert
            Assert.False(cache.TryGet(1, out _)); // Should be expired
        }

        [Fact]
        public void LruCache_UpdateExistingKey_ResetsPositionAndTtl()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 2, TimeSpan.FromMilliseconds(100));

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");

            // Wait a bit so key 1 is closer to expiration
            Thread.Sleep(60);

            // Update key 1 - should reset its position and TTL
            cache.Put(1, "ONE");

            // Add third item - should evict key 2, not key 1
            cache.Put(3, "three");

            // Assert
            Assert.True(cache.TryGet(1, out var value1)); // Should still be here
            Assert.Equal("ONE", value1); // Should have updated value

            Assert.False(cache.TryGet(2, out _)); // Should be evicted
            Assert.True(cache.TryGet(3, out var value3));
            Assert.Equal("three", value3);
        }

        [Fact]
        public void LruCache_Clear_RemovesAllEntries()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 10);

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Clear();

            // Assert
            Assert.False(cache.TryGet(1, out _));
            Assert.False(cache.TryGet(2, out _));
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void LruCache_GetAll_ReturnsNonExpiredEntriesInOrder()
        {
            // Arrange
            var cache = new LruCache<int, string>(capacity: 10, TimeSpan.FromMilliseconds(200));

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");

            Thread.Sleep(100); // Let first entry age

            cache.Put(3, "three");

            // Assert
            var allEntries = cache.GetAll().ToList();
            Assert.Equal(3, allEntries.Count);

            // Should be in LRU order: most recently used first
            Assert.Equal(3, allEntries[0].Key); // three (most recent)
            Assert.Equal(2, allEntries[1].Key); // two
            Assert.Equal(1, allEntries[2].Key); // one (least recent but not expired yet)
        }

        [Fact]
        public void LruCache_ThreadSafe_ConcurrentAccess()
        {
            // Arrange
            var cache = new LruCache<int, int>(capacity: 1100);
            const int threadCount = 10;
            const int iterationsPerThread = 100;

            // Act & Assert
            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadNum = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        int key = threadNum * iterationsPerThread + i;
                        cache.Put(key, key);

                        // Randomly read some values
                        if (i % 10 == 0)
                        {
                            cache.TryGet(key, out _);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Verify we can still read values
            var lastKey = (threadCount * iterationsPerThread) - 1;
            Assert.True(cache.TryGet(lastKey, out var lastValue));
            Assert.Equal(lastKey, lastValue);
        }
    }
}