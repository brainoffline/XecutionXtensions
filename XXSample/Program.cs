using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XXLib;

namespace XXSample
{
    class Program
    {
        static void Main(string[] args)
        {
            MainTask().Wait();
        }

        static async Task MainTask()
        {
            string value = await new XX<string>()
                .Execute(() => "Hello");

            Console.WriteLine(value);
        }
    }
}
