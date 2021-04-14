using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteamsLab4
{
    class Program
    {
        public static object savedStreamsLock = new object();
        public static ConcurrentDictionary<int, double> SavedStreams { get; set; } = new ConcurrentDictionary<int, double>();
        public static int StreamCount { get; set; } = 100;
        public static double C { get; set; } = 0.0001;
        public static List<IAsyncEnumerable<bool>> Streams = new List<IAsyncEnumerable<bool>>(Enumerable.Repeat(GetStream(), StreamCount));
        public static IAsyncEnumerable<bool> stream = GetStream();
        static async Task Main(string[] args)
        {
            try
            {
                if (!ThreadPool.SetMaxThreads(300, 300))
                {
                    Console.WriteLine("Not able to set max thread count");
                    return;
                }
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(1000);
                        var mostPopularStream = SavedStreams.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                        if (mostPopularStream.Value == 0)
                        {
                            Console.WriteLine("There is no popular streams right now");
                        }
                        else
                        {
                            Console.WriteLine($"Stream {mostPopularStream.Key} is most popular with sum {mostPopularStream.Value}");
                        }
                    }
                });

                var readStreamTasks = new List<Task>();
                for (int i = 0; i < Streams.Count(); i++)
                {
                    int temp = i;
                    readStreamTasks.Add(Task.Run(async () =>
                    {
                        await ReadAsyncStream(Streams[temp], temp);
                    }));

                }
                await Task.WhenAll(readStreamTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        private static async Task ReadAsyncStream(IAsyncEnumerable<bool> stream, int streamIndex)
        {
            try
            {
                await foreach (var input in stream)
                {
                    var value = Convert.ToInt32(input);
                    //Console.WriteLine($"We got {value} for {streamIndex}");
                    var isStreamSaved = SavedStreams.TryGetValue(streamIndex, out _);
                    if (isStreamSaved)
                    {
                        SavedStreams.AddOrUpdate(streamIndex, -1, (index, oldValue) => oldValue * (1 - C) + value);
                        SavedStreams.TryGetValue(streamIndex, out double updatedSum);
                        if (updatedSum < 0.5)
                        {
                            SavedStreams.TryRemove(streamIndex, out _);
                        }
                    }
                    else if (value == 1)
                    {
                        SavedStreams.AddOrUpdate(streamIndex, 1, (index, oldValue) => 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{streamIndex}, {ex.Message}");
            }
        }

        public static async IAsyncEnumerable<bool> GetStream()
        {
            var random = new Random();
            while (true)
            {
                await Task.Delay(100);
                yield return random.NextDouble() <= 0.05;
            }
        }
    }
}
