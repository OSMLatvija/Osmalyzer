using System;
using System.IO;
using System.Reflection;

namespace Osmalyzer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
#if REMOTE_EXECUTION
            string executionPoint = "Remote";
#else
            string executionPoint = "Local";
#endif
            Console.WriteLine(executionPoint + " execution (running from \"" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\", current path at \"" + Directory.GetCurrentDirectory() + "\")");


            Runner.Run();
        }
    }
}