using System.Windows.Media;
using Mygod.IO;

namespace Mygod.Puzzle
{
    internal static class Settings
    {
        static Settings()
        {
            SettingsFile = new IniFile("Settings.ini");
            AppearanceSection = new IniSection(SettingsFile, "Appearance");
            WindowWidthData = new DoubleData(AppearanceSection, "WindowWidth", 1024);
            WindowHeightData = new DoubleData(AppearanceSection, "WindowHeight", 600);
            BorderThicknessData = new DoubleData(AppearanceSection, "BorderThickness", 1);
            BorderColorData = new ColorData(AppearanceSection, "BorderColor", Color.FromArgb(0x66, 0xdd, 0xdd, 0xdd));

            AnimationSection = new IniSection(SettingsFile, "Animation");
            MoveDurationData = new DoubleData(AnimationSection, "MoveDuration", 0.2);
            FadingDurationData = new DoubleData(AnimationSection, "FadingDuration", 0.5);
            HighlightDurationData = new DoubleData(AnimationSection, "HighlightDuration", 0.2);

            GameSection = new IniSection(SettingsFile, "Game");
            BoardWidthData = new Int32Data(GameSection, "BoardWidth", 3);
            BoardHeightData = new Int32Data(GameSection, "BoardHeight", 3);
        }

        private static readonly IniFile SettingsFile;
        private static readonly IniSection AppearanceSection, AnimationSection, GameSection;
        private static readonly DoubleData WindowWidthData, WindowHeightData, BorderThicknessData,
                                           MoveDurationData, FadingDurationData, HighlightDurationData;
        private static readonly Int32Data BoardWidthData, BoardHeightData;
        private static readonly ColorData BorderColorData;

        public static double WindowWidth { get { return WindowWidthData.Get(); } set { WindowWidthData.Set(value); } }
        public static double WindowHeight { get { return WindowHeightData.Get(); } set { WindowHeightData.Set(value); } }
        public static double BorderThickness { get { return BorderThicknessData.Get(); } set { BorderThicknessData.Set(value); } }
        public static Color BorderColor { get { return BorderColorData.Get(); } set { BorderColorData.Set(value); } }
        public static int BoardWidth { get { return BoardWidthData.Get(); } set { BoardWidthData.Set(value); } }
        public static int BoardHeight { get { return BoardHeightData.Get(); } set { BoardHeightData.Set(value); } }
        public static double MoveDuration { get { return MoveDurationData.Get(); } set { MoveDurationData.Set(value); } }
        public static double FadingDuration { get { return FadingDurationData.Get(); } set { FadingDurationData.Set(value); } }
        public static double HighlightDuration { get { return HighlightDurationData.Get(); } set { HighlightDurationData.Set(value); } }
    }
}
