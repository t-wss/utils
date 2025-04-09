using System.Collections.Generic;
using System.Diagnostics;

using Twss.Utils.Collections;
using Xunit;


namespace Twss.SetGame;


public class SetCardTests
{
#pragma warning disable CS0618 // Member is obsolete.

  [Fact]
  public void CompareCheckIsSetImplementations()
  {
    IEnumerable<SetCard[]> combinations = SetCard.CardGame.EnumerateNChooseK(3);

    // Ensure that all implementations return the same results.
    foreach (SetCard[] combination in combinations)
    {
      bool isSet = SetCard.CheckIsSet(combination[0], combination[1], combination[2]);
      bool isSet2 = SetCard.CheckIsSet2(combination[0], combination[1], combination[2]);
      bool isSet3 = SetCard.CheckIsSetByProperties(combination[0], combination[1], combination[2]);
      Assert.Equal(isSet ,isSet2);
      Assert.Equal(isSet, isSet3);
    }

    // Compare performance.
    Stopwatch stopwatch = Stopwatch.StartNew();

    stopwatch.Restart();
    foreach (SetCard[] combination in combinations)
      SetCard.CheckIsSet(combination[0], combination[1], combination[2]);
    TestContext.Current.TestOutputHelper!.WriteLine($"Ticks for {nameof(SetCard.CheckIsSet)}(): {stopwatch.ElapsedTicks}");

    stopwatch.Restart();
    foreach (SetCard[] combination in combinations)
      SetCard.CheckIsSet2(combination[0], combination[1], combination[2]);
    TestContext.Current.TestOutputHelper!.WriteLine($"Ticks for {nameof(SetCard.CheckIsSet2)}(): {stopwatch.ElapsedTicks}");

    stopwatch.Restart();
    foreach (SetCard[] combination in combinations)
      SetCard.CheckIsSetByProperties(combination[0], combination[1], combination[2]);
    TestContext.Current.TestOutputHelper!.WriteLine($"Ticks for {nameof(SetCard.CheckIsSetByProperties)}(): {stopwatch.ElapsedTicks}");
  }

#pragma warning restore CS0618 // Member is obsolete.
}
