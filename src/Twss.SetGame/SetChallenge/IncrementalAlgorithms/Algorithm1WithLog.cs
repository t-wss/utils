using System;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Twss.SetGame.SetChallenge.IncrementalAlgorithms;


/// <summary>Only for development purposes.</summary>
internal sealed class Algorithm1WithLog : Algorithm1
{
  private StringBuilder Sb { get; } = new();

  protected override void AdaptBuffers()
  {
    Sb.Append("Evaluation awaits/duration (ms):");
    for (int i = 0; i < AdaptBuffersAwaits.Length; i++)
      Sb.Append(' ').Append(AdaptBuffersAwaits[i]).Append('/').Append(AdaptBuffersAwaitTimes[i] * 1000 / Stopwatch.Frequency);
    LogSb();

    var currentBufferCounts = Buffers.Select(bufferList => bufferList.Count).ToArray();

    base.AdaptBuffers();

    var newBufferCounts = Buffers.Select(bufferList => bufferList.Count).ToArray();
    for (int i = 0; i < newBufferCounts.Length; i++)
    {
      if (newBufferCounts[i] > currentBufferCounts[i])
        Console.WriteLine($"Add DeckBuffer for deck size {i} (now {newBufferCounts[i]}).");
      if (newBufferCounts[i] < currentBufferCounts[i])
        Console.WriteLine($"Remove DeckBuffer for deck size {i} (now {newBufferCounts[i]}).");
    }
  }

  private void LogSb()
  {
    Console.WriteLine(Sb.ToString());
    Sb.Clear();
  }
}
