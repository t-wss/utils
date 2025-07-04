using System;
using System.Linq;

using Xunit;


namespace Twss.SetGame.SetChallenge.Algorithm2;


public class Algorithm2Tests : AlgorithmTestsBase
{
  public Algorithm2Tests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override ISetChallenge CreateAlgorithm()
  {
    return new Algorithm2();
  }

  [Fact]
  public void CreateInitialDecks_ShouldCreateValid()
  {
    SetCard[] packOfCards = SetCard.CardGame.ToArray();
    SetCard[] include = [ SetCard.CardGame[30], SetCard.CardGame[5] ];
    SetCard[] exclude = [ SetCard.CardGame[42], SetCard.CardGame[17] ];

    (SetCard[] packOfCardsEffective, Deck[] initialDecks) = Algorithm2.CreateInitialDecks(packOfCards, include, exclude);

    Assert.True(SetMethods.CheckDeckValid(packOfCardsEffective));
    Assert.All(initialDecks, deckObj =>
    {
      SetCard[] deck = deckObj.ToArray();
      Assert.True(SetMethods.CheckDeckValid(deck));
      Assert.Equal(include, deck.Take(include.Length));
      Assert.Equal(deck.Skip(include.Length).OrderBy(card => card.Index), deck.Skip(include.Length));
    });
  }
}
