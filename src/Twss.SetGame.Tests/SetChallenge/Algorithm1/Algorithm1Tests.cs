using Xunit;


namespace Twss.SetGame.SetChallenge.Algorithm1;


public class Algorithm1Tests : AlgorithmTestsBase
{
  public Algorithm1Tests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override ISetChallenge CreateAlgorithm()
  {
    return new Algorithm1();
  }
}
