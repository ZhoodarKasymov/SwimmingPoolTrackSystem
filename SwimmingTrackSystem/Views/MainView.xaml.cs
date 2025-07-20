using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GolfClubSystem.Data;
using SwimmingTrackSystem.Helper;
using SwimmingTrackSystem.Models;
using SwimmingTrackSystem.Services;
using SwimmingTrackSystem.Windows;
using Transaction = SwimmingTrackSystem.Models.Transaction;

namespace SwimmingTrackSystem.Views;

public partial class MainView : UserControl, INotifyPropertyChanged
{
    public ObservableCollection<Transaction> Histories { get; set; }
    public ObservableCollection<Product> Products { get; set; }
    private readonly UnitOfWork _unitOfWork = new();
    public MainView()
    {
        InitializeComponent();
        DataContext = this;
        UpdateHistories();
        var products = _unitOfWork.ProductRepository.GetAll(true).ToList();
        Products = new ObservableCollection<Product>(products);
        
    }

    public void UpdateHistories()
    {
        var histories = _unitOfWork.TransactionRepository
            .GetAll(true)
            .OrderByDescending(t => t.CreateDate)
            .Take(10)
            .ToList();
        Histories = new ObservableCollection<Transaction>(histories);
        OnPropertyChanged(nameof(Histories));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    private void ReloadButton_click(object sender, RoutedEventArgs e)
    {
        var parentWindow = Window.GetWindow(this);
        if (parentWindow is { DataContext: INotifyPropertyChanged vm })
        {
            var commandProperty = vm.GetType().GetProperty("NavigateCommand");
            if (commandProperty != null)
            {
                if (commandProperty.GetValue(vm) is ICommand navigateCommand && navigateCommand.CanExecute("Main"))
                {
                    navigateCommand.Execute("Main");
                }
            }
        }
    }

    private async void CreditCard_click(object sender, RoutedEventArgs e)
    {
        if (ProductList.SelectedItem is null)
        {
            new DialogWindow("Ошибка", "Продукт должен быть выбран для оплаты!").ShowDialog();
            return;
        }
        var selectedProduct = ProductList.SelectedItem as Product;
        await MadeTransaction(selectedProduct, "Карта");
    }

    private async void Qr_click(object sender, RoutedEventArgs e)
    {
        if (ProductList.SelectedItem is null)
        {
            new DialogWindow("Ошибка", "Продукт должен быть выбран для оплаты!").ShowDialog();
            return;
        }
        var selectedProduct = ProductList.SelectedItem as Product;
        await MadeTransaction(selectedProduct, "QR");
    }

    private async Task MadeTransaction(Product selectedProduct, string type)
    {
        var settings = _unitOfWork.SettingRepository.GetAll().SingleOrDefault();
        
        if (settings == null)
        {
            new DialogWindow("Ошибка", "Добавьте настройки системы!").ShowDialog();
            return;
        }
        
        using var posTerminalService = new PosTerminalService(settings.PosTerminalIp);

        var errorMessage = await posTerminalService.ProcessPaymentAsync(selectedProduct.Price, selectedProduct.ProductName);
        var nowDate = DateTime.Now;

        if (string.IsNullOrEmpty(errorMessage))
        {
            using var terminalService = new TerminalService(settings.Login, settings.Password);
            
            var transaction = new Transaction
            {
                ProductName = selectedProduct.ProductName,
                Amount = selectedProduct.Price,
                CreateDate = nowDate,
                ExpireDate = GetDateTimeByTime(nowDate, selectedProduct.Time),
                TypeTransaction = type
            };

            await _unitOfWork.TransactionRepository.AddAsync(transaction);
            var guidUniqe = Guid.NewGuid().ToString("N").Substring(0, 20);
            
            await terminalService.AddUserInfoAsync(transaction, settings.EnterIp);
            await terminalService.AddUserInfoAsync(transaction, settings.ExitIp);

            await terminalService.AddCardInfoAsync(transaction, settings.EnterIp, guidUniqe);
            await terminalService.AddCardInfoAsync(transaction, settings.ExitIp, guidUniqe);
            
            PrinterHelper.TransactionCreatedDate = nowDate;
            PrinterHelper.ExpireDate = transaction.ExpireDate.Value;
            PrinterHelper.Guid = guidUniqe;
            
            UpdateHistories();
            PrinterHelper.Print();
        }
        else
        {
            var transaction = new Transaction
            {
                ProductName = selectedProduct.ProductName,
                Amount = selectedProduct.Price,
                CreateDate = nowDate,
                TypeTransaction = type,
                ErrorMessage = errorMessage
            };
            await _unitOfWork.TransactionRepository.AddAsync(transaction);
            UpdateHistories();
        }
    }

    private DateTime GetDateTimeByTime(DateTime createdDate, string type)
    {
        return type switch
        {
            "12hour" => createdDate.AddHours(12),
            "24hour" => createdDate.AddHours(24),
            "week" => createdDate.AddDays(7),
            "month" => createdDate.AddMonths(1),
            _ => createdDate
        };
    }
}