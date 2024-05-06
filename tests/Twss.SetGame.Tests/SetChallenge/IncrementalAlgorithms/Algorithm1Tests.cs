using Xunit.Abstractions;


namespace Twss.SetGame.SetChallenge.IncrementalAlgorithms;


public class Algorithm1Tests : AlgorithmTestsBase
{
  public Algorithm1Tests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override IAlgorithm CreateAlgorithm()
  {
    return new Algorithm1();
  }
}
