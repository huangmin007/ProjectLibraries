using SpaceCG.Net;

namespace Test.ConsoleApp.NET
{
    internal class Program
    {
        //static RPCServer rpcServer;
        static RPCServer2 rpcServer2;
        static RPCClient2 rpcClient2;
        

        static void Main(string[] args)
        {
            string s = "12aa";

            //rpcServer = new RPCServer(2001, "TestName");
            //rpcServer.Start();

            rpcServer2 = new RPCServer2(2002, "TestName2");
            rpcServer2.StartAsync();

            //rpcClient2 = new RPCClient2("127.0.0.1", 2002);
            //rpcClient2.StartAsync();

            Console.WriteLine(Console.Out);
            Console.WriteLine(Console.Out is TextWriter);

            Convert.FromHexString(s);
            Console.WriteLine("Hello, World!");

            Console.ReadKey();

            rpcServer2.Dispose();
            Console.WriteLine("Server disposed");
            Console.ReadKey();
        }
    }
}