using System.Diagnostics;
using System.IO;
using System.Reflection;

using GaSchedule.Algorithm;
using GaSchedule.Model;

[assembly: AssemblyVersionAttribute("1.2.3")]
namespace GaSchedule
{
    class ConsoleApp
    {
        static void Main(string[] args)
        {       
            Stopwatch stopwatch = Stopwatch.StartNew();

            var FILE_NAME = args.Length > 0 ? args[0] : "GaSchedule.json";
            var configuration = new Configuration();
            configuration.ParseFile(FILE_NAME);

            var alg = new NsgaIII<Schedule>(new Schedule(configuration));
            // var alg = new Amga2<Schedule>(new Schedule(configuration));

            System.Console.WriteLine("GaSchedule Version {0} C# .NET Core. Making a Class Schedule Using {1}.", Assembly.GetExecutingAssembly().GetName().Version, alg.ToString());
            System.Console.WriteLine("Copyright (C) 2022 - 2023 Miller Cy Chan.");

            alg.Run();
            var htmlResult = HtmlOutput.GetResult(alg.Result);

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
