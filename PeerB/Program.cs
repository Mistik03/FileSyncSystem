using System;
using System.IO;
using System.Net.Sockets;

namespace PeerB
{
    class Program
    {
        static void Main(string[] args)
        {
            const string serverIP = "127.0.0.1"; // or LAN IP
            const int port = 5000;
            string syncFolder = Path.Combine(Directory.GetCurrentDirectory(), "Synced");

            if (!Directory.Exists(syncFolder))
            {
                Directory.CreateDirectory(syncFolder);
            }

            try
            {
                using TcpClient client = new TcpClient();
                Console.WriteLine("[Peer B] Connecting to Peer A...");
                client.Connect(serverIP, port);
                Console.WriteLine("[Peer B] Connected.");

                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream);
                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                //Recieve file list
                int fileCount = int.Parse(reader.ReadLine());
                Console.WriteLine($"[Peer B] Peer A has {fileCount} file(s).");

                string[] peerAFiles = new string[fileCount];
                for (int i = 0; i < fileCount; i++)
                {
                    peerAFiles[i] = reader.ReadLine();
                }

                //Check which files are missing
                string[] localFiles = Directory.GetFiles(syncFolder);
                var localFileNames = new HashSet<string>(Array.ConvertAll(localFiles, Path.GetFileName));

                var filesToRequest = new List<string>();
                foreach (string file in peerAFiles)
                {
                    if (!localFileNames.Contains(file))
                    {
                        Console.WriteLine($"[Peer B] Missing file: {file}");
                        filesToRequest.Add(file);
                    }
                }

                //Request missing files
                writer.WriteLine(filesToRequest.Count);
                foreach (string file in filesToRequest)
                {
                    writer.WriteLine(file);
                    int size = int.Parse(reader.ReadLine());

                    if (size > 0)
                    {
                        byte[] buffer = new byte[size];
                        int bytesRead = 0;
                        while (bytesRead < size)
                        {
                            int read = stream.Read(buffer, bytesRead, size - bytesRead);
                            if (read == 0) break; // Connection closed
                            bytesRead += read;
                        }

                        string savePath = Path.Combine(syncFolder, file);
                        File.WriteAllBytes(savePath, buffer);
                        Console.WriteLine($"[Peer B] Downloaded: {file} ({size} bytes)");
                    }
                    else
                    {
                        Console.WriteLine($"[Peer B] Failed to get file: {file}");
                    }
                }

                Console.WriteLine("[Peer B] Sync complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Peer B] Error: {ex.Message}");
            }
        }
    }
}