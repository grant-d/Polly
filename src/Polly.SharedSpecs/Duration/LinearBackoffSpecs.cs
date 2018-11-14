﻿using FluentAssertions;
using Polly.Duration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Polly.Specs.Duration
{
    public static class LinearBackoffSpecs
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Should_have_identical_sequence_when_same_factor(bool fastFirst)
        {
            const int count = 1000;
            TimeSpan minDelay = TimeSpan.FromMilliseconds(10);

            LinearBackoff backoff1 = new LinearBackoff(minDelay, 1.0, fastFirst);
            LinearBackoff backoff2 = new LinearBackoff(minDelay, 1.0, fastFirst);
            IReadOnlyList<TimeSpan> discrete1 = backoff1.Discrete(count);
            IReadOnlyList<TimeSpan> discrete2 = backoff2.Discrete(count);

            discrete1.Should().HaveCount(count);
            discrete2.Should().HaveCount(count);

            discrete1.Should().ContainInOrder(discrete2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Should_have_different_sequence_when_different_factor(bool fastFirst)
        {
            const int count = 100;
            TimeSpan minDelay = TimeSpan.FromMilliseconds(10);

            LinearBackoff backoff1 = new LinearBackoff(minDelay, 1.0, fastFirst);
            LinearBackoff backoff2 = new LinearBackoff(minDelay, 2.0, fastFirst);
            IReadOnlyList<TimeSpan> discrete1 = backoff1.Discrete(count);
            IReadOnlyList<TimeSpan> discrete2 = backoff2.Discrete(count);

            discrete1.Should().HaveCount(count);
            discrete2.Should().HaveCount(count);

            if (fastFirst)
            {
                discrete1[0].Should().Be(discrete2[0]);
                discrete1[1].Should().Be(discrete2[1]);
            }
            else
            {
                discrete1[0].Should().Be(discrete2[0]);
            }

            discrete1[2].Should().NotBe(discrete2[2]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Should_have_an_adequate_variance_when_range_small(bool fastFirst)
        {
            const int count = 1000;
            TimeSpan minDelay = TimeSpan.FromMilliseconds(10);

            LinearBackoff backoff = new LinearBackoff(minDelay, 1.0, fastFirst);
            IEnumerable<TimeSpan> discrete = backoff.Discrete(count);

            discrete.Should().HaveCount(count);

            int expectedCount = count;
            if (fastFirst)
            {
                discrete.First().Should().Be(TimeSpan.Zero);
                discrete = discrete.Skip(1);
                expectedCount--;
            }

            discrete.Should().Contain(n => n >= minDelay);

            int groupCount = discrete
                .Select(n => n.TotalMilliseconds)
                .GroupBy(n => n)
                .Count();

            groupCount.Should().Be(expectedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Should_have_an_adequate_variance_when_range_large(bool fastFirst)
        {
            const int count = 1000;
            TimeSpan minDelay = TimeSpan.FromMilliseconds(10);

            LinearBackoff backoff = new LinearBackoff(minDelay, 1.0, fastFirst);
            IEnumerable<TimeSpan> discrete = backoff.Discrete(count);

            discrete.Should().HaveCount(count);

            int expectedCount = count;
            if (fastFirst)
            {
                discrete.First().Should().Be(TimeSpan.Zero);
                discrete = discrete.Skip(1);
                expectedCount--;
            }

            discrete.Should().Contain(n => n >= minDelay);

            int groupCount = discrete
                .Select(n => n.TotalMilliseconds)
                .GroupBy(n => n)
                .Count();

            groupCount.Should().Be(expectedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Should_have_no_variance_when_range_zero(bool fastFirst)
        {
            const int count = 1000;
            TimeSpan minDelay = TimeSpan.FromMilliseconds(0);

            LinearBackoff backoff = new LinearBackoff(minDelay, 1.0, fastFirst);
            IEnumerable<TimeSpan> discrete = backoff.Discrete(count);

            discrete.Should().HaveCount(count);

            if (fastFirst)
            {
                discrete.First().Should().Be(TimeSpan.Zero);
                discrete = discrete.Skip(1);
            }

            discrete.Should().Contain(n => n == minDelay);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Should_be_empty_when_count_zero(bool fastFirst)
        {
            const int count = 0;
            TimeSpan minDelay = TimeSpan.FromMilliseconds(10);

            LinearBackoff backoff = new LinearBackoff(minDelay, 1.0, fastFirst);
            IEnumerable<TimeSpan> discrete = backoff.Discrete(count);

            discrete.Should().BeEmpty();
        }

        [Fact]
        public static void Should_be_able_to_calculate_retry_timespans()
        {
            LinearBackoff durationStrategy = new LinearBackoff(TimeSpan.FromSeconds(1), 2, false);

            // Discrete

            TimeSpan[] expectedDiscrete = new TimeSpan[5]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(7),
                TimeSpan.FromSeconds(9)
            };

            IReadOnlyList<TimeSpan> actualDurations = durationStrategy.Discrete(5);
            actualDurations.Should().ContainInOrder(expectedDiscrete);

            // Take

            TimeSpan[] expectedContinuous = new TimeSpan[7]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(7),
                TimeSpan.FromSeconds(9),

                TimeSpan.FromSeconds(9),
                TimeSpan.FromSeconds(9)
            };

            actualDurations = durationStrategy.Continuous(5).Take(7).ToArray();
            actualDurations.Should().ContainInOrder(expectedContinuous);
        }

        [Fact]
        public static void Should_be_able_to_calculate_retry_timespans_fastfirst()
        {
            LinearBackoff durationStrategy = new LinearBackoff(TimeSpan.FromSeconds(1), 2, true);

            // Discrete

            TimeSpan[] expectedDurations = new TimeSpan[5]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(7)
            };

            IReadOnlyList<TimeSpan> actualDurations = durationStrategy.Discrete(5);
            actualDurations.Should().ContainInOrder(expectedDurations);

            // Take

            TimeSpan[] expectedContinuous = new TimeSpan[7]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(7),

                TimeSpan.FromSeconds(7),
                TimeSpan.FromSeconds(7)
            };

            actualDurations = durationStrategy.Continuous(5).Take(7).ToArray();
            actualDurations.Should().ContainInOrder(expectedContinuous);
        }
    }
}
