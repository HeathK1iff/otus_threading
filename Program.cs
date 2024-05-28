using System.Runtime.InteropServices.Marshalling;
using Application.Utils;
using ConsoleTables;

namespace Application;

public class Program
{
    public static void Main(string[] args)
    {
        var program = new Program();
        program.Run();
    }

    public void Run()
    {
        long[,] results = new long[3, 4];

        (results[0, 0], results[0, 1], results[0, 2], results[0, 3]) = BenchSum(Enumerable.Range(1, 100_000).ToArray());
        (results[1, 0], results[1, 1], results[1, 2], results[1, 3]) = BenchSum(Enumerable.Range(1, 1_000_000).ToArray());
        (results[2, 0], results[2, 1], results[2, 2], results[2, 3]) = BenchSum(Enumerable.Range(1, 10_000_000).ToArray());

        var table = new ConsoleTable("int[]", "Simple", "Separated Thread", "AsParallel()", "Parallel.For");
        table.AddRow("100_000", $"{results[0, 0]} ms", $"{results[0, 1]} ms", $"{results[0, 2]} ms", $"{results[0, 3]} ms");
        table.AddRow("1_000_000", $"{results[1, 0]} ms", $"{results[1, 1]} ms", $"{results[1, 2]} ms", $"{results[1, 3]} ms");
        table.AddRow("10_000_000", $"{results[2, 0]} ms", $"{results[2, 1]} ms", $"{results[2, 2]} ms", $"{results[2, 3]} ms");

        table.Write();
    }

    public (long ElapsedMs1, long ElapsedMs2, long ElapsedMs3, long ElapsedMs4) BenchSum(int[] array)
    {
        long sum = Benchmark.Bench(array, CalculateSum_Simple, out var elapsedTime);
        long sum2 = Benchmark.Bench(array, CalculateSum_Thread, out var elapsedTime2);
        long sum3 = Benchmark.Bench(array, CalculateSum_Parallel, out var elapsedTime3);
        long sum4 = Benchmark.Bench(array, CalculateSum_ParallelFor, out var elapsedTime4);

        if ((sum != sum2) || (sum != sum3) || (sum != sum4))
        {
            throw new ApplicationException("Something wrong");
        }

        return (elapsedTime, elapsedTime2, elapsedTime3, elapsedTime4);
    }

    public long CalculateSum_Simple(int[] array)
    {
        return array.Select(f => Convert.ToInt64(f)).Aggregate((a, b) => a + b);
    }

    public long CalculateSum_ParallelFor(int[] array)
    {
        long sum = 0;

        Parallel.For<long>(0, array.Length, () => 0,
        (i, loopState, localVar) =>
        {
            localVar = localVar + Convert.ToInt64(array[i]);
            return localVar;
        }, localVal =>
        {
            Interlocked.Add(ref sum, localVal);
        });

        return sum;
    }

    public long CalculateSum_Thread(int[] array)
    {
        long totalSum = 0;
        const int Chunks = 8;

        int chunkSize = array.Length / Chunks;
        int finishedThreadCount = 0;

        int chunkFirsIndex = 0;
        int chunkLastIndex = chunkSize;

        ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        while (chunkLastIndex <= array.Length)
        {
            new Thread((obj) =>
            {
                long chunkSum = 0;
                (int chunkFirsIndex, int chunkLastIndex) = obj as Tuple<int, int>;

                Span<int> span = array.AsSpan(new Range(chunkFirsIndex, chunkLastIndex));
                foreach (int value in span)
                {
                    chunkSum += value;
                }

                Interlocked.Add(ref totalSum, chunkSum);

                Interlocked.Increment(ref finishedThreadCount);
                if (finishedThreadCount >= Chunks)
                {
                    manualResetEvent.Set();
                }
            }).Start(new Tuple<int, int>(chunkFirsIndex, chunkLastIndex));

            chunkFirsIndex += chunkSize;
            chunkLastIndex = chunkFirsIndex + chunkSize;
        }

        manualResetEvent.WaitOne();

        return totalSum;
    }

    public long CalculateSum_Parallel(int[] array)
    {
        return array.AsParallel().Select(f => Convert.ToInt64(f)).Aggregate((a, b) => a + b);
    }

}
