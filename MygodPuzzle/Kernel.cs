using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mygod.Puzzle.Annotations;
using Expression = System.Linq.Expressions.Expression;

namespace Mygod.Puzzle
{
    public sealed class Board
    {
        public Board(int width, int height, BigInteger number)
        {
            if (width <= 1 || height <= 1) throw new ArgumentException();
            Width = width;
            Height = height;
            Data = new int[width, height];
            Mappings = new Int32Point[Size];
            if (number < 0) return;
            var inversion = new BigInteger[Size];
            for (var i = 0; i < Size; i++)
            {
                inversion[i] = number % (i + 1);
                number /= i + 1;
            }
            for (var i = 0; i < Size; i++)
            {
                int j;
                for (j = Size - 1; j >= 0; j--) if (inversion[j] == 0) break;
                this[Size - 1 - j] = i;
                for (; j < Size; j++) inversion[j]--;
            }
        }
        public Board(int width, int height) : this(width, height, 0)
        {
        }
        public Board(Board copy) : this(copy.Width, copy.Height, -1)
        {
            Array.Copy(copy.Data, Data, Size);
            Array.Copy(copy.Mappings, Mappings, Size);
        }

        public readonly int Width, Height;
        public readonly int[,] Data;
        public readonly Int32Point[] Mappings;

        public int this[int x, int y]
        {
            get { return Data[x, y]; }
            set
            {
                Data[x, y] = value;
                if (Data[x, y] >= 0) Mappings[value] = new Int32Point(x, y);
                number = BigInteger.MinusOne;
            }
        }
        public int this[int key]
        {
            get { var point = GetPoint(key); return this[point.X, point.Y]; }
            set { var point = GetPoint(key); this[point.X, point.Y] = value; }
        }

        public Int32Point EmptyPoint { get { return Mappings[Size - 1]; } }

        private int size = -1;
        public int Size { get { if (size < 0) size = Width * Height; return size; } }
        private int oddHash = -1;
        public int OddHash
        {
            get
            {
                if (oddHash < 0)
                {
                    var tree = new FenwickTree<int>(Size - 1);
                    oddHash = 0;
                    for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++) if (this[x, y] != Size - 1)
                    {
                        oddHash += tree.GetSum(this[x, y]);
                        tree.Update(this[x, y], 1);
                    }
                    oddHash &= 1;
                }
                return oddHash;
            }
        }
        private BigInteger number = BigInteger.MinusOne;
        public BigInteger Number
        {
            get
            {
                if (number < 0)
                {
                    number = 0;
                    var s1 = Size - 1;
                    for (var i = 0; i < s1; i++)
                    {
                        for (var j = i + 1; j < Size; j++) if (this[i] > this[j]) number++;
                        number *= s1 - i;
                    }
                }
                return number;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as BoardWrapper);
        }

