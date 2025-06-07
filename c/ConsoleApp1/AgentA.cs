using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO.Pipes;
using System.Diagnostics;
using System.Collections.Concurrent;

class Program
{
    static Dictionary<string, Dictionary<string, int>> fileWordCounts = new();
    static readonly object lockObj = new();
    static readonly BlockingCollection<string> pipeQueue = new();

    static void Main(string[] args)
    {
        SetCpuAffinity(0); 

        Console.WriteLine("Agent A started.");
        Console.WriteLine("Args received: " + string.Join(", ", args));

        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a directory path as an argument.");
            return;
        }

        string directoryPath = args[0];

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine("Directory does not exist: " + directoryPath);
            return;
        }

        Thread readingThread = new Thread(() => ReadAndIndexFiles(directoryPath));
        Thread sendingThread = new Thread(() => SendDataToMaster("agentA"));

        readingThread.Start();
        sendingThread.Start();

        readingThread.Join();

        
        Console.WriteLine("\n--- Word Counts Per File ---");
        lock (lockObj)
        {
            foreach (var fileEntry in fileWordCounts)
            {
                string filename = fileEntry.Key;
                var wordCounts = fileEntry.Value;
                Console.WriteLine($"File: {filename}");
                foreach (var wordEntry in wordCounts)
                {
                    Console.WriteLine($"{wordEntry.Key}: {wordEntry.Value}");
                }

                int totalWords = 0;
                foreach (var count in wordCounts.Values)
                    totalWords += count;
                Console.WriteLine($"Total words: {totalWords}\n");
            }
        }

        pipeQueue.CompleteAdding(); 
        sendingThread.Join();

        Console.WriteLine("Agent A finished.");
    }

    static void SetCpuAffinity(int cpuIndex)
    {
#if WINDOWS || LINUX
        try
        {
            var process = Process.GetCurrentProcess();
            IntPtr mask = (IntPtr)(1 << cpuIndex);
            process.ProcessorAffinity = mask;
            Console.WriteLine($"Agent A running on CPU core {cpuIndex}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not set CPU affinity: " + ex.Message);
        }
#else
        Console.WriteLine("CPU affinity setting is not supported on this platform.");
#endif
    }

    static void ReadAndIndexFiles(string directoryPath)
    {
        string[] files = Directory.GetFiles(directoryPath, "*.txt");
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            Dictionary<string, int> wordCounts = IndexWords(text);
            string filename = Path.GetFileName(file);

            lock (lockObj)
            {
                fileWordCounts[filename] = wordCounts;
            }

            
            foreach (var wordEntry in wordCounts)
            {
                string line = $"{filename}:{wordEntry.Key}:{wordEntry.Value}";
                pipeQueue.Add(line);
            }
        }
    }

    static Dictionary<string, int> IndexWords(string text)
    {
        Dictionary<string, int> wordCounts = new();
        foreach (Match match in Regex.Matches(text.ToLower(), @"\b\w+\b"))
        {
            string word = match.Value;
            if (wordCounts.ContainsKey(word))
                wordCounts[word]++;
            else
                wordCounts[word] = 1;
        }
        return wordCounts;
    }

    static void SendDataToMaster(string pipeName)
    {
        try
        {
            using NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.Out);
            pipeClient.Connect();
            using StreamWriter writer = new(pipeClient) { AutoFlush = true };

            foreach (var line in pipeQueue.GetConsumingEnumerable())
            {
                writer.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending data to master: " + ex.Message);
        }
    }
}
