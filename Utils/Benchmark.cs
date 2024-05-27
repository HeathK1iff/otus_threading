using System.Diagnostics;

namespace Application.Utils;

public static class Benchmark
{
    public static long Bench(int[] array, Func<int[], long> action, out long elapsedMs)
    {
        elapsedMs = 0;
        
        if (action == null)
        {
            throw new ArgumentNullException($"Action is required");
        }

        var sw = new Stopwatch();
        sw.Start();
        try
        {
            return action.Invoke(array);
        } 
        finally
        {
            sw.Stop();
            elapsedMs = sw.ElapsedMilliseconds; 
        }
    }
}