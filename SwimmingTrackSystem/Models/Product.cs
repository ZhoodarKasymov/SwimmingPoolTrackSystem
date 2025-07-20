using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace SwimmingTrackSystem.Models;

public partial class Product : IDataErrorInfo, INotifyPropertyChanged
{
    public int Id { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal Price { get; set; }
    
    public string Time { get; set; } = null!;
    
    [NotMapped]
    public string Error { get; }
    [NotMapped] 
    private bool _hasError;
    
    [NotMapped]
    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError != value)
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }
    }

    private bool GetValidationErrors()
    {
        return !string.IsNullOrEmpty(ProductName)
               && Price != default && Price > 0;
    }

    [NotMapped]
    public string this[string columnName]
    {
        get
        {
            string result = null;

            switch (columnName)
            {
                case nameof(ProductName):
                {
                    if (string.IsNullOrEmpty(ProductName))
                    {
                        result = "Поле обязательно для заполнения";
                    }

                    break;
                }
                case nameof(Price):
                {
                    if (Price != default && Price <= 0)
                    {
                        result = "Цена должна быть больше 0";
                    }

                    break;
                }
            }

            HasError = GetValidationErrors();
            OnPropertyChanged(nameof(HasError));
            return result;
        }
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
