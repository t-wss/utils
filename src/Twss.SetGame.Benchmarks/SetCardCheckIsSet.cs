using System.Collections.Immutable;

using BenchmarkDotNet.Attributes;

using Twss.Utils.Collections;


namespace Twss.SetGame.Benchmarks;


[MemoryDiagnoser]
public class SetCardCheckIsSet
{
  private ImmutableArray<SetCard[]> _permutations;

  [GlobalSetup]
  public void GlobalSetup()
  {
    _permutations = SetCard.CardGame.EnumerateNChooseK(3).ToImmutableArray();
  }

  [Benchmark(Baseline = true)]
  public int CheckIsSetByProperties()
  {
    int result = 0;
    foreach (SetCard[] permutation in _permutations)
      if (SetCard.CheckIsSetByProperties(permutation[0], permutation[1], permutation[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSet1()
  {
    int result = 0;
    foreach (SetCard[] permutation in _permutations)
      if (SetCard.CheckIsSet(permutation[0], permutation[1], permutation[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSet2()
  {
    int result = 0;
    foreach (SetCard[] permutation in _permutations)
      if (SetCard.CheckIsSet2(permutation[0], permutation[1], permutation[2]))
        result++;
    return result;
  }
}
