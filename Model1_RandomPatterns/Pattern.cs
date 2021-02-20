using System;
using System.Collections.Generic;
using System.Text;

namespace Model1_RandomPatterns
{
    internal enum PatternDefinition
    {
        // Require 0
        Zero,
        // Require 1
        One,
        // Anything
        Any,
    }
    internal enum PatternBehavior
    {
        // Assert 0
        Zero,
        // Assert 1
        One,
        // Toggle,
        Toggle
    }
    internal class Pattern
    {
        public int ID { get; set; }
        public int Size { get; set; }
        public PatternDefinition[,,] Match { get; set; }
        public PatternBehavior Behavior { get; set; }

        public Pattern(int id, int size)
        {
            ID = id;
            Size = size;
        }
        public override string ToString()
            => $"<{ID}> ({Size}) {Behavior}: {string.Join(", ", Match.ToEnumerable<PatternDefinition>())}";
    }

    public static class ArrayExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this Array target)
        {
            foreach (var item in target)
                yield return (T)item;
        }
    }
}