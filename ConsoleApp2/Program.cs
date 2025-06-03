using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Master started. Waiting for Agent A...");

        Thread agentAThread = new Thread(() => HandleAgentPipe("agentA"));
        agentAThread.Start();
        agentAThread.Join();

        Console.WriteLine("Master finished.");
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
                Console.WriteLine($"Received: {line}");
            }

            Console.WriteLine($"Finished receiving data from {pipeName}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {pipeName} handler: {ex.Message}");
        }
    }
}
