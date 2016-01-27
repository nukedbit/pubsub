using System;

namespace PubSubDemoConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Udp Console";
            if (args.Length == 0)
            {
                Console.WriteLine("Specify scenario");
                Console.WriteLine("PubSubDemoConsole.exe pingpong");
                Console.WriteLine("PubSubDemoConsole.exe massive");
                return;
            }
            else if (args[0] == "pingpong")
            {
                Console.Title = "Udp Console - PingPong";
                try
                {
                    new PingPongScenario().Run();
                }
                catch (Exception ex)
                {
                    
                }
            }
            else if (args[0] == "massive")
            {
                Console.Title = "Udp Console - Massive";
                new MassiveScenario().Run();
            }
            Console.ReadLine();
        }
    }
}
