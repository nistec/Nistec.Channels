using Nistec.Channels;
using System;
using System.IO;
using System.IO.Pipes;

namespace ChannelsClientDemo
{


   public class AnonymousPipeClientDemo : AnonymousPipeClient
    {

       public static void Run(string[] args)
       {

           Console.WriteLine("Client start...");

           using (AnonymousPipeClientDemo client = new AnonymousPipeClientDemo())
           {
               client.Execute(args);
           }
       }

       protected override AnonymousMessage ExecRequest(AnonymousMessage request)
       {
           
           object o= request.DecodeBody();
           Console.WriteLine("DecodeBody: {0}", o);
           AnonymousMessage response = new AnonymousMessage("ack","ok","exec requested completed",0);
           return response;     
       }

       public static void Run1(string[] args)
       {

           Console.WriteLine("Client start...");

           string parentSenderID;
           string parentReceiverID;

           //get pipe handle id
           parentSenderID = args[0];
           parentReceiverID = args[1];

           Console.WriteLine("Parent sender:{0}, receiver:{1}", parentSenderID, parentReceiverID);

           //create streams
           var receiver = new AnonymousPipeClientStream(PipeDirection.In, parentSenderID);
           var sender = new AnonymousPipeClientStream(PipeDirection.Out, parentReceiverID);

           //read data
           int dataReceive = receiver.ReadByte();
           Console.WriteLine("Client receive: " + dataReceive.ToString());

           //write data
           byte dataSend = 24;
           sender.WriteByte(dataSend);
           Console.WriteLine("Client send: " + dataSend.ToString());
       }

       public static void Run2(string[] args)
        {
            if (args.Length > 0)
            {
                using (PipeStream pipeClient =
                    new AnonymousPipeClientStream(PipeDirection.In, args[0]))
                {
                    // Show that anonymous Pipes do not support Message mode.
                    try
                    {
                        Console.WriteLine("[CLIENT] Setting ReadMode to \"Message\".");
                        pipeClient.ReadMode = PipeTransmissionMode.Byte;
                    }
                    catch (NotSupportedException e)
                    {
                        Console.WriteLine("[CLIENT] Execption:\n    {0}", e.Message);
                    }

                    Console.WriteLine("[CLIENT] Current TransmissionMode: {0}.",
                       pipeClient.TransmissionMode);

                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        // Display the read text to the console
                        string temp;

                        // Wait for 'sync message' from the server.
                        do
                        {
                            Console.WriteLine("[CLIENT] Wait for sync...");
                            temp = sr.ReadLine();
                        }
                        while (!temp.StartsWith("SYNC"));

                        // Read the server data and echo to the console.
                        while ((temp = sr.ReadLine()) != null)
                        {
                            Console.WriteLine("[CLIENT] Echo: " + temp);
                        }
                    }
                }
            }
            Console.Write("[CLIENT] Press Enter to continue...");
            Console.ReadLine();
        }
    }
}
