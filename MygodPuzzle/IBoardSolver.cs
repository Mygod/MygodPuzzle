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

    public class CommonSolver : IBoardSolver    // BBBBBBBBBBBBBBUUUUUUUUUUUUUUUUUUGGGGGGGGGGGGGGGGGGGG!!!!!!!!!!!!!!!!!!!!!!!!!
    {
        private class Solver
        {
            public Solver(Board board, Board target)
            {
                this.board = board;
                this.target = target;
                right = board.Width - 1;
                down = board.Height - 1;
            }

            private readonly Board board, target;
            private int left, right, up, down;

            public IEnumerable<Int32Point> Solve()
            {
                var lastEmptyPoint = target.Mappings[target.Size - 1];
                if (lastEmptyPoint.X == 0) foreach (var point in SolveRight(board.Width - 2)) yield return point;
                else
                {
                    foreach (var point in SolveRight(board.Width - lastEmptyPoint.X - 1)) yield return point;
                    foreach (var point in SolveLeft(lastEmptyPoint.X - 1)) yield return point;
                }
                if (lastEmptyPoint.Y == 0) foreach (var point in SolveDown(board.Height - 2)) yield return point;
                else
                {
                    foreach (var point in SolveDown(board.Height - lastEmptyPoint.Y - 1)) yield return point;
                    foreach (var point in SolveUp(lastEmptyPoint.Y - 1)) yield return point;
                }
                foreach (var point in SpinLast()) yield return point;
            }

            private IEnumerable<Int32Point> SolveUp(int times = 1)
            {
                for (var i = 0; i < times; i++)
                {
                    var y = up++;
                    for (var x = left; x < right - 1; x++)
                    {
                        var src = board.Mappings[target[x, y]];
                        foreach (var point in MoveXDown(src.X, src.Y, x)) yield return point;
                        foreach (var point in MoveYRight(x, src.Y, y)) yield return point;
                    }
                    var last1 = board.Mappings[target[right, y]];
                    foreach (var point in MoveYRight(last1.X, last1.Y, up)) yield return point;
                    foreach (var point in MoveXDown(up, last1.Y, right - 2)) yield return point;
                    var last2 = board.Mappings[target[right - 1, y]];
                    if (last2.X < right - 1)
                    {
                        foreach (var point in MoveYLeft(last2.X, last2.Y, up + 1)) yield return point;
                        last2 = new Int32Point(last2.X, up + 1);
                    }
                    foreach (var point in MoveXDown(last2.X, last2.Y, right)) yield return point;
                    foreach (var point in MoveYRight(right, last2.Y, y)) yield return point;
                    foreach (var point in MoveYRight(up, right - 2, right)) yield return point;  // move last1
                    yield return Move(right - 1, y);
                    yield return Move(right, y);
                    yield return Move(right, up);
                }
            }
            private IEnumerable<Int32Point> SolveDown(int times = 1)
            {
                for (var i = 0; i < times; i++)
                {
                    var y = down--;
                    for (var x = left; x < right - 1; x++)
                    {
                        var src = board.Mappings[target[x, y]];
                        foreach (var point in MoveXUp(src.X, src.Y, x)) yield return point;
                        foreach (var point in MoveYRight(x, src.Y, y)) yield return point;
                    }
                    var last1 = board.Mappings[target[right, y]];
                    foreach (var point in MoveYRight(last1.X, last1.Y, down)) yield return point;
                    foreach (var point in MoveXUp(down, last1.Y, right - 2)) yield return point;
                    var last2 = board.Mappings[target[right - 1, y]];
                    if (last2.X < right - 1)
                    {
                        foreach (var point in MoveYLeft(last2.X, last2.Y, down - 1)) yield return point;
                        last2 = new Int32Point(last2.X, down - 1);
                    }
                    foreach (var point in MoveXUp(last2.X, last2.Y, right)) yield return point;
                    foreach (var point in MoveYRight(right, last2.Y, y)) yield return point;
                    foreach (var point in MoveYRight(down, right - 2, right)) yield return point;  // move last1
                    yield return Move(right - 1, y);
                    yield return Move(right, y);
                    yield return Move(right, down);
                }
            }
            private IEnumerable<Int32Point> SolveLeft(int times = 1)
            {
                for (var i = 0; i < times; i++)
                {
                    var x = left++;
                    for (var y = up; y < down - 1; y++)
                    {
                        var src = board.Mappings[target[x, y]];
                        foreach (var point in MoveYRight(src.X, src.Y, y)) yield return point;
                        foreach (var point in MoveXDown(src.X, y, x)) yield return point;
                    }
                    var last1 = board.Mappings[target[x, down]];
                    foreach (var point in MoveXDown(last1.X, last1.Y, left)) yield return point;
                    foreach (var point in MoveYRight(left, last1.Y, down - 2)) yield return point;
                    var last2 = board.Mappings[target[x, down - 1]];
                    if (last2.Y < down - 1)
                    {
                        foreach (var point in MoveXUp(last2.X, last2.Y, left + 1)) yield return point;
                        last2 = new Int32Point(left + 1, last2.Y);
                    }
                    foreach (var point in MoveYRight(last2.X, last2.Y, down)) yield return point;
                    foreach (var point in MoveXDown(last2.X, down, x)) yield return point;
                    foreach (var point in MoveXDown(left, down - 2, down)) yield return point;  // move last1
                    yield return Move(x, down - 1);
                    yield return Move(x, down);
                    yield return Move(left, down);
                }
            }
            private IEnumerable<Int32Point> SolveRight(int times = 1)
            {
                for (var i = 0; i < times; i++)
                {
                    var x = right--;
                    for (var y = up; y < down - 1; y++)
                    {
                        var src = board.Mappings[target[x, y]];
                        foreach (var point in MoveYLeft(src.X, src.Y, y)) yield return point;
                        foreach (var point in MoveXDown(src.X, y, x)) yield return point;
                    }
                    var last1 = board.Mappings[target[x, down]];
                    foreach (var point in MoveXDown(last1.X, last1.Y, right)) yield return point;
                    foreach (var point in MoveYRight(right, last1.Y, down - 2)) yield return point;
                    var last2 = board.Mappings[target[x, down - 1]];
                    if (last2.Y < down - 1)
                    {
                        foreach (var point in MoveXUp(last2.X, last2.Y, right - 1)) yield return point;
                        last2 = new Int32Point(right - 1, last2.Y);
                    }
                    foreach (var point in MoveYRight(last2.X, last2.Y, down)) yield return point;
                    foreach (var point in MoveXDown(last2.X, down, x)) yield return point;
                    foreach (var point in MoveXDown(right, down - 2, down)) yield return point;  // move last1
                    yield return Move(x, down - 1);
                    yield return Move(x, down);
                    yield return Move(right, down);
                }
            }

            private IEnumerable<Int32Point> MoveXUp(int pointX, int pointY, int x)
            {
                return pointX > x
                           ? pointY == down ? SpinCcw(pointX, pointY, x, pointY - 1) : SpinCw(pointX, pointY, x, pointY + 1)
                           : pointY == down ? SpinCw(pointX, pointY, x, pointY - 1) : SpinCcw(pointX, pointY, x, pointY + 1);
            }
            private IEnumerable<Int32Point> MoveXDown(int pointX, int pointY, int x)
            {
                return pointX > x
                           ? pointY == up ? SpinCw(pointX, pointY, x, pointY + 1) : SpinCcw(pointX, pointY, x, pointY - 1)
                           : pointY == up ? SpinCcw(pointX, pointY, x, pointY + 1) : SpinCw(pointX, pointY, x, pointY - 1);
            }
            private IEnumerable<Int32Point> MoveYLeft(int pointX, int pointY, int y)
            {
                return pointY > y
                           ? pointX == right ? SpinCcw(pointX, pointY, pointX - 1, y) : SpinCw(pointX, pointY, pointX + 1, y)
                           : pointX == right ? SpinCw(pointX, pointY, pointX - 1, y) : SpinCcw(pointX, pointY, pointX + 1, y);
            }
            private IEnumerable<Int32Point> MoveYRight(int pointX, int pointY, int y)
            {
                return pointY > y
                           ? pointX == left ? SpinCw(pointX, pointY, pointX + 1, y) : SpinCcw(pointX, pointY, pointX - 1, y)
                           : pointX == left ? SpinCcw(pointX, pointY, pointX + 1, y) : SpinCw(pointX, pointY, pointX - 1, y);
            }

            private IEnumerable<Int32Point> SpinCcw(int ax, int ay, int bx, int by)
            {
                if (ax == bx || ay == by) yield break;
                foreach (var point in GoToRect(ax, bx, by)) yield return point;
                if (ax < bx) if (ay < by)
                        for (var y = ay; y < by; y++)
                        {
                            yield return Move(ax, by);
                            yield return Move(ax, y);
                            if (y == by - 1) continue;
                            yield return Move(bx, y);
                            yield return Move(bx, by);
                        }
                    else for (var y = ay; y > by; y--)
                        {
                            yield return Move(bx, y);
                            yield return Move(ax, y);
                            if (y == by + 1) continue;
                            yield return Move(ax, by);
                            yield return Move(bx, by);
                        }
                else if (ay < by)
                    for (var y = ay; y < by; y++)
                    {
                        yield return Move(bx, y);
                        yield return Move(ax, y);
                        if (y == by - 1) continue;
                        yield return Move(ax, by);
                        yield return Move(bx, by);
                    }
                else for (var y = ay; y > by; y--)
                    {
                        yield return Move(ax, by);
                        yield return Move(ax, y);
                        if (y == by + 1) continue;
                        yield return Move(bx, y);
                        yield return Move(bx, by);
                    }
            }
            private IEnumerable<Int32Point> SpinCw(int ax, int ay, int bx, int by)
            {
                if (ax == bx || ay == by) yield break;
                foreach (var point in GoToRect(ax, bx, by)) yield return point;
                if (ax < bx) if (ay < by)
                        for (var y = ay; y < by; y++)
                        {
                            yield return Move(bx, y);
                            yield return Move(ax, y);
                            if (y == by - 1) continue;
                            yield return Move(ax, by);
                            yield return Move(bx, by);
                        }
                    else for (var y = ay; y > by; y--)
                        {
                            yield return Move(ax, by);
                            yield return Move(ax, y);
                            if (y == by + 1) continue;
                            yield return Move(bx, y);
                            yield return Move(bx, by);
                        }
                else if (ay < by)
                    for (var y = ay; y < by; y++)
                    {
                        yield return Move(ax, by);
                        yield return Move(ax, y);
                        if (y == by - 1) continue;
                        yield return Move(bx, y);
                        yield return Move(bx, by);
                    }
                else for (var y = ay; y > by; y--)
                    {
                        yield return Move(bx, y);
                        yield return Move(ax, y);
                        if (y == by + 1) continue;
                        yield return Move(ax, by);
                        yield return Move(bx, by);
                    }
            }
            private IEnumerable<Int32Point> SpinLast()
            {
                for (var i = 0; i < 4; i++)
                {
                    yield return Move(left, up);
                    if (board.Number == target.Number) yield break;
                    yield return Move(right, up);
                    if (board.Number == target.Number) yield break;
                    yield return Move(right, down);
                    if (board.Number == target.Number) yield break;
                    yield return Move(left, down);
                    if (board.Number == target.Number) yield break;
                }
            }

            private IEnumerable<Int32Point> GoToRect(int ax, int bx, int by)
            {
                var emptyPoint = board.EmptyPoint;
                if (emptyPoint.X == ax) yield return Move(emptyPoint.X, by);
                else yield return Move(bx, emptyPoint.Y);
                yield return Move(bx, by);
            }
            private Int32Point Move(int x, int y)
            {
                var pos = new Int32Point(x, y);
                board.Move(pos);
                return pos;
            }
        }

        public IEnumerable<Int32Point> GetSolution(BoardWrapper board)
        {
            return new Solver(board.Board, board.Target).Solve().ToList();
        }
    }
}
