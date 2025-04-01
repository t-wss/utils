using Xunit.Abstractions;


namespace Twss.SetGame.SetChallenge;


public class BasicAlgorithmTests : AlgorithmTestsBase
{
  public BasicAlgorithmTests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override IAlgorithm CreateAlgorithm()
  {
    return new BasicAlgorithm();
  }
}
