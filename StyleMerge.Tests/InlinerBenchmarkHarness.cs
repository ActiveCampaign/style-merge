using CsQuery;
using System;
using System.Diagnostics;

namespace StyleMerge.Tests
{
    public class InlinerBenchmarkHarness
    {
        public void InliningBenchmark()
        {
            var runs = 5000;

            var startingHtmlContent = Inputs.EmailACIDTest;
            var runTime = Stopwatch.StartNew();
            for (var i = 0; i < runs; i++)
            {
                var result = Inliner.ProcessHtml(startingHtmlContent);
            }
            runTime.Stop();
            Console.WriteLine("Test Runs: {0}x, Total Time: {1}, Time/Run: {2}ms, Starting Content Length: {3}",
                runs, runTime.Elapsed, runTime.ElapsedMilliseconds / (double)runs, startingHtmlContent.Length);
        }

        public void CsQueryParseBenchmark()
        {
            var runs = 5000;

            var startingHtmlContent = Inputs.EmailACIDTest;
            var runTime = Stopwatch.StartNew();
            for (var i = 0; i < runs; i++)
            {
                var result = CQ.Create(startingHtmlContent);
            }
            runTime.Stop();
            Console.WriteLine("Test Runs: {0}x, Total Time: {1}, Time/Run: {2}ms, Starting Content Length: {3}",
                runs, runTime.Elapsed, runTime.ElapsedMilliseconds / (double)runs, startingHtmlContent.Length);
        }

    }
}
