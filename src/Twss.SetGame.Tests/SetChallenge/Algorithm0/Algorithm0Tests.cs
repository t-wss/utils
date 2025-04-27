using Xunit;


namespace Twss.SetGame.SetChallenge.Algorithm0;


public class Algorithm0Tests : AlgorithmTestsBase
{
  public Algorithm0Tests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override ISetChallenge CreateAlgorithm()
  {
    return new Algorithm0();
  }
}
