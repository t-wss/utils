using System;
using System.Threading;
using System.Threading.Tasks;


namespace Twss.SetGame.SetChallenge;


/// <summary>Defines the interface for an algorithm which implements the "Set challenge":
/// For "Set" decks with a given number of cards (i.e. deck size),
/// determine the number of decks which don't contain a set.</summary>
public interface IAlgorithm
{
  // This will be called a lot => avoid the overhead of an event, just use a delegate.
  /// <summary>Represents a method receiving the evaluation results for a given "Set" deck.</summary>
  /// <param name="deck">The deck which has been evaluated.</param>
  /// <param name="combinationsTested">The number of 3-card combinations in given <paramref name="deck"/>
  /// which have been checked for sets.</param>
  /// <param name="combinationsAreSets">The number of 3-card combinations in given <paramref name="deck"/>
  /// which have been found to be a "Set".</param>
  delegate void DeckEvaluatedAction(ReadOnlySpan<SetCard> deck, int combinationsTested, int combinationsAreSets);

  /// <summary>Gets or sets the callback to be invoked when a deck has been evaluated.</summary>
  /// <remarks>
  /// <para>The implementation must invoke the callback for every deck that it processes;
  /// it must also be invoked for decks with a number of cards different from the nominal deck size
  /// (for example, when the algorithm processes decks incrementally).</para>
  /// <para>The implementation must ensure that invocations are thread-safe.</para>
  /// <para>The implementation may cache the property value locally while running the algorithm;
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
  /// <returns>The determined result (see <see cref="IAlgorithm"/> summary)
  /// or <c>-1</c> when the algorithm couldn't find a definite answer.</returns>
  /// <exception cref="ArgumentException"><paramref name="deckSize"/> is less than 3 or greater than 81
  /// - OR - <paramref name="include"/> is not null/empty and contains invalid or duplicate elements
  /// - OR - <paramref name="include"/> contains more cards than <paramref name="deckSize"/>
  /// - OR - <paramref name="exclude"/> is not null/empty and contains invalid or duplicate elements
  /// - OR - <paramref name="exclude"/> contains elements which are also in <paramref name="include"/>.</exception>
  Task<long> RunAsync(
    int deckSize,
    SetCard[]? include,
    SetCard[]? exclude,
    CancellationToken cancellationToken = default);
}
