using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using C5;

namespace Mygod.Puzzle
{
    public interface IBoardSolver
    {
        IEnumerable<Int32Point> GetSolution(BoardWrapper board);
    }

    public class NoSolutionException : Exception
    {
    }

    public class SearchSolver : IBoardSolver
    {
        private struct QueueEntry
        {
            public QueueEntry(int steps, BigInteger index, Searcher searcher)
            {
                Index = index;
                Priority = Math.Abs(steps);
                if (Math.Abs(Settings.Current.Optimization) < 1e-4) return;
                var distance = 0;
                var board = new Board(searcher.Board.Width, searcher.Board.Height, index);
                for (var i = 0; i < board.Size - 1; i++) distance += GetDistance(board.Mappings[i], searcher.TargetBoard.Mappings[i]);
                Priority += distance * Settings.Current.Optimization;
            }

            private static int GetDistance(Int32Point a, Int32Point b)
            {
                return a.X + a.Y - b.X - b.Y;
            }

            public readonly BigInteger Index;
            public readonly double Priority;
        }
        private struct DictionaryEntry
        {
            public DictionaryEntry(BigInteger previous, int steps)
            {
                Previous = previous;
                Steps = steps;
            }

            public readonly BigInteger Previous;
            public readonly int Steps;    // positive: source, negative: target
        }

        private class Searcher
        {
            private class PriorityComparer : IComparer<QueueEntry>
            {
                public int Compare(QueueEntry x, QueueEntry y)
                {
                    return x.Priority.CompareTo(y.Priority);
                }
            }

            public Searcher(Board board, Board target)
            {
                Board = board;
                TargetBoard = target;
                targetNumber = target.Number;
                var comparer = new PriorityComparer();
                sourceQueue = new IntervalHeap<QueueEntry>(comparer);
                targetQueue = new IntervalHeap<QueueEntry>(comparer);
            }

            public readonly Board Board, TargetBoard;
            private readonly BigInteger targetNumber;
            private readonly IntervalHeap<QueueEntry> sourceQueue, targetQueue;     // it's actually used as a priority queue
            private readonly Dictionary<BigInteger, DictionaryEntry> dictionary = new Dictionary<BigInteger, DictionaryEntry>();

            private IEnumerable<Int32Point> GetSolution(BigInteger sourceLast, BigInteger targetFirst)
            {
                var temp = new List<BigInteger> { sourceLast };
                var previous = dictionary[sourceLast].Previous;
                while (previous != BigInteger.MinusOne)
                {
                    temp.Add(previous);
                    previous = dictionary[previous].Previous;
                }
                for (var i = temp.Count - 2; i >= 0; i--) yield return new Board(Board.Width, Board.Height, temp[i]).EmptyPoint;
                yield return new Board(Board.Width, Board.Height, targetFirst).EmptyPoint;
                previous = dictionary[targetFirst].Previous;
                while (previous != BigInteger.MinusOne)
                {
                    yield return new Board(Board.Width, Board.Height, previous).EmptyPoint;
                    previous = dictionary[previous].Previous;
                }
            }

            public IEnumerable<Int32Point> Solve(bool bbfs = true)
            {
                if (Board.Number == targetNumber) return new Int32Point[0];
                sourceQueue.Add(new QueueEntry(1, Board.Number, this));
                dictionary.Add(Board.Number, new DictionaryEntry(BigInteger.MinusOne, 1));
                targetQueue.Add(new QueueEntry(-1, targetNumber, this));
                dictionary.Add(targetNumber, new DictionaryEntry(BigInteger.MinusOne, -1));
                while (sourceQueue.Count > 0 && targetQueue.Count > 0)
                {
                    var solution = bbfs && sourceQueue.Count <= targetQueue.Count ? Extend(sourceQueue) : Extend(targetQueue);
                    if (solution != null) return GetSolution(solution.Item1, solution.Item2);
                }
                throw new NoSolutionException();    // this exception should never be thrown
            }

            private Tuple<BigInteger, BigInteger> Extend(IntervalHeap<QueueEntry> queue)
            {
                var number = queue.DeleteMin().Index;
                var previous = dictionary[number];
                var sign = Math.Sign(previous.Steps);
                var newSteps = previous.Steps + sign;
                var current = new Board(Board.Width, Board.Height, number);
                for (var i = 1; i <= 4; i++)
                {
                    var direction = (Direction)i;
                    var point = current.GetPoint(direction);
                    if (!current.IsInRange(point)) continue;    // skip if it is out of range
                    var newBoard = new Board(current);
                    newBoard.Move(point);                       // emulate clicking the point
                    var n = newBoard.Number;
                    if (dictionary.ContainsKey(n))
                    {
                        var entry = dictionary[n];
                        if (Math.Sign(entry.Steps) == sign) continue;                            // the board has been reached before
                        return sign == 1 ? Tuple.Create(number, n) : Tuple.Create(n, number);   // else solution found! YAY!
                    }
                    queue.Add(new QueueEntry(newSteps, n, this));
                    dictionary.Add(n, new DictionaryEntry(number, newSteps));
                }
                return null;
            }
        }

        public IEnumerable<Int32Point> GetSolution(BoardWrapper board)
        {
            return new Searcher(board.Board, board.Target).Solve(Settings.Current.Bidirectional).ToList();
        }
    }
}
