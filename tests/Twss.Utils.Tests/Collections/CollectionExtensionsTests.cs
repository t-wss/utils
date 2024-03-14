using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;


namespace Twss.Utils.Collections;


public class CollectionExtensionsTests
{
  // Use n = 4, k = 2 as example.
  [Fact]
  public void EnumerateNChooseK_4Choose2_ShouldReturnCorrespondingCombinations()
  {
    char[] source = { 'a', 'b', 'c', 'd' };
    List<char[]> expected = new()
    {
      new[] { 'a', 'b' },
      new[] { 'a', 'c' },
      new[] { 'a', 'd' },
      new[] { 'b', 'c' },
      new[] { 'b', 'd' },
      new[] { 'c', 'd' }
    };

    foreach (char[] combination in source.EnumerateNChooseK(2))
    {
      // Try to find in expected.
      int idx = expected.Count - 1;
      for (; idx >= 0; idx--)
      {
        char[] expectedCombination = expected[idx];
        if (combination.SequenceEqual(expectedCombination))
          break;
      }

      idx.Should().BeGreaterThanOrEqualTo(0);

      // Remove match from expected => next combination must be one of the remaining expected combinations.
      expected.RemoveAt(idx);
    }

    // All expected combinations found.
    expected.Count.Should().Be(0);
  }

  // Use n = 6 as example.
  [Fact]
  public void EnumerateNChooseK_ShouldReturnBinomialCoefficientNumberOfElements()
  {
    List<int> listOf6 = Enumerable.Range(0, 6).ToList();

    listOf6.EnumerateNChooseK(0).Count().Should().Be(1);
    listOf6.EnumerateNChooseK(1).Count().Should().Be(6 / 1);
    listOf6.EnumerateNChooseK(2).Count().Should().Be(6 * 5 / 2 / 1);
    listOf6.EnumerateNChooseK(3).Count().Should().Be(6 * 5 * 4 / 3 / 2 / 1);
    listOf6.EnumerateNChooseK(4).Count().Should().Be(6 * 5 * 4 * 3 / 4 / 3 / 2 / 1);
    listOf6.EnumerateNChooseK(5).Count().Should().Be(6 * 5 * 4 * 3 * 2 / 5 / 4 / 3 / 2 / 1);
    listOf6.EnumerateNChooseK(6).Count().Should().Be(1);
  }

  [Fact]
  public void EnumerateNChooseK_InvalidArgument_ThrowsArgumentException()
  {
    Action[] invalidInvocations = new[]
    {
      () => { CollectionExtensions.EnumerateNChooseK((int[])null!, 2, null).Count(); },
      () => { CollectionExtensions.EnumerateNChooseK((int[])null!, 2, new int[2]).Count(); },
      () => { CollectionExtensions.EnumerateNChooseK(new [] { 0, 1, 2 }, -1, null).Count(); },
      () => { CollectionExtensions.EnumerateNChooseK(new [] { 0, 1, 2 }, 4, null).Count(); },
      () => { CollectionExtensions.EnumerateNChooseK(new [] { 0, 1, 2 }, 1, new int[4]).Count(); },
    };

    foreach (var invalidInvocation in invalidInvocations)
      Assert.ThrowsAny<ArgumentException>(invalidInvocation);
  }
}
