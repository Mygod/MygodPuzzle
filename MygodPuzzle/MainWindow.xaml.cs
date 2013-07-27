/* Pages:
 * 0. MainPage
 * 1. SelectLevelPage
 * 2. SelectDifficultyPage
 * 3. GamingPage
 * 4. WinPage
 * 5. LoadSavedataPage
 * 6. SettingsPage
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Mygod.Windows;

namespace Mygod.Puzzle
{
    public partial class MainWindow
    {
        #region Global

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(saveDataPath);
            watcher = new FileSystemWatcher(saveDataPath, "*.dat");
            watcher.Created += UpdateProfileList;
            watcher.Deleted += UpdateProfileList;
            watcher.EnableRaisingEvents = true;
            reseau.SetBinding(Reseau.ColumnsProperty, new Binding("BoardWidth") { Source = Settings.Current });
            reseau.SetBinding(Reseau.RowsProperty, new Binding("BoardHeight") { Source = Settings.Current });
            reseau.SetBinding(Reseau.BorderBrushProperty, new Binding("BorderBrush") { Source = Settings.Current });
            reseau.SetBinding(Reseau.BorderThicknessProperty, new Binding("BorderThickness") { Source = Settings.Current });
            UpdateProfileList();
            BackgroundBrush.ImageSource = new BitmapImage(new Uri(Path.Combine(CurrentApp.Directory, "Resources/Background.png")));
            Spinner.Source = new BitmapImage(new Uri(Path.Combine(CurrentApp.Directory, "Resources/Processing.png")));
            PictureList.ItemsSource = new DirectoryInfo(Path.Combine(CurrentApp.Directory, "Pictures"))
                .EnumerateFiles().Select(file => file.FullName);
        }

        private bool fadingOut;
        private Point mainPoint;
        private double mainHeight;
        private readonly StoryboardQueue queue = new StoryboardQueue();

        private void FadeIn(object sender, RoutedEventArgs e)
        {
            var s = GetFadingStoryboard(true);
            s.Completed += (a, b) =>
            {
                mainPoint = MainLabel.TranslatePoint(new Point(0, 0), LastPieceCanvas);
                mainHeight = MainLabel.ActualHeight;
            };
            queue.EnqueueAndBegin(s);
        }

        private void FadeOut(object sender, CancelEventArgs e)
        {
            if (fadingOut) return;
            e.Cancel = true;
            if (Tabs.SelectedIndex == 3 && !TryExitBoard()) return;
            fadingOut = true;
            var storyboard = GetFadingStoryboard(false);
            storyboard.Completed += (a, b) => Close();
            queue.EnqueueAndBegin(storyboard);
        }

        private void PreviousPage(object sender = null, RoutedEventArgs e = null)
        {
            SwitchTab(Tabs.SelectedIndex - 1);
        }

        private void NextPage(object sender = null, RoutedEventArgs e = null)
        {
            switch (Tabs.SelectedIndex)
            {
                case 1:
                    if (PictureList.SelectedItem != null) SwitchTab(2, UpdatePicture);
                    break;
                case 2:
                    SwitchTab(3, StartGame, () => 
                    {
                        wrapper.StartTiming();
                        MoveLastPiece(true);
                    });
                    break;
                default:
                    SwitchTab(Tabs.SelectedIndex + 1);
                    break;
            }
        }

        private void GoToLoadPage(object sender, RoutedEventArgs e)
        {
            SwitchTab(5);
        }

        private Storyboard GetFadingStoryboard(bool fadeIn, DependencyObject target = null)
        {
            var s = new Storyboard();
            var anim = new DoubleAnimation(fadeIn ? 1 : 0, StoryboardQueue.FadingDuration);
            Storyboard.SetTarget(anim, target ?? MainGrid);
            Storyboard.SetTargetProperty(anim, new PropertyPath(OpacityProperty));
            s.Children.Add(anim);
            return s;
        }

        private void SwitchTab(int index, Action fadedOut = null, Action fadedIn = null)
        {
            var storyboard = GetFadingStoryboard(false);
            storyboard.Completed += (sender, e) =>
            {
                Tabs.SelectedIndex = index;
                if (fadedOut != null) fadedOut();
                var s = GetFadingStoryboard(true);
                s.Completed += (ss, ee) =>
                {
                    if (fadedIn != null) fadedIn();
                    switch (index)
                    {
                        case 1:
                            PictureList.Focus();
                            break;
                        case 2:
                            WidthBox.Focus();
                            break;
                        case 5:
                            ProfileList.Focus();
                            break;
                    }
                };
                queue.EnqueueAndBegin(s);
            };
            queue.EnqueueAndBegin(storyboard);
        }

        private void TryExitBoard(object sender, RoutedEventArgs e)
        {
            switch (Tabs.SelectedIndex)
            {
                case 3:
                    if (TryExitBoard()) SwitchTab(0, LastPieceCanvas.Children.Clear);
                    break;
                default:
                    SwitchTab(0);
                    break;
            }
        }

        #endregion

        #region Main Page

        private void ExitGame(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GoToSettingsPage(object sender, RoutedEventArgs e)
        {
            SwitchTab(6);
        }

        #endregion

        #region Select Diffulculty Page

        private readonly Image preview = new Image();
        private readonly Reseau reseau = new Reseau();

        private void UpdatePicture()
        {
            SelectDifficultyGrid.Children.Clear();
            preview.SetBinding(Image.SourceProperty, new Binding
            {
                Source = PictureList.ItemContainerGenerator.ContainerFromItem(PictureList.SelectedItem).FindVisualChild<AsyncImage>(), 
                Path = new PropertyPath("Source")
            });
            SelectDifficultyGrid.Children.Add(preview);
            SelectDifficultyGrid.Children.Add(reseau);
        }

        #endregion

        #region Gaming Page

        private BoardWrapper wrapper;
        private ImageSplitter splitter;
        private Size pictureSize;
        private Grid[] pieces;
        private bool[] highlighted;
        private GamingStatus status;

        private void RecalcPictureSize()
        {
            pictureSize = new Size(GamingCanvas.Width / wrapper.Board.Width, mainHeight / wrapper.Board.Height);
            double thicknessX = Settings.Current.BorderThickness / 2, thicknessY = thicknessX;
            if (pictureSize.Width >= pictureSize.Height) thicknessX *= pictureSize.Height / pictureSize.Width;
            else thicknessY *= pictureSize.Width / pictureSize.Height;
            var borderThickness = new Thickness(thicknessX, thicknessY, thicknessX, thicknessY);
            foreach (var grid in GamingCanvas.Children.OfType<Grid>())
            {
                var key = (int)grid.Tag;
                var point = wrapper.Board.Mappings[key];
                Canvas.SetLeft(grid, point.X * pictureSize.Width);
                Canvas.SetTop(grid, point.Y * pictureSize.Height);
                grid.Width = pictureSize.Width;
                grid.Height = pictureSize.Height;
                ((FrameworkElement)grid.Children[0]).Margin = borderThickness;
            }
        }

        private Rect GetLastPieceActualRect()
        {
            var lastPieceRect = new Rect(LastPieceGrid.TranslatePoint(new Point(0, 0), LastPieceCanvas),
                                         new Size(LastPieceGrid.ActualWidth, LastPieceGrid.ActualHeight));
            var result = new Rect();
            if (pictureSize.Width / pictureSize.Height > lastPieceRect.Width / lastPieceRect.Height)
            {
                result.Width = lastPieceRect.Width;
                result.Height = lastPieceRect.Width / pictureSize.Width * pictureSize.Height;
                result.X = lastPieceRect.X;
                result.Y = lastPieceRect.Y + (lastPieceRect.Height - result.Height) / 2;
            }
            else
            {
                result.Width = lastPieceRect.Height * pictureSize.Width / pictureSize.Height;
                result.Height = lastPieceRect.Height;
                result.X = lastPieceRect.X + (lastPieceRect.Width - result.Width) / 2;
                result.Y = lastPieceRect.Y;
            }
            return result;
        }
        private Rect GetLastPieceCorrectRect()
        {
            var position = wrapper.Board.Mappings[wrapper.Board.Size - 1];
            return new Rect(mainPoint.X + pictureSize.Width * position.X, mainPoint.Y + pictureSize.Height * position.Y,
                            pictureSize.Width, pictureSize.Height);
        }
        private void MoveLastPiece(bool toBox, EventHandler completed = null)
        {
            var storyboard = new Storyboard();
            Storyboard.SetTarget(storyboard, pieces[wrapper.Board.Size - 1]);
            var duration = StoryboardQueue.MoveDuration;
            Rect fromRect = GetLastPieceActualRect(), toRect = GetLastPieceCorrectRect();
            if (toBox)
            {
                var t = fromRect;
                fromRect = toRect;
                toRect = t;
            }
            var animation = new DoubleAnimation(fromRect.X, toRect.X, duration);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.LeftProperty));
            storyboard.Children.Add(animation);
            animation = new DoubleAnimation(fromRect.Y, toRect.Y, duration);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.TopProperty));
            storyboard.Children.Add(animation);
            animation = new DoubleAnimation(fromRect.Width, toRect.Width, duration);
            Storyboard.SetTargetProperty(animation, new PropertyPath(WidthProperty));
            storyboard.Children.Add(animation);
            animation = new DoubleAnimation(fromRect.Height, toRect.Height, duration);
            Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));
            storyboard.Children.Add(animation);
            if (completed != null) storyboard.Completed += completed;
            queue.EnqueueAndBegin(storyboard);
        }

        private void RestoreGame()
        {
            status = GamingStatus.Playing;
            GamingCanvas.Width = preview.Source.Width / preview.Source.Height * mainHeight;
            StatusDisplay.DataContext = null;
            StatusDisplay.DataContext = wrapper;
            splitter = new ImageSplitter((BitmapSource) preview.Source, wrapper.Board.Height, wrapper.Board.Width);
            pieces = new Grid[wrapper.Board.Size];
            highlighted = new bool[wrapper.Board.Size];
            GamingCanvas.Children.Clear();
            for (var y = 0; y < wrapper.Board.Height; y++) for (var x = 0; x < wrapper.Board.Width; x++)
            {
                var key = wrapper.Board.GetKey(x, y);
                pieces[key] = new Grid { Background = new SolidColorBrush(Settings.Current.BorderColor),
                                         Tag = key, Children = { new Image { Source = splitter.Result[x, y] } } };
                if (key < wrapper.Board.Size - 1)
                {
                    pieces[key].Cursor = Cursors.Hand;
                    pieces[key].MouseLeftButtonDown += TryMove;
                }
                GamingCanvas.Children.Add(pieces[key]);
            }
            RecalcPictureSize();
            GamingCanvas.Children.Remove(pieces[wrapper.Board.Size - 1]);
            LastPieceCanvas.Children.Clear();
            LastPieceCanvas.Children.Add(pieces[wrapper.Board.Size - 1]);
            var rect = GetLastPieceCorrectRect();
            Canvas.SetLeft(pieces[wrapper.Board.Size - 1], rect.X);
            Canvas.SetTop(pieces[wrapper.Board.Size - 1], rect.Y);
        }

        private void StartGame()
        {
            wrapper = new BoardWrapper(PictureList.SelectedItem.ToString(), Settings.Current.BoardWidth, Settings.Current.BoardHeight);
            wrapper.RandomGenerate();
            RestoreGame();
        }

        private void TryMove(Int32Point point)
        {
            var affected = wrapper.TryMove(point).ToArray();
            if (affected.Length > 0)
            {
                var storyboard = new Storyboard();
                var duration = StoryboardQueue.MoveDuration;
                foreach (var i in affected)
                {
                    var animation = new DoubleAnimation(pictureSize.Width * wrapper.Board.Mappings[i].X, duration);
                    Storyboard.SetTarget(animation, pieces[i]);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.LeftProperty));
                    storyboard.Children.Add(animation);
                    animation = new DoubleAnimation(pictureSize.Height * wrapper.Board.Mappings[i].Y, duration);
                    Storyboard.SetTarget(animation, pieces[i]);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.TopProperty));
                    storyboard.Children.Add(animation);
                }
                queue.EnqueueAndBegin(storyboard);
                if (wrapper.Finished)
                {
                    status = GamingStatus.Won;
                    MoveLastPiece(false, (ss, ee) => SwitchTab(4, () =>
                    {
                        LastPieceCanvas.Children.Clear();
                        WinTab.Content = new Image { Source = splitter.Source };
                    }));
                }
            }
            else SystemSounds.Exclamation.Play();
        }
        private void TryMove(object sender, MouseButtonEventArgs e)
        {
            if (status == GamingStatus.Playing) TryMove(wrapper.Board.Mappings[(int)((Grid)sender).Tag]);
        }

        private void PeekMove(object sender, MouseEventArgs e)
        {
            if (Tabs.SelectedIndex != 3) return;
            if (status != GamingStatus.Playing)
            {
                Highlight(new int[0]);
                return;
            }
            var pos = e.GetPosition(GamingCanvas);
            Highlight(new HashSet<int>(wrapper.Board.PeekMove(new Int32Point((int)(pos.X / pictureSize.Width), 
                                                                             (int) (pos.Y / pictureSize.Height)))));
        }

        private void Highlight(ICollection<int> set)
        {
            for (var i = 0; i < wrapper.Board.Size; i++)
            {
                var b = set.Contains(i);
                if (highlighted[i] && !b) pieces[i].BeginAnimation(OpacityProperty, new DoubleAnimation(1, StoryboardQueue.MoveDuration));
                else if (!highlighted[i] && b)
                    pieces[i].BeginAnimation(OpacityProperty, new DoubleAnimation(0.5, StoryboardQueue.HighlightDuration));
                highlighted[i] = b;
            }
        }

        private void SaveBoard(object sender = null, RoutedEventArgs e = null)
        {
            if (status != GamingStatus.Won) wrapper.Save(Path.Combine(saveDataPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".dat"));
        }

        private bool TryExitBoard()
        {
            wrapper.PauseTiming();
            if (status != GamingStatus.Won)
            {
                var result = MessageBox.Show("退出前要保存你当前的游戏吗？", "退出当前游戏",
                                             MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Cancel)
                {
                    wrapper.StartTiming();
                    return false;
                }
                if (result == MessageBoxResult.Yes) SaveBoard();
            }
            queue.Clear();
            return true;
        }

        private void Solve(IBoardSolver solver)
        {
            if (status != GamingStatus.Playing) return;
            status = GamingStatus.Processing;
            Processing.IsHitTestVisible = true;
            queue.EnqueueAndBegin(GetFadingStoryboard(true, Processing));
            var solutionFinder = new Task<IEnumerable<Int32Point>>(() => solver.GetSolution(wrapper));
            solutionFinder.Start();
            solutionFinder.ContinueWith(task => Dispatcher.Invoke(() =>
            {
                var storyboard = GetFadingStoryboard(false, Processing);
                storyboard.Completed += (sender, e) =>
                {
                    status = GamingStatus.Showing;
                    Int32Point last = wrapper.Board.Mappings[wrapper.Board.Size - 1], previous = last;
                    foreach (var point in task.Result)
                    {
                        if (point.X != last.X && point.Y != last.Y) TryMove(last = previous);
                        previous = point;
                    }
                    TryMove(previous);
                    Processing.IsHitTestVisible = false;
                };
                queue.EnqueueAndBegin(storyboard);
            }));
        }

        private void FastSolution(object sender, RoutedEventArgs e)
        {
            Solve(new CommonSolver());
        }

        private void SearchForSolution(object sender, RoutedEventArgs e)
        {
            Solve(new SearchSolver());
        }

        #endregion

        #region Load Savedata Page

        private readonly string saveDataPath = Path.Combine(CurrentApp.Directory, "Savedata");
        private readonly FileSystemWatcher watcher;

        private void UpdateProfileList(object sender = null, FileSystemEventArgs e = null)
        {
            Dispatcher.Invoke(() => ProfileList.ItemsSource = new DirectoryInfo(saveDataPath).EnumerateFiles("*.dat"));
        }

        private void LoadSavedata(object sender, RoutedEventArgs e)
        {
            var file = ProfileList.SelectedItem as FileInfo;
            if (file == null) return;
            try
            {
                wrapper = BoardWrapper.Load(file.FullName);
                preview.Source = AsyncImage.Load(wrapper.ImagePath);
                SwitchTab(3, RestoreGame, () =>
                {
                    wrapper.StartTiming();
                    MoveLastPiece(true);
                });
            }
            catch
            {
                MessageBox.Show("载入失败！", "载入进度", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteProfile(object sender, RoutedEventArgs e)
        {
            var file = ProfileList.SelectedItem as FileInfo;
            if (file != null) file.Delete();
        }

        private void DeleteAllProfile(object sender, RoutedEventArgs e)
        {
            foreach (var file in Directory.EnumerateFileSystemEntries(saveDataPath, "*.dat")) File.Delete(file);
        }

        private void ReturnMainPage(object sender, RoutedEventArgs e)
        {
            SwitchTab(0);
        }

        #endregion
    }

    public enum GamingStatus
    {
        Playing, Won, Processing, Showing
    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class MinuteSecondConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan)) return null;
            var span = (TimeSpan) value;
            return string.Format("{0}:{1:00}", Math.Floor(span.TotalMinutes), span.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class ToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? null : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
