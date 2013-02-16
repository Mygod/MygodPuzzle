using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Mygod.Windows;

namespace Mygod.Puzzle
{
    public class AsyncImage : Image
    {
        static AsyncImage()
        {
            var sum = Environment.ProcessorCount - 1;   // leave one processor for smooth animation
            if (sum <= 0) sum = 1;
            Factory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(sum));
            Loading = new BitmapImage();
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;
            Loading.BeginInit();
            Loading.StreamSource = new FileStream(Path.Combine(CurrentApp.Directory, "Resources/Loading.png"), 
                                                  FileMode.Open, FileAccess.Read, FileShare.Read);
            Loading.EndInit();
            Loading.Freeze();
        }

        private static readonly BitmapImage Loading;
        private static readonly TaskFactory Factory;

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register("ImagePath", typeof(string), 
            typeof(AsyncImage), new PropertyMetadata(string.Empty, ImagePathChanged));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            private set { SetValue(IsLoadingPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsLoadingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsLoading", typeof(bool), typeof(AsyncImage), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLoadingProperty = IsLoadingPropertyKey.DependencyProperty;

        private static void ImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var image = (AsyncImage) d;
            image.IsLoading = true;
            image.Source = Loading;
            Factory.StartNew(AsyncLoad, new ThreadParameter(image, (e.NewValue ?? string.Empty).ToString()));
        }

        private static void AsyncLoad(object obj)
        {
            var parameter = (ThreadParameter) obj;
            var bitmap = Load(parameter.Path);
            parameter.Image.Dispatcher.Invoke(() => 
            {
                parameter.Image.Source = bitmap;
                parameter.Image.IsLoading = false;
            });
        }
        public static BitmapImage Load(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private class ThreadParameter
        {
            public ThreadParameter(AsyncImage image, string path)
            {
                Image = image;
                Path = path;
            }

            public readonly AsyncImage Image;
            public readonly string Path;
        }
    }
}
