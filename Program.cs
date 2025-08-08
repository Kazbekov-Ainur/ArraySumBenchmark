using System.Diagnostics;

class Program
{
    static void Main()
    {
        Console.WriteLine("Многопоточный сумматор массивов");
        Console.WriteLine("===============================");

        PrintSystemInfo();

        int[] sizes = { 100_000, 1_000_000, 10_000_000 };

        Console.WriteLine("\nРезультаты замеров времени (мс):");
        Console.WriteLine("| Размер массива | Последовательный  | Параллельный (Thread)  | PLINQ      |");
        Console.WriteLine("|----------------|-------------------|------------------------|------------|");

        foreach (int size in sizes)
        {
            int[] array = GenerateRandomArray(size);

            long seqTime = MeasureTime(() => SequentialSum(array));
            long threadTime = MeasureTime(() => ParallelSumWithThreads(array));
            long plinqTime = MeasureTime(() => ParallelSumWithPLINQ(array));

            Console.WriteLine($"| {size,14:N0} | {seqTime,17} | {threadTime,22} | {plinqTime,10} |");

            ValidateResults(array);
        }
    }

    static long SequentialSum(int[] array)
    {
        long sum = 0;
        foreach (int num in array)
        {
            sum += num;
        }
        return sum;
    }

    static long ParallelSumWithThreads(int[] array)
    {
        int threadCount = Environment.ProcessorCount;
        int chunkSize = array.Length / threadCount;

        long total = 0;
        var threads = new List<Thread>();

        for (int i = 0; i < threadCount; i++)
        {
            int start = i * chunkSize;
            int end = (i == threadCount - 1) ? array.Length : start + chunkSize;

            Thread thread = new Thread(() =>
            {
                long localSum = 0;
                for (int j = start; j < end; j++)
                {
                    localSum += array[j];
                }
                Interlocked.Add(ref total, localSum);
            });

            threads.Add(thread);
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        return total;
    }

    static long ParallelSumWithPLINQ(int[] array)
    {
        return array.AsParallel().Sum(x => (long)x);
    }

    static int[] GenerateRandomArray(int size)
    {
        Random rand = new Random();
        int[] array = new int[size];
        for (int i = 0; i < size; i++)
        {
            array[i] = rand.Next(1, 100);
        }
        return array;
    }

    static long MeasureTime(Func<long> action)
    {
        var sw = Stopwatch.StartNew();
        long result = action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    static void PrintSystemInfo()
    {
        Console.WriteLine($"ОС: {Environment.OSVersion}");
        Console.WriteLine($"Процессор: {Environment.ProcessorCount} ядер");
        Console.WriteLine($"Версия .NET: {Environment.Version}");
    }

    static void ValidateResults(int[] array)
    {
        long seq = SequentialSum(array);
        long thread = ParallelSumWithThreads(array);
        long plinq = ParallelSumWithPLINQ(array);

        if (seq != thread || seq != plinq)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nОшибка! Разные суммы для массива {array.Length}:");
            Console.WriteLine($"Последовательная: {seq}");
            Console.WriteLine($"Потоки: {thread}");
            Console.WriteLine($"PLINQ: {plinq}");
            Console.ResetColor();
        }
    }
}