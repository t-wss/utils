using System.Collections.Immutable;

using BenchmarkDotNet.Attributes;

using Twss.Utils.Collections;


namespace Twss.SetGame.Benchmarks;


[MemoryDiagnoser]
public class SetMethodsCheckIsSet
{
  private ImmutableArray<SetCard[]> _combinations;

  [GlobalSetup]
  public void GlobalSetup()
  {
    _combinations = SetCard.CardGame.EnumerateNChooseK(3).ToImmutableArray();
  }

  [Benchmark(Baseline = true)]
  public int CheckIsSetDefault()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetDefault(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSetBitOperations1()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetBitOperations1(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSetBitOperations2()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetBitOperations2(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }
}
