using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Mygod.Puzzle
{
    public interface IBoardSolver
    {
        IEnumerable<Int32Point> GetSolution(BoardWrapper board);
    }

    public class NoSolutionException : Exception
    {
    }

    public class UnremovableQueue<TKey, TValue>
    {
        private readonly List<KeyValuePair<TKey, TValue>> queue = new List<KeyValuePair<TKey, TValue>>();
        private readonly Dictionary<TKey, int> queuePointer = new Dictionary<TKey, int>();
        private int head;

        public int Count { get { return queue.Count - head; } }

        public bool Contains(TKey key)
        {
            return queuePointer.ContainsKey(key);
        }
        public int GetIndex(TKey key)
        {
            return Contains(key) ? queuePointer[key] : -1;
        }
        public KeyValuePair<TKey, TValue> GetPair(int index)
        {
            return queue[index];
        }
        public void Enqueue(TKey key, TValue value)
        {
            if (Contains(key)) throw new InvalidOperationException("试图重复将同一节点插入队列。");
            queuePointer.Add(key, queue.Count);
            queue.Add(new KeyValuePair<TKey, TValue>(key, value));
        }
        public KeyValuePair<TKey, TValue> Dequeue()
        {
            if (head == queue.Count) throw new InvalidOperationException("队列已空！");
            return queue[head++];
        }
    }

    public class BidirectionalBreadthFirstSearchSolver : IBoardSolver
    {
        private class Solver
        {
            public Solver(Board b, Board target)
            {
                board = b;
                targetNumber = target.Number;
            }

            private readonly Board board;
            private readonly BigInteger targetNumber;
            private readonly UnremovableQueue<BigInteger, Direction> sourceQueue = new UnremovableQueue<BigInteger, Direction>(),
                                                                     targetQueue = new UnremovableQueue<BigInteger, Direction>();

            public IEnumerable<Int32Point> Solve()
            {
                if (board.Number == targetNumber) yield break;
                sourceQueue.Enqueue(board.Number, Direction.None);
                targetQueue.Enqueue(targetNumber, Direction.None);
                while (sourceQueue.Count > 0 && targetQueue.Count > 0)
                {
                    var solution = sourceQueue.Count <= targetQueue.Count ? Extend(sourceQueue, targetQueue)
                                                                          : Extend(targetQueue, sourceQueue);
                    if (solution < 0) continue;
                    var result = new LinkedList<Direction>();
                    var copy = new Board(board.Width, board.Height, solution);
                    var pair = sourceQueue.GetPair(sourceQueue.GetIndex(solution));
                    while (pair.Value != Direction.None)
                    {
                        result.AddFirst(pair.Value);
                        copy.Move(copy.GetPoint(Board.Reverse(pair.Value)));
                        pair = sourceQueue.GetPair(sourceQueue.GetIndex(copy.Number));
                    }
                    copy = new Board(board.Width, board.Height, solution);
                    pair = sourceQueue.GetPair(targetQueue.GetIndex(solution));
                    while (pair.Value != Direction.None)
                    {
                        var reversed = Board.Reverse(pair.Value);
                        result.AddLast(reversed);
                        copy.Move(copy.GetPoint(reversed));
                        pair = targetQueue.GetPair(targetQueue.GetIndex(copy.Number));
                    }
                    copy = new Board(board);
                    foreach (var direction in result)
                    {
                        Int32Point p;
                        yield return p = copy.GetPoint(direction);
                        copy.Move(p);
                    }
                }
                throw new NoSolutionException();
            }

            private BigInteger Extend(UnremovableQueue<BigInteger, Direction> queueToExtend,
                                      UnremovableQueue<BigInteger, Direction> queueOther)
            {
                var number = queueToExtend.Dequeue();
                var current = new Board(board.Width, board.Height, number.Key);
                var except = Board.Reverse(number.Value);
                for (var i = 1; i <= 4; i++)
                {
                    var direction = (Direction) i;
                    if (direction == except) continue;
                    var point = current.GetPoint(direction);
                    if (!current.IsInRange(point)) continue;
                    var newBoard = new Board(current);
                    newBoard.Move(point);
                    var n = newBoard.Number;
                    if (queueToExtend.Contains(n)) continue;
                    queueToExtend.Enqueue(n, direction);
                    if (queueOther.Contains(n)) return n;   // solution found! YAY!
                }
                return BigInteger.MinusOne;
            }
        }

        public IEnumerable<Int32Point> GetSolution(BoardWrapper board)
        {
            return new Solver(board.Board, board.Target).Solve().ToList();
        }
    }

    public class AStarSearchSolver : IBoardSolver
    {
        public IEnumerable<Int32Point> GetSolution(BoardWrapper board)
        {
            throw new NotImplementedException();
        }
    }
}
