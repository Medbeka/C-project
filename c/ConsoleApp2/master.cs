using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;

class Program
{
    static ConcurrentDictionary<string, Dictionary<string, int>> combinedIndex = new();
    static readonly object lockObj = new();

    static void Main(string[] args)
    {
        SetCpuAffinity(2); // Master on CPU core 2

        Console.WriteLine("Master started. Waiting for Agent A and Agent B...");

        if (args.Length < 2)
        {
            Console.WriteLine("Please provide two pipe names as arguments.");
            return;
        }

        string pipe1 = args[0];
        string pipe2 = args[1];

        Thread agentAThread = new Thread(() => HandleAgentPipe(pipe1));
        Thread agentBThread = new Thread(() => HandleAgentPipe(pipe2));

        agentAThread.Start();
        agentBThread.Start();

        agentAThread.Join();
        agentBThread.Join();

        Console.WriteLine("\n--- Combined Index ---");
        foreach (var word in combinedIndex.Keys)
        {
            foreach (var file in combinedIndex[word])
            {
                Console.WriteLine($"{file.Key}:{word}:{file.Value}");
            }
        }

        Console.WriteLine("\nMaster finished.");
    }

    static void SetCpuAffinity(int cpuIndex)
    {
        var process = Process.GetCurrentProcess();
        IntPtr mask = (IntPtr)(1 << cpuIndex);
        process.ProcessorAffinity = mask;
        Console.WriteLine($"Master running on CPU core {cpuIndex}");
    }

    static void HandleAgentPipe(string pipeName)
    {
        try
        {
            using NamedPipeServerStream pipeServer = new(pipeName, PipeDirection.In);
            pipeServer.WaitForConnection();

            using StreamReader reader = new(pipeServer);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(':');
                if (parts.Length != 3) continue;

                string file = parts[0];
                string word = parts[1];
                if (!int.TryParse(parts[2], out int count)) continue;

                lock (lockObj)
                {
                    if (!combinedIndex.ContainsKey(word))
                        combinedIndex[word] = new Dictionary<string, int>();

                    if (!combinedIndex[word].ContainsKey(file))
                        combinedIndex[word][file] = 0;

                    combinedIndex[word][file] += count;
                }
            }

            Console.WriteLine($"Finished receiving from {pipeName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {pipeName} handler: {ex.Message}");
        }
    }
}
