using System.Collections.Concurrent;
using System.Threading;

namespace NMemoryCache.ConcurrentTags
{
    internal class TagEntry : ConcurrentDictionary<CacheEntry, byte>
    {
        private bool _isRemoved;
        
        /// <summary>
        /// Create already not empty <see cref="TagEntry"/>.
        /// </summary>
        public TagEntry(CacheEntry cacheEntry)
        {
            _isRemoved = false;
            
            TryAdd(cacheEntry, 0);
        }
        
        public bool IsRemoved => Volatile.Read(ref _isRemoved);

        public void MarkAsRemoved()
        {
            Volatile.Write(ref _isRemoved, true);
        }

        public bool IsActive => !IsRemoved;
    }
}
