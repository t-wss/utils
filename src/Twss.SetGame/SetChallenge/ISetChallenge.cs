using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Twss.SetGame.SetChallenge;


/// <summary>Defines the interface for an algorithm which implements the "Set challenge":
/// For all "Set" decks with a given number of cards (aka. deck size),
/// determine the number of decks which don't contain a set.</summary>
public interface ISetChallenge
{
  // This will be called a lot => avoid the overhead of an event, just use a delegate.
  /// <summary>Represents a method receiving the evaluation results for a given "Set" deck.</summary>
  /// <param name="deck">The deck which has been evaluated.</param>
  /// <param name="combinationsTested">The number of 3-card combinations (distinct cards)
  /// from given <paramref name="deck"/> which have been checked for sets.</param>
  /// <param name="combinationsAreSets">The number of 3-card combinations (distinct cards)
  /// from given <paramref name="deck"/> which have been found to be a "Set".</param>
  delegate void DeckEvaluatedAction(ReadOnlySpan<SetCard> deck, int combinationsTested, int combinationsAreSets);

  /// <summary>Gets or sets the callback to be invoked when a deck has been evaluated.</summary>
  /// <remarks>
  /// <para>When a callback is set then the implementation must invoke the callback for every deck that it processes;
  /// it must also be invoked for decks with a number of cards different from the nominal deck size
  /// (for example, when the algorithm processes decks incrementally).</para>
  /// <para>The implementation must ensure that invocations are thread-safe.</para>
  /// <para>The implementation may cache the callback locally while running the algorithm;
  /// the caller mustn't change the value while <see cref="RunAsync"/> is executing.</para>
  /// </remarks>
  DeckEvaluatedAction? DeckEvaluatedCallback { get; set; }

  /// <summary>Runs the algorithm.</summary>
  /// <param name="deckSize">The number of cards per deck which should be evaluated.</param>
  /// <param name="include">The cards that every deck to be evalauted should contain.
  /// When the collection is null or empty then the algorithm should build decks from all elements
  /// in <see cref="SetCard.CardGame"/> except for excluded cards.</param>
  /// <param name="exclude">The cards that no deck to be evaluated should contain.</param>
  /// <param name="cancellationToken">The token to monitor for cancellation requests.
  /// The default value is <see cref="CancellationToken.None"/>.</param>
  /// <returns>The determined result (see <see cref="ISetChallenge"/> summary)
  /// or <c>-1</c> when the algorithm couldn't find a definite answer.</returns>
  /// <exception cref="ArgumentException"><paramref name="deckSize"/> is less than 3 or greater than 81
  /// - OR - <paramref name="include"/> contains invalid or duplicate elements
  /// - OR - <paramref name="include"/> contains more cards than <paramref name="deckSize"/>
  /// - OR - <paramref name="exclude"/> contains invalid or duplicate elements
  /// - OR - <paramref name="exclude"/> contains elements which are also in <paramref name="include"/>.</exception>
  Task<long> RunAsync(
    int deckSize,
    SetCard[]? include,
    SetCard[]? exclude,
    CancellationToken cancellationToken = default);

  /// <summary>Throws an argument exception if any of the given arguments doesn't conform
  /// to the restrictions of the <see cref="RunAsync"/> method.</summary>
  /// <inheritdoc cref="ISetChallenge.RunAsync" path="//param"/>
  /// <inheritdoc cref="ISetChallenge.RunAsync" path="//exception"/>
  public static void ThrowIfRunArgsInvalid(int deckSize, SetCard[]? include, SetCard[]? exclude)
  {
    if (deckSize < 3 || deckSize > SetCard.CardGame.Length)
      throw new ArgumentOutOfRangeException(nameof(deckSize));

    if (include != null && include.Length > 0)
    {
      if (!SetMethods.CheckDeckValid(include))
        throw new ArgumentException($"{nameof(include)} contains invalid or duplicate elements.");

      if (include.Length > deckSize)
        throw new ArgumentException($"{nameof(include)} contains more elements than given {nameof(deckSize)}.");
    }

    if (exclude != null && exclude.Length > 0)
    {
      if (!SetMethods.CheckDeckValid(exclude))
        throw new ArgumentException($"{nameof(exclude)} contains invalid or duplicate elements.");

      if (include != null && include.Length > 0 && exclude.Intersect(include).Count() > 0)
        throw new ArgumentException($"{nameof(exclude)} contains elements which are also in {nameof(include)}.");
    }
  }
}
