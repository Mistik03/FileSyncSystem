using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace PeerA
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 5000;
            string sharedFolder = Path.Combine(Directory.GetCurrentDirectory(), "Shared");

            if (!Directory.Exists(sharedFolder))
            {
                Directory.CreateDirectory(sharedFolder);
            }

            Console.WriteLine($"[Peer A] Sharing files from: {sharedFolder}");
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("[Peer A] Listening for incoming connections...");

            while (true)
            {
                using TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("[Peer A] Client connected.");
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream);
                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                //Send list of files
                string[] files = Directory.GetFiles(sharedFolder);
                writer.WriteLine(files.Length);
                foreach (string file in files)
                {
                    writer.WriteLine(Path.GetFileName(file));
                }

                //Recieve rquested files
                int filesToSend = int.Parse(reader.ReadLine());
                for (int i = 0; i < filesToSend; i++)
                {
                    string requestedFile = reader.ReadLine();
                    string fullPath = Path.Combine(sharedFolder, requestedFile);

                    if (File.Exists(fullPath))
                    {
                        byte[] fileBytes = File.ReadAllBytes(fullPath);
                        writer.WriteLine(fileBytes.Length);
                        stream.Write(fileBytes, 0, fileBytes.Length);
                        stream.Flush();
                        Console.WriteLine($"[Peer A] Sent file: {requestedFile} ({fileBytes.Length} bytes)");
                    }
                    else
                    {
                        writer.WriteLine("0");
                        Console.WriteLine($"[Peer A] File not found: {requestedFile}");
                    }
                }

                Console.WriteLine("[Peer A] Sync completed. Waiting for next client...\n");
            }
        }
    }
}