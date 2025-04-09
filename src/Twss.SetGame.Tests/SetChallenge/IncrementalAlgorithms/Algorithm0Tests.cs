using Xunit;


namespace Twss.SetGame.SetChallenge.IncrementalAlgorithms;


public class Algorithm0Tests : AlgorithmTestsBase
{
  public Algorithm0Tests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override IAlgorithm CreateAlgorithm()
  {
    return new Algorithm0();
  }
}
