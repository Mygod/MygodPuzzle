using System.ComponentModel;
using System.Windows.Media;
using Mygod.IO;

namespace Mygod.Puzzle
{
    internal sealed class Settings : IniFile, INotifyPropertyChanged
    {
        private Settings() : base("Settings.ini")
        {
            windowWidth = new DoubleData(appearance = this["Appearance"], "WindowWidth", 1024);
            windowHeight = new DoubleData(appearance, "WindowHeight", 600);
            borderThickness = new DoubleData(appearance, "BorderThickness", 1);
            borderColor = new ColorData(appearance, "BorderColor", Color.FromArgb(0x66, 0xdd, 0xdd, 0xdd));

            moveDuration = new DoubleData(animation = this["Animation"], "MoveDuration", 0.2);
            fadingDuration = new DoubleData(animation, "FadingDuration", 0.5);
            highlightDuration = new DoubleData(animation, "HighlightDuration", 0.2);

            boardWidth = new Int32Data(game = this["Game"], "BoardWidth", 3);
            boardHeight = new Int32Data(game, "BoardHeight", 3);

            bidirectional = new YesNoData(search = this["Search"], "Bidirectional", true);
            optimization = new DoubleData(search, "Optimization", 2);
        }

        public static readonly Settings Current = new Settings();

        private readonly IniSection appearance, animation, game, search;
        private readonly DoubleData windowWidth, windowHeight, borderThickness, moveDuration,
                                    fadingDuration, highlightDuration, optimization;
        private readonly Int32Data boardWidth, boardHeight;
        private readonly ColorData borderColor;
        private readonly YesNoData bidirectional;

        public double WindowWidth
        {
            get { return windowWidth.Get(); }
            set
            {
                windowWidth.Set(value);
                OnPropertyChanged("WindowWidth");
            }
        }
        public double WindowHeight
        {
            get { return windowHeight.Get(); } 
            set
            {
                windowHeight.Set(value);
                OnPropertyChanged("WindowHeight");
            }
        }
        public double BorderThickness
        {
            get { return borderThickness.Get(); } 
            set
            {
                borderThickness.Set(value);
                OnPropertyChanged("BorderThickness");
            }
        }
        public Color BorderColor
        {
            get { return borderColor.Get(); }
            set
            {
                borderColor.Set(value);
                OnPropertyChanged("BorderColor");
            }
        }
        public int BoardWidth
        {
            get { return boardWidth.Get(); }
            set
            {
                boardWidth.Set(value);
                OnPropertyChanged("BoardWidth");
            }
        }
        public int BoardHeight
        {
            get { return boardHeight.Get(); }
            set
            {
                boardHeight.Set(value);
                OnPropertyChanged("BoardHeight");
            }
        }
        public double MoveDuration
        {
            get { return moveDuration.Get(); }
            set
            {
                moveDuration.Set(value);
                OnPropertyChanged("MoveDuration");
            }
        }
        public double FadingDuration
        {
            get { return fadingDuration.Get(); }
            set
            {
                fadingDuration.Set(value);
                OnPropertyChanged("FadingDuration");
            }
        }
        public double HighlightDuration
        {
            get { return highlightDuration.Get(); }
            set
            {
                highlightDuration.Set(value);
                OnPropertyChanged("HighlightDuration");
            }
        }
        public bool Bidirectional
        {
            get { return bidirectional.Get(); }
            set
            {
                bidirectional.Set(value);
                OnPropertyChanged("Bidirectional");
            }
        }
        public double Optimization
        {
            get { return optimization.Get(); }
            set
            {
                optimization.Set(value);
                OnPropertyChanged("Optimization");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
