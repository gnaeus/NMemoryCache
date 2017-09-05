using System;

namespace NMemoryCache.Tests.Data
{
    public class Entity
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Content{ get; set; }

        public string[] Tags { get; set; } = new string[0];
    }
}
