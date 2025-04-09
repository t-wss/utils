using System;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

using Xunit;


namespace Twss.SetGame.SetChallenge;


#pragma warning disable SYSLIB0046 // ControlledExecution.Run should not be used


// Long-running tests are commented out; uncomment locally to run manually.

/// <summary>Provides basic tests for an <see cref="IAlgorithm"/> implementation.</summary>
public abstract class AlgorithmTestsBase
{
  #region CONSTANTS

  /// <summary>Default timeout for aborts in algorithm implementation tests
  /// (i.e. the longest amount of time that an algorithm may run/block without reacting).</summary>
  public static readonly TimeSpan AbortTimeout = TimeSpan.FromSeconds(4.0);

  #endregion CONSTANTS

  protected readonly ITestOutputHelper _output;

  protected AlgorithmTestsBase(ITestOutputHelper output)
  {
    _output = output;
  }

  /// <summary>Creates a new instance of the algorithm which should be used in a test.</summary>
  protected abstract IAlgorithm CreateAlgorithm();

  [Theory]
  [InlineData(3, 84_240L)] // BasicAlgorithm ~ 15 - 50 ms
  [InlineData(4, 1_579_500L)] // BasicAlgorithm ~ 0,5 - 1,5 s
  //[InlineData(5, 22_441_536L)] // BasicAlgorithm ~ 7 - 30 s
  //[InlineData(6, 247_615_056L)] // BasicAlgorithm ~ 1:20 - 8:00 min
  //[InlineData(7, 2_144_076_480L)] // BasicAlgorithm ~ 18 - ??? min
  public async Task RunAsync_ForAllDecks_ShouldReturnWellKnownResults(int deckSize, long expectedDecksWithNoSets)
  {
    // Collect IAlgorithm.DeckEvaluatedCallback data; output at the end of the test for information purposes.
    DeckStatistics statistics = new DeckStatistics() { DeckSize = deckSize };

    IAlgorithm algorithm = CreateAlgorithm();
    algorithm.DeckEvaluatedCallback = (deck, combinationsTested, combinationsAreSets) =>
    {
      Assert.True(SetCard.CheckDeckValid(deck));
      Assert.True(combinationsTested >= 0);
      Assert.True(combinationsAreSets >= 0);
      Assert.True(combinationsAreSets <= combinationsTested);

      statistics.AddOne(combinationsTested, combinationsAreSets);
    };

    long result = await algorithm.RunAsync(deckSize, null, null, TestContext.Current.CancellationToken);

    _output.WriteLine(statistics.ToString());
    Assert.Equal(expectedDecksWithNoSets, result);
  }

