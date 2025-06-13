using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Block1;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BettingViewModel vm = new BettingViewModel();
    private List<Horse> Horses = new();
    private Barrier barrier;
    private CancellationTokenSource raceTokenSource;
    private const int FinishLine = 1600;
    private bool raceFinished = false;
    private Random random = new();

    public MainWindow()
    {
        InitializeComponent();

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string backgroundPath = System.IO.Path.Combine(baseDirectory, "Images", "Background", "Track.png");

        var backgroundImage = new BitmapImage(new Uri(backgroundPath));
        ((Image)canvas.Children[0]).Source = backgroundImage;

        Horses = vm.Horses;
        foreach (var horse in Horses)
        {
            if (horse.Color is SolidColorBrush brush)
            {
                var color = brush.Color;
                horse.AnimationFrames = GetHorseAnimation(color);
            }
        }

        DataContext = vm;
    }
    private void IncreaseBet_Click(object sender, RoutedEventArgs e) => vm.IncreaseBet();
    private void DecreaseBet_Click(object sender, RoutedEventArgs e) => vm.DecreaseBet();
    private void NextHorse_Click(object sender, RoutedEventArgs e) => vm.NextHorse();
    private void PreviousHorse_Click(object sender, RoutedEventArgs e) => vm.PreviousHorse();

    private void Bet_Click(object sender, RoutedEventArgs e)
    {
        if (vm.Balance < vm.BetAmount)
        {
            MessageBox.Show("Not enough money");
            return;
        }
        vm.Balance -= vm.BetAmount;
        foreach (var horse in Horses)
        {
            horse.Reset();
            horse.Speed = random.Next(5, 10);
        }

        StartRace();
    }


    private void StartRace()
    {
        raceFinished = false;
        raceTokenSource = new CancellationTokenSource();
        barrier = new Barrier(Horses.Count, _ => Dispatcher.Invoke(DrawHorses));

        foreach (var horse in Horses)
        {
            horse.StartMoving(
                barrier,
                FinishLine,
                () => Dispatcher.Invoke(DrawHorses),
                raceTokenSource.Token,
                OnHorseFinish);
        }
    }

    private void OnHorseFinish(Horse winner)
    {
        if (raceFinished) return;
        raceFinished = true;
        raceTokenSource.Cancel();

        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"{winner.Name} wins!\nTime: {winner.Time.TotalSeconds:F2} sec", "Race Result");

            bool win = winner.Name == vm.SelectedHorseText;
            double coeff = winner.Coefficient;

            if (win)
            {
                int payout = (int)(vm.BetAmount * coeff);
                vm.Balance += payout;
                MessageBox.Show($"You won! +{payout}$");
            }
            else
            {
                MessageBox.Show($"You lost! -{vm.BetAmount}$");
            }

            UpdateHorseCoefficients();
            dataGrid.Items.Refresh();
            vm.NotifyBalance();
        });
    }

    private void UpdateHorseCoefficients()
    {
        var sorted = Horses.OrderBy(h => h.Time).ToList();
        int n = Horses.Count;
        for (int i = 0; i < n; i++)
        {
            sorted[i].Coefficient = 1.5 + (n - i - 1) * 0.5;
        }
    }

    private void DrawHorses()
    {
        for (int i = canvas.Children.Count - 1; i >= 1; i--)
            canvas.Children.RemoveAt(i);

        var ranked = Horses.OrderByDescending(h => h.TrackX).ToList();
        for (int i = 0; i < ranked.Count; i++)
            ranked[i].Position = i + 1;

        for (int i = 0; i < Horses.Count; i++)
        {
            var horse = Horses[i];
            var image = new Image
            {
                Source = horse.CurrentFrame,
                Width = 80,
                Height = 80,
            };
            Canvas.SetLeft(image, horse.TrackX);
            Canvas.SetTop(image, 300 + i * 50);
            canvas.Children.Add(image);
        }
        dataGrid.Items.Refresh();
    }

    public List<ImageSource> GetHorseAnimation(Color color)
    {
        var bitmaps = ReadImageList("Images\\Horses");
        var masks = ReadImageList("Images\\HorsesMask");
        return bitmaps.Select((img, i) => GetImageWithColor(img, masks[i], color)).ToList();
    }

    private List<BitmapImage> ReadImageList(string folderPath)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string fullPath = System.IO.Path.Combine(baseDirectory, folderPath);

        if (!Directory.Exists(fullPath))
        {
            MessageBox.Show($"Directory not found: {fullPath}\nPlease ensure the Images folder is copied to the output directory.",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return new List<BitmapImage>();
        }

        return Directory.GetFiles(fullPath, "*.png")
                        .OrderBy(f => f)
                        .Select(f => new BitmapImage(new Uri(f, UriKind.Absolute)))
                        .ToList();
    }

    private ImageSource GetImageWithColor(BitmapImage image, BitmapImage mask, Color tintColor)
    {
        int width = image.PixelWidth, height = image.PixelHeight;
        var imageBmp = new WriteableBitmap(image);
        var maskBmp = new WriteableBitmap(mask);
        var output = new WriteableBitmap(width, height, image.DpiX, image.DpiY, PixelFormats.Bgra32, null);

        int stride = width * 4;
        byte[] imagePixels = new byte[height * stride];
        byte[] maskPixels = new byte[height * stride];
        byte[] resultPixels = new byte[height * stride];

        imageBmp.CopyPixels(imagePixels, stride, 0);
        maskBmp.CopyPixels(maskPixels, stride, 0);

        for (int i = 0; i < imagePixels.Length; i += 4)
        {
            double alphaFactor = maskPixels[i + 3] / 255.0;
            resultPixels[i] = (byte)(imagePixels[i] * (1 - alphaFactor) + tintColor.B * alphaFactor);
            resultPixels[i + 1] = (byte)(imagePixels[i + 1] * (1 - alphaFactor) + tintColor.G * alphaFactor);
            resultPixels[i + 2] = (byte)(imagePixels[i + 2] * (1 - alphaFactor) + tintColor.R * alphaFactor);
            resultPixels[i + 3] = imagePixels[i + 3];
        }

        output.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, stride, 0);
        return output;
    }
}
