using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GolfClubSystem.Data;
using SwimmingTrackSystem.Models;
using SwimmingTrackSystem.Windows;

namespace SwimmingTrackSystem.Views;

public partial class ProductView : UserControl, INotifyPropertyChanged
{
    public ObservableCollection<Product> Products { get; set; }
    private readonly UnitOfWork _unitOfWork = new();
    
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    
    public ProductView()
    {
        InitializeComponent();
        EditCommand = new RelayCommand<Product>(OnEdit);
        DeleteCommand = new RelayCommand<Product>(OnDelete);
        UpdateProducts();
        DataContext = this;
    }
    
    private void UpdateProducts()
    {
        var products = _unitOfWork.ProductRepository
            .GetAll(true)
            .ToList();
        Products = new ObservableCollection<Product>(products);
        OnPropertyChanged(nameof(Products));
    }
    
    private void OnEdit(Product product)
    {
        var window = new AddEditProductWindow(product);
        window.ShowDialog();
        UpdateProducts();
    }

    private async void OnDelete(Product product)
    {
        if (product == null) return;
        var answer = new DialogWindow("Вопрос?", $"Вы уверены удалить продукт: {product.ProductName}?", "Да", "Нет").ShowDialog();

        if (answer.HasValue && answer.Value)
        {
            var currentProduct = _unitOfWork.ProductRepository.GetAll().FirstOrDefault(o => o.Id == product.Id);
            if (currentProduct is not null)
            {
                await _unitOfWork.ProductRepository.DeleteAsync(product.Id);
                UpdateProducts();
            }
        }
    }

    private void AddProductCommand(object sender, RoutedEventArgs e)
    {
        var window = new AddEditProductWindow(null);
        window.ShowDialog();
        UpdateProducts();
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
}