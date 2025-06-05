using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

class Program
{
    static Dictionary<string, Dictionary<string, int>> combinedIndex = new();
    static object lockObj = new();

    static void Main()
    {
        Console.WriteLine("Master started. Waiting for Agent A and Agent B...");

        Thread agentAThread = new Thread(() => HandleAgentPipe("agentA"));
        Thread agentBThread = new Thread(() => HandleAgentPipe("agentB"));

        agentAThread.Start();
        agentBThread.Start();

        agentAThread.Join();
        agentBThread.Join();

        Console.WriteLine("\n--- Combined Index ---");
        foreach (var word in combinedIndex.Keys)
        {
            foreach (var file in combinedIndex[word])
            {
                Console.WriteLine($"{file}:{word}:{combinedIndex[word][file]}");
            }
        }

        Console.WriteLine("\nMaster finished.");
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
                int count = int.Parse(parts[2]);

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

