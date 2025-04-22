using SpaceCG.Net;

namespace Test.ConsoleApp.NET
{
    internal class Program
    {
        static RPCServer rpcServer;
        static void Main(string[] args)
        {
            string s = "12aa";

            rpcServer = new RPCServer(2001, "TestName");
            rpcServer.Start();

            Console.WriteLine(Console.Out);
            Console.WriteLine(Console.Out is TextWriter);

            Convert.FromHexString(s);
            Console.WriteLine("Hello, World!");

            Console.ReadKey();
        }
    }
}