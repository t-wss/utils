using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;


namespace Twss.Utils.Benchmarks;


// This BenchmarkDotNet entry point is project-agnostic; it can be reused in other projects.
internal class Program
{
  internal static int Main(string[] args)
  {
    IConfig benchmarkConfig = DefaultConfig.Instance;
    // If you want to debug the benchmarks in Visual Studio, uncomment the following line temporarily.
    // benchmarkConfig = new DebugInProcessConfig();

    // Adjust benchmark configuration.
    benchmarkConfig = benchmarkConfig
      .WithArtifactsPath(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts"))
      // If you want to benchmark on multiple runtimes, uncomment the following lines.
      // Alternatively, call from command line "dotnet run -c Release -- --runtimes net8.0 net9.0".
      // .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80))
      // .AddJob(Job.Default.WithRuntime(CoreRuntime.Core90))
      ;

    IEnumerable<Summary> results = BenchmarkSwitcher
      .FromAssembly(typeof(Program).Assembly)
      .Run(args, benchmarkConfig);

    // No benchmarks have been run.
    if (!results.Any())
      return 1;

    // At least one benchmark has failed.
    if (results.Any(result => result.HasCriticalValidationErrors))
      return 2;

    BenchmarkReport[] benchmarkReports = results.SelectMany(result => result.Reports).ToArray();
    if (benchmarkReports.Any(report => !report.BuildResult.IsBuildSuccess || !report.AllMeasurements.Any()))
      return 3;

    // Indicate success.
    return 0;
  }
}
