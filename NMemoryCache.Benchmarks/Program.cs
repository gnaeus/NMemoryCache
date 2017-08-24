using System;

namespace NMemoryCache.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine($"NMemoryCache.Benchmarks.exe keys:int tags:int tagsPerKey:int lifetimeSec invalidationMsec");
                return;
            }

            int keys = Int32.Parse(args[0]);
            int tags = Int32.Parse(args[1]);
            int tagsPerKey = Int32.Parse(args[2]);
            int lifetimeSec = Int32.Parse(args[3]);
            int invalidationMsec = Int32.Parse(args[4]);

            new CacheBenchmark().Run(keys, tags, tagsPerKey, lifetimeSec, invalidationMsec);
        }
    }
}