        private bool Equals(Board board)
        {
            if (ReferenceEquals(null, board)) return false;
            if (board.Width != Width || board.Height != Height) return false;
            for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++) if (board[x, y] != this[x, y]) return false;
            return true;
        }

        private const int HashKey = 0xABCDEF;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0xA3E503E;   // <= this is sort of AWESOME, heh HEH!
                hashCode = (hashCode * HashKey) ^ Width;
                hashCode = (hashCode * HashKey) ^ Height;
                if (Data != null) for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++)
                            hashCode = (hashCode * HashKey) ^ Data[x, y];
                return hashCode;
            }
        }

        public static bool operator ==(Board a, Board b)
        {
            return ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
        }

        public static bool operator !=(Board a, Board b)
        {
            return !(a == b);
        }

        public int GetKey(int x, int y)
        {
            return y * Width + x;
        }

        public int GetKey(Int32Point point)
        {
            return GetKey(point.X, point.Y);
        }

        public Int32Point GetPoint(int key)
        {
            return new Int32Point(key % Width, key / Width);
        }

        public void RandomGenerate()
        {
            var set = new List<int>();
            for (var i = 0; i < Size - 1; i++) set.Add(i);
            var random = new Random();
            for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++)
                if (set.Count == 0) this[x, y] = Size - 1;
                else
                {
                    var i = random.Next(set.Count);
                    this[x, y] = set[i];
                    set.RemoveAt(i);
                }
            FlushOddHash();
        }

        public void FlushOddHash()
        {
            oddHash = -1;
        }

        public IEnumerable<int> PeekMove(Int32Point position)
        {
            if (position.X < 0 || position.Y < 0 || position.X >= Width || position.Y >= Height) yield break;
            var emptyPoint = EmptyPoint;
            if (position == emptyPoint || (position.X != emptyPoint.X && position.Y != emptyPoint.Y)) yield break;
            if (position.X == emptyPoint.X)
            {
                for (var y = emptyPoint.Y; y < position.Y; y++) yield return this[position.X, y + 1];
                for (var y = emptyPoint.Y; y > position.Y; y--) yield return this[position.X, y - 1];
            }
            else
            {
                for (var x = emptyPoint.X; x < position.X; x++) yield return this[x + 1, position.Y];
                for (var x = emptyPoint.X; x > position.X; x--) yield return this[x - 1, position.Y];
            }
        }

        public IEnumerable<int> TryMove(Int32Point position)
        {
            if (!IsInRange(position)) yield break;
            var emptyPoint = EmptyPoint;
            if (position == emptyPoint || (position.X != emptyPoint.X && position.Y != emptyPoint.Y)) yield break;
            if (position.X == emptyPoint.X)
            {
                for (var y = emptyPoint.Y; y < position.Y; y++)
                {
                    yield return this[position.X, y + 1];
                    this[position.X, y] = this[position.X, y + 1];
                }
                for (var y = emptyPoint.Y; y > position.Y; y--)
                {
                    yield return this[position.X, y - 1];
                    this[position.X, y] = this[position.X, y - 1];
                }
            }
            else
            {
                for (var x = emptyPoint.X; x < position.X; x++)
                {
                    yield return this[x + 1, position.Y];
                    this[x, position.Y] = this[x + 1, position.Y];
                }
                for (var x = emptyPoint.X; x > position.X; x--)
                {
                    yield return this[x - 1, position.Y];
                    this[x, position.Y] = this[x - 1, position.Y];
                }
            }
            this[position.X, position.Y] = Size - 1;
        }

        public void Move(Int32Point position)
        {
            if (!IsInRange(position)) return;
            var emptyPoint = EmptyPoint;
            if (position.X == emptyPoint.X)
            {
                if (position.Y == emptyPoint.Y) return;
                for (var y = emptyPoint.Y; y < position.Y; y++) this[position.X, y] = this[position.X, y + 1];
                for (var y = emptyPoint.Y; y > position.Y; y--) this[position.X, y] = this[position.X, y - 1];
            }
            else if (position.Y == emptyPoint.Y)
            {
                for (var x = emptyPoint.X; x < position.X; x++) this[x, position.Y] = this[x + 1, position.Y];
                for (var x = emptyPoint.X; x > position.X; x--) this[x, position.Y] = this[x - 1, position.Y];
            }
            else return;
            this[position.X, position.Y] = Size - 1;
        }

        public Int32Point GetPoint(Direction dir)
        {
            var emptyPoint = EmptyPoint;
            switch (dir)
            {
                case Direction.Up:      return new Int32Point(emptyPoint.X, emptyPoint.Y + 1);
                case Direction.Down:    return new Int32Point(emptyPoint.X, emptyPoint.Y - 1);
                case Direction.Left:    return new Int32Point(emptyPoint.X + 1, emptyPoint.Y);
                case Direction.Right:   return new Int32Point(emptyPoint.X - 1, emptyPoint.Y);
                default:                throw new NotSupportedException();
            }
        }

        public bool IsInRange(Int32Point point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height;
        }
    }

    public enum Direction : byte
    {
        None = 0, Up = 1, Down = 2, Left = 3, Right = 4
    }

    public sealed class BoardWrapper : INotifyPropertyChanged
    {
        public BoardWrapper(string imagePath, int width, int height, BigInteger number, bool isTarget = false)
        {
            ImagePath = imagePath;
            Board = new Board(width, height, number);
            Target = isTarget ? Board : new Board(width, height);
            notifier.Interval = TimeSpan.FromSeconds(1);
            notifier.Tick += NotifyTimeChanged;
        }
        public BoardWrapper(string imagePath, int width, int height, bool isTarget = false)
            : this(imagePath, width, height, BigInteger.Zero, isTarget)
        {
        }

        public readonly string ImagePath;
        public readonly Board Board, Target;
        private const int BoardHash = 0x4753504D, BoardVersion = 1;
        private int moves;
        private TimeSpan time;
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly DispatcherTimer notifier = new DispatcherTimer();

        public int Moves { get { return moves; } set { moves = value; OnPropertyChanged("Moves"); } }
        public TimeSpan Time { get { return time + stopwatch.Elapsed; } }
        public bool Finished { get { return Board == Target; } }
        public bool IsPossible { get { return Board.OddHash == Target.OddHash; } }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as BoardWrapper);
        }

        private bool Equals(BoardWrapper board)
        {
            return !ReferenceEquals(null, board) && Board == board.Board;
        }

        public override int GetHashCode()
        {
            return Board.GetHashCode();
        }

        public static bool operator ==(BoardWrapper a, BoardWrapper b)
        {
            return ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
        }

        public static bool operator !=(BoardWrapper a, BoardWrapper b)
        {
            return !(a == b);
        }

        public void RandomGenerate()
        {
            Board.RandomGenerate();
            if (IsPossible && !Finished) return;
            int p = IsPossible ? 1 : 0, t = Board[0, 1];
            Board[0, 1] = Board[p, 0];
            Board[p, 0] = t;
            Board.FlushOddHash();
        }

        public IEnumerable<int> TryMove(Int32Point position)
        {
            foreach (var i in Board.TryMove(position))
            {
                Moves++;
                yield return i;
            }
            if (!Finished) yield break;
            PauseTiming();
            NotifyTimeChanged(this, EventArgs.Empty);
        }

        public void Save(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(BoardHash);
            writer.Write(BoardVersion);
            writer.Write(Board.Width);
            writer.Write(Board.Height);
            writer.Write(ImagePath);
            writer.Write(Moves);
            writer.Write(Time.Ticks);
            writer.Write(Board.Number.ToByteArray());
        }
        public void Save(string path)
        {
            using (var stream = File.Create(path)) Save(stream);
        }

        private static FileFormatException FileFormatError { get { return new FileFormatException("格式错误！"); } }

        public static BoardWrapper Load(Stream stream)
        {
            var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != BoardHash) throw FileFormatError;
            if (reader.ReadInt32() != BoardVersion) throw FileFormatError;
            int width = reader.ReadInt32(), height = reader.ReadInt32();
            var imagePath = reader.ReadString();
            var moves = reader.ReadInt32();
            var time = new TimeSpan(reader.ReadInt64());
            return new BoardWrapper(imagePath, width, height, new BigInteger(reader.ReadBytes
                ((int)(reader.BaseStream.Length - reader.BaseStream.Position)))) { Moves = moves, time = time };
        }
        public static BoardWrapper Load(string path)
        {
            using (var stream = File.OpenRead(path)) return Load(stream);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyTimeChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("Time");
        }

        public void StartTiming()
        {
            stopwatch.Start();
            notifier.Start();
        }

        public void PauseTiming()
        {
            stopwatch.Stop();
            notifier.Stop();
        }
    }

    public struct Int32Point
    {
        public Int32Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly int X, Y;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Int32Point && Equals((Int32Point) obj);
        }

        public bool Equals(Int32Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(Int32Point a, Int32Point b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Int32Point a, Int32Point b)
        {
            return !(a == b);
        }
    }

    public class ImageSplitter
    {
        public ImageSplitter(BitmapSource source, int rows, int columns)
        {
            Source = source;
            Rows = rows;
            Columns = columns;
            Result = new CroppedBitmap[Columns, Rows];
            double width = source.Width / columns, height = source.Height / rows;
            for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++)
                Result[x, y] = new CroppedBitmap(source, new Int32Rect((int) (x * width), (int) (y * height), (int) width, (int) height));
        }

        public readonly int Rows, Columns;
        public readonly CroppedBitmap[,] Result;
        public readonly BitmapSource Source;
    }

    public class FenwickTree<T>
    {
        public FenwickTree(int length)
        {
            f = new T[length + 1];
        }

        private readonly T[] f;

        private static int Lowbit(int i)
        {
            return i & -i;
        }

        public T GetSum(int max)
        {
            var result = default(T);
            for (max++; max > 0; max -= max & -max) result = Add(result, f[max]);
            return result;
        }
        public T GetSum(int min, int max)
        {
            return Subtract(GetSum(max), GetSum(min - 1));
        }

        public void Update(int index, T delta)
        {
            for (index++; index < f.Length; index += Lowbit(index)) f[index] = Add(f[index], delta);
        }

        public T this[int index]
        {
            get { return GetSum(index, index); }
            set { Update(index, Subtract(value, this[index])); }
        }

        private static T Add(T a, T b)
        {
            ParameterExpression paramA = Expression.Parameter(typeof(T), "a"), paramB = Expression.Parameter(typeof(T), "b");
            return Expression.Lambda<Func<T, T, T>>(Expression.Add(paramA, paramB), paramA, paramB).Compile()(a, b);
        }

        private static T Subtract(T a, T b)
        {
            ParameterExpression paramA = Expression.Parameter(typeof(T), "a"), paramB = Expression.Parameter(typeof(T), "b");
            return Expression.Lambda<Func<T, T, T>>(Expression.Subtract(paramA, paramB), paramA, paramB).Compile()(a, b);
        }
    }

    public class StoryboardQueue : Queue<Storyboard>
    {
        private Storyboard currentTask;
        public static Duration MoveDuration { get { return new Duration(TimeSpan.FromSeconds(Settings.Current.MoveDuration)); } }
        public static Duration FadingDuration { get { return new Duration(TimeSpan.FromSeconds(Settings.Current.FadingDuration)); } }
        public static Duration HighlightDuration { get { return new Duration(TimeSpan.FromSeconds(Settings.Current.HighlightDuration)); } }

        public void Begin()
        {
            if (Count == 0)
            {
                currentTask = null;
                return;
            }
            if (currentTask != null) return;
            currentTask = Dequeue();
            currentTask.Completed += TaskCompleted;
            currentTask.Begin();
        }

        private void TaskCompleted(object sender, EventArgs e)
        {
            currentTask = null;
            Begin();
        }

        public void EnqueueAndBegin(Storyboard storyboard)
        {
            Enqueue(storyboard);
            Begin();
        }
    }
}
