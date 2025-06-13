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
public class BettingViewModel : INotifyPropertyChanged
{
    public int Balance { get; set; } = 250;
    public int BetAmount { get; set; } = 20;
    private static Random random = new();
    public List<Horse> Horses { get; set; } = new()
    {
        new Horse("Блискавка", RandomBrush(), random.Next(5, 10)),
        new Horse("Граф", RandomBrush(), random.Next(5, 10)),
        new Horse("Підкова", RandomBrush(), random.Next(5, 10)),
        new Horse("Мажор", RandomBrush(), random.Next(5, 10)),
        new Horse("Армагедон", RandomBrush(), random.Next(5, 10)),
        new Horse("Йосип", RandomBrush(), random.Next(5, 10)),
    };

    private int horseIndex = 0;

    public string BetAmountText => $"{BetAmount}$";
    public string BalanceText => $"Balance: {Balance}$";
    public string SelectedHorseText => Horses[horseIndex].Name;

    public void IncreaseBet() => SetBet(BetAmount + 10);
    public void DecreaseBet() => SetBet(Math.Max(10, BetAmount - 10));
    public void NextHorse() => SetHorse((horseIndex + 1) % Horses.Count);
    public void PreviousHorse() => SetHorse((horseIndex - 1 + Horses.Count) % Horses.Count);

    private void SetBet(int value) { BetAmount = value; OnPropertyChanged(nameof(BetAmountText)); }
    private void SetHorse(int index) { horseIndex = index; OnPropertyChanged(nameof(SelectedHorseText)); }

    public void NotifyBalance()
    {
        OnPropertyChanged(nameof(BalanceText));
    }

    private static SolidColorBrush RandomBrush()
    {
        return new SolidColorBrush(Color.FromArgb(255,
            (byte)random.Next(0, 255),
            (byte)random.Next(0, 255),
            (byte)random.Next(0, 255)));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
