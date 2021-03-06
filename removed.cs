        

        public Delegate[] GetPropertyChangedInvokers()
        {
            return PropertyChanged == null ? new Delegate[0] : PropertyChanged.GetInvocationList();
        }
        
        private class Searcher
        {
            public Searcher(Board board, Board target)
            {
                this.board = board;
                targetNumber = target.Number;
            }

            private readonly Board board;
            private readonly BigInteger targetNumber;
            private readonly Queue<BigInteger> sourceQueue = new Queue<BigInteger>(), targetQueue = new Queue<BigInteger>();
            private readonly Dictionary<BigInteger, Entry> dictionary = new Dictionary<BigInteger, Entry>();

            private IEnumerable<Int32Point> GetSolution(BigInteger sourceLast, BigInteger targetFirst)
            {
                var temp = new List<BigInteger> { sourceLast };
                var previous = dictionary[sourceLast].Previous;
                while (previous != BigInteger.MinusOne)
                {
                    temp.Add(previous);
                    previous = dictionary[previous].Previous;
                }
                for (var i = temp.Count - 2; i >= 0; i--) yield return new Board(board.Width, board.Height, temp[i]).EmptyPoint;
                yield return new Board(board.Width, board.Height, targetFirst).EmptyPoint;
                previous = dictionary[targetFirst].Previous;
                while (previous != BigInteger.MinusOne)
                {
                    yield return new Board(board.Width, board.Height, previous).EmptyPoint;
                    previous = dictionary[previous].Previous;
                }
            }

            public IEnumerable<Int32Point> Solve(bool bbfs = true)
            {
                if (board.Number == targetNumber) return new Int32Point[0];
                sourceQueue.Enqueue(board.Number);
                dictionary.Add(board.Number, new Entry(BigInteger.MinusOne, 1));
                targetQueue.Enqueue(targetNumber);
                dictionary.Add(targetNumber, new Entry(BigInteger.MinusOne, -1));
                while (sourceQueue.Count > 0 && targetQueue.Count > 0)
                {
                    var solution = bbfs && sourceQueue.Count <= targetQueue.Count ? Extend(sourceQueue) : Extend(targetQueue);
                    if (solution != null) return GetSolution(solution.Item1, solution.Item2);
                }
                throw new NoSolutionException();    // this exception should never be thrown
            }

            private Tuple<BigInteger, BigInteger> Extend(Queue<BigInteger> queue)
            {
                var number = queue.Dequeue();
                var previous = dictionary[number];
                var sign = Math.Sign(previous.Step);
                var current = new Board(board.Width, board.Height, number);
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
                        if (Math.Sign(entry.Step) == sign) continue;                            // the board has been reached before
                        return sign == 1 ? Tuple.Create(number, n) : Tuple.Create(n, number);   // else solution found! YAY!
                    }
                    queue.Enqueue(n);
                    dictionary.Add(n, new Entry(number, previous.Step + sign));
                }
                return null;
            }
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