  [Fact]
  public void RunAsync_Canceled_ShouldReturnQuickly()
  {
    // Force abort when not completed/canceled in reasonable time.
    using CancellationTokenSource abortCts = new(AbortTimeout);
    ControlledExecution.Run(() =>
    {
      // Cancel after a short amount of time (which is less than it takes to produce the result).
      using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(100.0));

      IAlgorithm algorithm = CreateAlgorithm();
      algorithm.DeckEvaluatedCallback = (deck, combinationsTested, combinationsAreSets) =>
      {
        Assert.True(SetCard.CheckDeckValid(deck));
        Assert.True(combinationsTested >= 0);
        Assert.True(combinationsAreSets >= 0);
        Assert.True(combinationsAreSets <= combinationsTested);
      };

      algorithm.RunAsync(7, null, null, cts.Token)
        .ContinueWith(task =>
        {
          // Completed/canceled is fine, any exception is rethrown.
          if (task.IsFaulted)
            throw task.Exception.InnerException ?? task.Exception;
        })
        .GetAwaiter().GetResult();
    }, abortCts.Token);
  }

  [Fact]
  public async Task RunAsync_InvalidArgument_ThrowsArgumentException()
  {
    Func<Task>[] invalidInvocations = new[]
    {
      () => CreateAlgorithm().RunAsync(2, null, null),
      () => CreateAlgorithm().RunAsync(82, null, null),
      () => CreateAlgorithm().RunAsync(3, new SetCard[] { default }, null),
      () => CreateAlgorithm().RunAsync(6, GetSetCards(1, 2, 3, 2), null),
      () => CreateAlgorithm().RunAsync(4, GetSetCards(11, 21, 31, 41, 51), null),
      () => CreateAlgorithm().RunAsync(3, null, new SetCard[] { default }),
      () => CreateAlgorithm().RunAsync(6, null, GetSetCards(1, 2, 3, 2)),
      () => CreateAlgorithm().RunAsync(4, GetSetCards(5, 6, 7), GetSetCards(6))
    };

    foreach (var invalidInvocation in invalidInvocations)
      await Assert.ThrowsAnyAsync<ArgumentException>(invalidInvocation);
  }

  /// <summary>Runs the algorithm for 4-card decks out of 9 preselected cards (index 0, 10, 20, ..., 80)
  /// by excluding all cards except for those 9. This should yield 54 decks with no sets.</summary>
  [Fact]
  public async Task RunAsync_WithExclude()
  {
    SetCard[] exclude = Enumerable.Range(0, SetCard.CardGame.Count)
      .Where(i => i % 10 != 0)
      .Select(i => SetCard.CardGame[i])
      .ToArray();

    IAlgorithm algorithm = CreateAlgorithm();
    algorithm.DeckEvaluatedCallback = (deck, combinationsTested, combinationsAreSets) =>
    {
      Assert.True(SetCard.CheckDeckValid(deck));
      Assert.True(combinationsTested >= 0);
      Assert.True(combinationsAreSets >= 0);
      Assert.True(combinationsAreSets <= combinationsTested);

      Assert.DoesNotContain(exclude, deck);
    };

    long result = await algorithm.RunAsync(4, null, exclude, TestContext.Current.CancellationToken);
    Assert.Equal(54, result);
  }

  /// <summary>Runs the algorithm for 5-card decks with 3 preselected cards which already form a set.
  /// This should yield 0 decks with no sets (because the preselected cards are a set).</summary>
  [Fact]
  public async Task RunAsync_WithInclude()
  {
    SetCard[] include = GetSetCards(0, 10, 20);

    IAlgorithm algorithm = CreateAlgorithm();
    algorithm.DeckEvaluatedCallback = (deck, combinationsTested, combinationsAreSets) =>
    {
      Assert.True(SetCard.CheckDeckValid(deck));
      Assert.True(combinationsTested >= 0);
      Assert.True(combinationsAreSets >= 0);
      Assert.True(combinationsAreSets <= combinationsTested);

      foreach (SetCard included in include)
        Assert.True(deck.Contains(included));
    };

    long result = await algorithm.RunAsync(6, include, null, TestContext.Current.CancellationToken);
    Assert.Equal(0, result);
  }

  /// <summary>Runs algorithm for 'large' deck size, but constrains it with includes/excludes
  /// (=> reasonable number of permutations); throws when running takes unacceptably long.</summary>
  /// <remarks>
  /// When the algorithm creates permutations of given deck size first and then filters for include/exclude,
  /// it will take 'forever'; instead it must consider include/exclude when building permutations.
  /// </remarks>
  [Fact]
  public void RunAsync_WithIncludeExclude_ShouldNotTimeout()
  {
    SetCard[] include = GetSetCards(1, 3, 5, 14, 21);
    SetCard[] exclude = GetSetCards(0, 9, 10, 55);

    IAlgorithm algorithm = CreateAlgorithm();
    algorithm.DeckEvaluatedCallback = (deck, combinationsTested, combinationsAreSets) =>
    {
      Assert.True(SetCard.CheckDeckValid(deck));
      Assert.True(combinationsTested >= 0);
      Assert.True(combinationsAreSets >= 0);
      Assert.True(combinationsAreSets <= combinationsTested);

      foreach (SetCard included in include)
        Assert.True(deck.Contains(included));
      Assert.False(deck.ContainsAny(exclude));
    };

    // Force abort when not completed in reasonable time.
    using CancellationTokenSource abortCts = new(AbortTimeout);
    ControlledExecution.Run(() =>
    {
      algorithm.RunAsync(7, include, exclude).GetAwaiter().GetResult();
    }, abortCts.Token);
  }

  protected SetCard[] GetSetCards(params int[] indexes)
  {
    return indexes.Select(idx => SetCard.CardGame[idx]).ToArray();
  }
}
