using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GaSchedule.Algorithm;

namespace GaSchedule
{
    class ConsoleApp
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("GaSchedule Version {0} C# .NET Core. Making a Class Schedule Using a Genetic Algorithm.", Assembly.GetExecutingAssembly().GetName().Version);
            System.Console.WriteLine("Copyright (C) 2020 Miller Cy Chan.");

            Stopwatch stopwatch = Stopwatch.StartNew();

            var FILE_NAME = args.Length > 0 ? args[0] : "GaSchedule.json";
            var configuration = new GaSchedule.Algorithm.Configuration();
            configuration.ParseFile(FILE_NAME);
            
            var ga = new GeneticAlgorithm<Schedule>(new Schedule(configuration));
            ga.Run();
            var htmlResult = HtmlOutput.GetResult(ga.Result);

            var tempFilePath = Path.GetTempPath() + FILE_NAME.Replace(".json", ".htm");
            using (StreamWriter outputFile = new StreamWriter(tempFilePath))
            {
                outputFile.WriteLine(htmlResult);
            }
            System.Console.WriteLine("");
            System.Console.WriteLine(@"Completed in {0:s\.fff} secs with peak memory usage of {1}.", stopwatch.Elapsed, Process.GetCurrentProcess().PeakWorkingSet64.ToString("#,#"));

            using (var proc = new Process())
            {
                proc.StartInfo.FileName = tempFilePath;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "open";
                proc.Start();
            }
        }
    }
}
