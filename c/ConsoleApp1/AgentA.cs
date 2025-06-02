using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

class Program
{
    // This dictionary holds: filename -> (word -> count)
    static Dictionary<string, Dictionary<string, int>> fileWordCounts = new();

    static void Main(string[] args)
    {
        Console.WriteLine("Agent A started.");

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

        // Start the file reading thread
        Thread readingThread = new Thread(() => ReadAndIndexFiles(directoryPath));
        readingThread.Start();

        // Wait for the reading thread to finish before exiting
        readingThread.Join();

        // For now, just print the result to console
        foreach (var fileEntry in fileWordCounts)
        {
            string filename = fileEntry.Key;
            var wordCounts = fileEntry.Value;
            Console.WriteLine($"File: {filename}");
            foreach (var wordEntry in wordCounts)
            {
                Console.WriteLine($"{wordEntry.Key}: {wordEntry.Value}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("Agent A finished.");
    }

    static void ReadAndIndexFiles(string directoryPath)
    {
        string[] files = Directory.GetFiles(directoryPath, "*.txt");

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);

            // Index words in this file
            Dictionary<string, int> wordCounts = IndexWords(text);

            // Add to the main dictionary
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

        // Use regex to extract words ignoring punctuation
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
}

