using System;
using System.Collections.Immutable;
using System.Runtime.Intrinsics;

using BenchmarkDotNet.Attributes;

using Twss.Utils.Collections;


namespace Twss.SetGame;


[MemoryDiagnoser]
public class SetMethodsCheckIsSetBenchmark
{
  private ImmutableArray<SetCard[]> _combinations;

  [GlobalSetup]
  public void GlobalSetup()
  {
    _combinations = SetCard.CardGame.EnumerateNChooseK(3).ToImmutableArray(); // Expect 1080 sets in combinations.

    Console.WriteLine($"Vector128 is {(Vector128.IsHardwareAccelerated ? "" : "NOT ")}hardware accelerated.");
    Console.WriteLine($"Vector256 is {(Vector256.IsHardwareAccelerated ? "" : "NOT ")}hardware accelerated.");
    Console.WriteLine($"Vector512 is {(Vector512.IsHardwareAccelerated ? "" : "NOT ")}hardware accelerated.");
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
  public int CheckIsSetBitOperations()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetBitOperations(combination[0], combination[1], combination[2]))
        result++;
     return result;
  }

  [Benchmark]
  public int CheckIsSetBitOperationsWithShift()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetBitOperationsWithShift(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSetVector128()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetVector128(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSetVector256()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetVector256(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }

  [Benchmark]
  public int CheckIsSetVector512()
  {
    int result = 0;
    foreach (SetCard[] combination in _combinations)
      if (SetMethods.CheckIsSetVector512(combination[0], combination[1], combination[2]))
        result++;
    return result;
  }
}
