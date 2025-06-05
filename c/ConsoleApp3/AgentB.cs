using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.Threading;

class Program
{
    static Dictionary<string, Dictionary<string, int>> fileWordCounts = new();

    static void Main(string[] args)
    {
        Console.WriteLine("Agent B started.");

        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a directory path as an argument.");
            return;
        }

        string directoryPath = args[0];

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine("Directory does not exist.");
            return;
        }

        Thread readingThread = new Thread(() => ReadAndIndexFiles(directoryPath));
        readingThread.Start();
        readingThread.Join();

        Thread sendingThread = new Thread(() => SendDataToMaster("agentB"));
        sendingThread.Start();
        sendingThread.Join();

        Console.WriteLine("Agent B finished.");
    }

    static void ReadAndIndexFiles(string directoryPath)
    {
        string[] files = Directory.GetFiles(directoryPath, "*.txt");

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            Dictionary<string, int> wordCounts = IndexWords(text);
            string filename = Path.GetFileName(file);

            lock (fileWordCounts)
            {
                fileWordCounts[filename] = wordCounts;
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

            lock (fileWordCounts)
            {
                foreach (var fileEntry in fileWordCounts)
                {
                    string filename = fileEntry.Key;
                    foreach (var wordEntry in fileEntry.Value)
                    {
                        string line = $"{filename}:{wordEntry.Key}:{wordEntry.Value}";
                        writer.WriteLine(line);
                    }
                }
            }

            Console.WriteLine("Data sent to master.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending data: {ex.Message}");
        }
    }
}
