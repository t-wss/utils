using System.Collections.Generic;
using System.Threading.Tasks;


namespace Twss.SetGame.SetChallenge.Algorithm2;


/// <summary>Captures all data resp. other objects needed to evaluate decks.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>The class is intended to have one thread evaluating decks
/// independently from other threads/<see cref="RunContext"/>s.
/// <see cref="Algorithm2"/> uses one <see cref="RunContext"/> object per worker thread.</item>
/// <item><see cref="RunContext"/> is initialized with the nominal deck size and available cards
/// to have these parameters easily available to the executing thread.</item>
/// <item><see cref="RunContext"/> contains a collection of <see cref="Decks">decks</see> and
/// <see cref="DecksEvaluated">evaluated decks</see>. Contained decks may have a deck size
/// up to <see cref="NominalDeckSize"/>.</item>
/// <item>The property <see cref="RunTask"/> is intended to 'carry' the task of an asynchronous evaluation;
/// it is used for synchronization of evaluation and cleanup.</item>
/// </list>
/// </remarks>
internal sealed class RunContext
{
  internal RunContext(int nominalDeckSize, SetCard[] packOfCards)
  {
    NominalDeckSize = nominalDeckSize;
    PackOfCards = packOfCards;
  }

  internal List<Deck> Decks { get; } = new();

  internal List<Deck> DecksEvaluated { get; } = new();

  internal int NominalDeckSize { get; }

  internal SetCard[] PackOfCards { get; }

  internal Task? RunTask { get; set; }
}
