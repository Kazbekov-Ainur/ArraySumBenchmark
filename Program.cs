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
        Console.WriteLine("| Размер массива | Последовательный | Параллельный (Thread) | PLINQ      |");
        Console.WriteLine("|----------------|------------------|------------------------|------------|");

        foreach (int size in sizes)
        {
            int[] array = GenerateRandomArray(size);

            WarmUpMethods(array);

            if (!ValidateResults(array))
            {
                Console.WriteLine("Остановка из-за ошибки валидации");
                return;
            }

            long seqTime = MeasureTime(() => SequentialSum(array), 5);
            long threadTime = MeasureTime(() => ParallelSumWithThreads(array), 5);
            long plinqTime = MeasureTime(() => ParallelSumWithPLINQ(array), 5);

            Console.WriteLine($"| {size,14:N0} | {seqTime,16} | {threadTime,22} | {plinqTime,10} |");
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
        long[] partialSums = new long[threadCount];
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            int threadIndex = i;
            threads[i] = new Thread(() =>
            {
                long localSum = 0;
                int start = threadIndex * chunkSize;
                int end = (threadIndex == threadCount - 1)
                    ? array.Length
                    : start + chunkSize;

                for (int j = start; j < end; j++)
                {
                    localSum += array[j];
                }
                partialSums[threadIndex] = localSum;
            });
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        return partialSums.Sum();
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

    static long MeasureTime(Func<long> action, int iterations)
    {
        // Прогрев
        action();

        Stopwatch sw = new Stopwatch();
        long minTime = long.MaxValue;

        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            long result = action();
            sw.Stop();

            // Защита от оптимизации
            if (result == 0) throw new InvalidOperationException();

            minTime = Math.Min(minTime, sw.ElapsedMilliseconds);
        }

        return minTime;
    }

    static void PrintSystemInfo()
    {
        Console.WriteLine($"ОС: {Environment.OSVersion}");
        Console.WriteLine($"Процессор: {Environment.ProcessorCount} ядер");
        Console.WriteLine($"Версия .NET: {Environment.Version}");
        Console.WriteLine($"Режим: {(Environment.Is64BitProcess ? "x64" : "x86")}");
    }

    static void WarmUpMethods(int[] array)
    {
        SequentialSum(array);
        ParallelSumWithThreads(array);
        ParallelSumWithPLINQ(array);
    }

    static bool ValidateResults(int[] array)
    {
        long seq = SequentialSum(array);
        long thread = ParallelSumWithThreads(array);
        long plinq = ParallelSumWithPLINQ(array);

        if (seq != thread || seq != plinq)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nОшибка валидации для размера {array.Length}:");
            Console.WriteLine($"Последовательная: {seq}");
            Console.WriteLine($"Потоки: {thread}");
            Console.WriteLine($"PLINQ: {plinq}");
            Console.ResetColor();
            return false;
        }
        return true;
    }
}