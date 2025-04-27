using Xunit;


namespace Twss.SetGame.SetChallenge;


public class BasicAlgorithmTests : AlgorithmTestsBase
{
  public BasicAlgorithmTests(ITestOutputHelper output)
    : base(output)
  {
  }

  protected override ISetChallenge CreateAlgorithm()
  {
    return new BasicAlgorithm();
  }
}
