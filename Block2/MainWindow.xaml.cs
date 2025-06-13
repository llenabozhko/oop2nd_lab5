using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Block2;

    public partial class MainWindow : Window
    {
        private List<Point> sites = new List<Point>();
        private List<Color> colors = new List<Color>();
        private Random rand = new Random();
        private int width = 900;
        private int height = 600;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Canvas_LeftClick(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(DrawCanvas);
            sites.Add(p);
            colors.Add(GetRandomColor());
            Redraw();
        }

        private void Canvas_RightClick(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(DrawCanvas);
            for (int i = 0; i < sites.Count; i++)
            {
                if ((sites[i] - p).Length < 10)
                {
                    sites.RemoveAt(i);
                    colors.RemoveAt(i);
                    break;
                }
            }
            Redraw();
        }

        private void GenerateRandomPoints_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 50; i++)
            {
                sites.Add(new Point(rand.Next(width), rand.Next(height)));
                colors.Add(GetRandomColor());
            }
            Redraw();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            sites.Clear();
            colors.Clear();
            DrawCanvas.Children.Clear();
        }

        private Color GetRandomColor() =>
            Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));

        private void Redraw()
        {
            if (sites.Count == 0)
            {
                DrawCanvas.Children.Clear();
                return;
            }

            bool isParallel = ((ComboBoxItem)ModeSelector.SelectedItem).Content.ToString().Contains("Паралельний");

            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(true);

            var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            var pixels = new byte[width * height * 4];
            var stride = width * 4;

            if (isParallel)
            {
                ProcessPixelsParallel(pixels, stride);
            }
            else
            {
                ProcessPixelsSequential(pixels, stride);
            }

            DrawVoronoiDiagram(bmp, pixels, stride);
            DrawSites();

            long memAfter = GC.GetTotalMemory(false);
            sw.Stop();

            UpdateStats(sw.ElapsedMilliseconds, memAfter - memBefore, isParallel);
        }

        private void ProcessPixelsParallel(byte[] pixels, int stride)
        {
            var localSites = sites.ToArray();
            var localColors = colors.ToArray();

            Parallel.For(0, height, y =>
            {
                ProcessRow(y, pixels, stride, localSites, localColors);
            });
        }

        private void ProcessPixelsSequential(byte[] pixels, int stride)
        {
            for (int y = 0; y < height; y++)
            {
                ProcessRow(y, pixels, stride, sites.ToArray(), colors.ToArray());
            }
        }

        private void ProcessRow(int y, byte[] pixels, int stride, Point[] siteArray, Color[] colorArray)
        {
            var rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + (x * 4);
                int nearest = GetNearestSiteIndex(new Point(x, y), siteArray);
                Color c = colorArray[nearest];
                SetPixelColor(pixels, index, c);
            }
        }

        private void SetPixelColor(byte[] pixels, int index, Color c)
        {
            pixels[index] = c.B;
            pixels[index + 1] = c.G;
            pixels[index + 2] = c.R;
            pixels[index + 3] = 255;
        }

        private void DrawVoronoiDiagram(WriteableBitmap bmp, byte[] pixels, int stride)
        {
            bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            DrawCanvas.Children.Clear();
            DrawCanvas.Children.Add(new Image { Source = bmp });
        }

        private void DrawSites()
        {
            foreach (var site in sites)
            {
                var ellipse = CreateSiteEllipse(site);
                DrawCanvas.Children.Add(ellipse);
            }
        }

        private Ellipse CreateSiteEllipse(Point site)
        {
            var ellipse = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Black };
            Canvas.SetLeft(ellipse, site.X - 3);
            Canvas.SetTop(ellipse, site.Y - 3);
            return ellipse;
        }

        private void UpdateStats(long elapsedMs, long memoryDiff, bool isParallel)
        {
            StatsText.Text = $"Час: {elapsedMs} мс | Пам'ять: {memoryDiff / 1024} КБ | Потік: {(isParallel ? "Паралельний" : "Один")}";
        }


        private int GetNearestSiteIndex(Point p, Point[] siteArray)
        {
            double minDist = double.MaxValue;
            int index = 0;
            for (int i = 0; i < siteArray.Length; i++)
            {
                double dist = (siteArray[i] - p).LengthSquared;
                if (dist < minDist)
                {
                    minDist = dist;
                    index = i;
                }
            }
            return index;
        }
    }
