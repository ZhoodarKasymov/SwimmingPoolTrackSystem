using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace SwimmingTrackSystem.Models;

public partial class Setting : IDataErrorInfo, INotifyPropertyChanged
{
    public int Id { get; set; }

    public string EnterIp { get; set; } = null!;

    public string ExitIp { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? PosTerminalIp { get; set; }

    public string? RegNumber { get; set; }
    
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
        return !string.IsNullOrEmpty(PosTerminalIp)
               && !string.IsNullOrEmpty(EnterIp)
               && (!string.IsNullOrEmpty(ExitIp))
               && !string.IsNullOrEmpty(Login)
               && !string.IsNullOrEmpty(Password);
    }

    [NotMapped]
    public string this[string columnName]
    {
        get
        {
            string result = null;

            switch (columnName)
            {
                case nameof(PosTerminalIp):
                {
                    if (string.IsNullOrEmpty(PosTerminalIp))
                    {
                        result = "Поле обязательно для заполнения";
                    }

                    break;
                }
                case nameof(EnterIp):
                {
                    if (string.IsNullOrEmpty(EnterIp))
                    {
                        result = "Поле обязательно для заполнения";
                    }

                    break;
                }
                case nameof(ExitIp):
                {
                    if (string.IsNullOrEmpty(ExitIp))
                    {
                        result = "Поле обязательно для заполнения";
                    }

                    break;
                }
                case nameof(Login):
                {
                    if (string.IsNullOrEmpty(Login))
                    {
                        result = "Поле обязательно для заполнения";
                    }

                    break;
                }
                case nameof(Password):
                {
                    if (string.IsNullOrEmpty(Password))
                    {
                        result = "Поле обязательно для заполнения";
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
