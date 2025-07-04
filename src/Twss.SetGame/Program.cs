using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Twss.SetGame.SetChallenge;


namespace Twss.SetGame;


public class Program
{
  #region ALGORITHM PARAMETERS

  public static readonly ISetChallenge Algorithm = new SetChallenge.Algorithm2.Algorithm2();

  public const int DeckSize = 21;

  public static readonly SetCard[]? Include = null;

  public static readonly SetCard[]? Exclude = null;

  public static TimeSpan StatusRate = TimeSpan.FromSeconds(2);

  #endregion ALGORITHM PARAMETERS

  #region FILEDS

  private static CancellationTokenSource? _cancellationTokenSource;

  private static SetCard[]? _deckWithNoSetsExample = null;

  /// <summary>Array of <see cref="DeckStatistics"/>; array index represents deck size.</summary>
  private static DeckStatistics[] _statistics = Enumerable.Range(0, SetCard.CardGame.Length + 1)
    .Select(i => new DeckStatistics() { DeckSize = i })
    .ToArray();

  private static DeckStatistics _statusPrevious = new();

  private static TimeSpan _statusNextInvocation = StatusRate;

  private static Stopwatch _stopwatch = Stopwatch.StartNew();

  #endregion FIELDS

  public static void Main(string[] args)
  {
    InitializeCtrlCHandler();

    ISetChallenge algorithm = Algorithm;
    algorithm.DeckEvaluatedCallback = HandleDeckEvaluated;

    WriteProlog(algorithm, DeckSize);

    // Currently do not care about the return value, it's just about the performance.
    bool hasBeenCanceled = false;
    algorithm.RunAsync(DeckSize, Include, Exclude, _cancellationTokenSource!.Token)
      .ContinueWith(task =>
      {
        if (task.IsCanceled)
          hasBeenCanceled = true;
        if (task.IsFaulted)
          throw task.Exception.InnerException ?? task.Exception;
      })
      .GetAwaiter().GetResult();

    WriteEpilog(hasBeenCanceled);
 
    WriteLine("");
    WriteLine("Press any key to exit.");
    Console.ReadKey();
  }

  private static void GeneratePeriodicStatus()
  {
    TimeSpan timeNow = _stopwatch.Elapsed;
    if (timeNow >= _statusNextInvocation)
    {
      _statusNextInvocation += StatusRate;

      DeckStatistics totalStats = new()
      {
        CombinationsCount = _statistics.Sum(s => s.CombinationsCount),
        CombinationsAreSetsCount = _statistics.Sum(s => s.CombinationsAreSetsCount),
        DecksCount = _statistics.Sum(s => s.DecksCount),
        DecksWithNoSetCount = _statistics.Sum(s => s.DecksWithNoSetCount)
      };

      WriteStatus(totalStats, _statusPrevious, timeNow, StatusRate);
      _statusPrevious = totalStats;
    }
  }

  private static void HandleDeckEvaluated(ReadOnlySpan<SetCard> deck, int combinationsTested, int combinationsAreSets)
  {
    DeckStatistics stats = _statistics[deck.Length];
    stats.AddOne(combinationsTested, combinationsAreSets);

    bool deckWithNoSet = combinationsTested > 0 && combinationsAreSets == 0;
    if (deckWithNoSet && (_deckWithNoSetsExample == null || deck.Length > _deckWithNoSetsExample.Length))
    {
      _deckWithNoSetsExample = deck.ToArray().OrderBy(card => card.Index).ToArray();
    }

    GeneratePeriodicStatus();
  }

  /// <summary>Initialize CTRL+C handler: On first CTRL+C request cancel and don't terminate;
  /// any subsequent CTRL+C will terminate the application.</summary>
  private static void InitializeCtrlCHandler()
  {
    _cancellationTokenSource = new CancellationTokenSource();

    Console.CancelKeyPress += (sender, e) =>
    {
      if (!_cancellationTokenSource.IsCancellationRequested)
      {
        _cancellationTokenSource.Cancel();
        e.Cancel = true;
      }
      else
      {
        Environment.Exit(1);
      }
    };
  }

  private static void WriteEpilog(bool hasBeenCanceled)
  {
    WriteLine("");
    WriteLine(hasBeenCanceled
      ? $"Canceled after {_stopwatch.Elapsed.TotalMinutes:F2} minutes."
      : $"Completed in {_stopwatch.Elapsed.TotalMinutes:F2} minutes.");
    WriteLine("");

    // Write summary per deck size for decks which have actually been evaluated.
    int toDisplayCount = _statistics.Where(s => s.DecksCount > 0)
      .Select(s => s.DeckSize)
      .LastOrDefault(_statistics.Length);
    foreach (var stats in _statistics.Skip(1).Take(toDisplayCount))
    {
      WriteStatistics(stats);
      WriteLine("");
    }

    if (_deckWithNoSetsExample != null)
    {
      WriteLine("Example deck without sets:");
      foreach (SetCard card in _deckWithNoSetsExample)
        WriteLine($"  {card.ToString()}");
    }
  }

  private static void WriteLine(string text)
  {
    Console.WriteLine(text);
  }

  private static void WriteProlog(ISetChallenge algorithm, int deckSize)
  {
    WriteLine($"Run algorithm {algorithm.GetType().Name}.");
    WriteLine($"Evaluating {deckSize} cards per deck ...");
    WriteLine("");

    WriteStatusHeader();
  }

  private static void WriteStatistics(DeckStatistics statistics)
  {
    WriteLine($"Number of cards per deck: {statistics.DeckSize}");
    WriteLine($"Number of 3-card combinations tested: {statistics.CombinationsCount:#,##0}");
    WriteLine($"Number of 3-card combinations which were sets: {statistics.CombinationsAreSetsCount:#,##0}");
    WriteLine($"Number of decks analyzed: {statistics.DecksCount:#,##0}");
    WriteLine($"Number of decks with no set: {statistics.DecksWithNoSetCount:#,##0}");
  }

  private static void WriteStatus(
    DeckStatistics stats,
    DeckStatistics statsPrevious,
    TimeSpan timeNow,
    TimeSpan timeDiff)
  {
    long combinationsOf3TestedDiff = stats.CombinationsCount - statsPrevious.CombinationsCount;
    long combinationsOf3PerSec = (long)Math.Round(combinationsOf3TestedDiff / timeDiff.TotalSeconds);

    WriteLine($"{timeNow.TotalSeconds,8:N1}"
      + $" {stats.CombinationsCount,20:N0}"
      + $" {combinationsOf3PerSec,20:N0}"
      + $" {stats.DecksCount,17:N0}"
      + $" {stats.DecksWithNoSetCount,17:N0}");
  }

  private static void WriteStatusHeader()
  {
    WriteLine("Time (s)"
      + "       3-cards tested"
      + "   3-cards tested / s"
      + "    Decks analyzed"
      + "     Decks w/o set");
  }
}
