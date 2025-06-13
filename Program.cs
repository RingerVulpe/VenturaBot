using System;
using System.Threading.Tasks;

namespace VenturaBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("[Program] Main() start");
            try
            {
                var bot = new Bot();
                await bot.RunAsync();
                Console.WriteLine("[Program] Bot.InitializeAsync() returned (should never happen)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Program] Exception thrown:");
                Console.WriteLine(ex);
            }
        }
    }
}
