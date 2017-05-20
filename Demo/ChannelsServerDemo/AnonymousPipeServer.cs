using Nistec.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace ChannelsServerDemo
{
    public class AnonymousPipeServerDemo
    {
        const string filename = @"C:\Dev\Nistec\Git_4.0.2.0\_Demo\ChannelsDemo\ChannelsClientDemo\bin\Debug\ChannelsClientDemo.exe";
 

        public static void Run(string[] args)
        {

            AnonymousMessage request = new AnonymousMessage("request", "get data", "request demo", 0);
            var response= AnonymousPipeServer.SendDuplex(request, filename);
            var obj = response.DecodeBody();
            Console.WriteLine(obj);
            
            Console.WriteLine("Client execution finished");

            Console.ReadKey();
        }

        public static void Run1(string[] args)
        {
            //create streams
            var sender = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var receiver = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            //start client, pass pipe ids as command line parameter 
            string clientPath = filename;// @"...";
            string senderID = sender.GetClientHandleAsString();
            string receiverID = receiver.GetClientHandleAsString();

            var startInfo = new ProcessStartInfo(clientPath, senderID + " " + receiverID);
            startInfo.UseShellExecute = false;
            Process clientProcess = Process.Start(startInfo);

            //release resources handlet by client
            sender.DisposeLocalCopyOfClientHandle();
            receiver.DisposeLocalCopyOfClientHandle();

            //write data
            byte dataSend = 48;
            sender.WriteByte(dataSend);
            Console.WriteLine("Parent send: " + dataSend.ToString());

            //read data
            int dataReceive = receiver.ReadByte();
            Console.WriteLine("Parent receive: " + dataReceive.ToString());

            //wait until client is closed
            clientProcess.WaitForExit();
            Console.WriteLine("Client execution finished");

            Console.ReadKey();
        }

        public static void Run2(string[] args)
        {
            Process pipeClient = new Process();

            

            pipeClient.StartInfo.FileName = filename;// "pipeClient.exe";

            using (AnonymousPipeServerStream pipeServer =
                new AnonymousPipeServerStream(PipeDirection.Out,
                HandleInheritability.Inheritable))
            {
                // Show that anonymous pipes do not support Message mode.
                try
                {
                    Console.WriteLine("[SERVER] Setting ReadMode to \"Message\".");
                    pipeServer.ReadMode = PipeTransmissionMode.Byte;
                }
                catch (NotSupportedException e)
                {
                    Console.WriteLine("[SERVER] Exception:\n    {0}", e.Message);
                }

                Console.WriteLine("[SERVER] Current TransmissionMode: {0}.",
                    pipeServer.TransmissionMode);

                // Pass the client process a handle to the server.
                pipeClient.StartInfo.Arguments =
                    pipeServer.GetClientHandleAsString();
                pipeClient.StartInfo.UseShellExecute = false;
                pipeClient.Start();

                pipeServer.DisposeLocalCopyOfClientHandle();

                try
                {
                    // Read user input and send that to the client process.
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        sw.AutoFlush = true;
                        // Send a 'sync message' and wait for client to receive it.
                        sw.WriteLine("SYNC");
                        pipeServer.WaitForPipeDrain();
                        // Send the console input to the client process.
                        Console.Write("[SERVER] Enter text: ");
                        sw.WriteLine(Console.ReadLine());
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e)
                {
                    Console.WriteLine("[SERVER] Error: {0}", e.Message);
                }
            }

            pipeClient.WaitForExit();
            pipeClient.Close();
            Console.WriteLine("[SERVER] Client quit. Server terminating.");
        }
    }
}
