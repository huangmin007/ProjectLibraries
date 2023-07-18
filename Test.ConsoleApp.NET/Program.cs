namespace Test.ConsoleApp.NET
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string s = "12aa";

            Console.WriteLine(Console.Out);
            Console.WriteLine(Console.Out is TextWriter);

            Convert.FromHexString(s);
            Console.WriteLine("Hello, World!");
        }
    }
}