using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SwimmingTrackSystem.Views;

namespace SwimmingTrackSystem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        NavigateCommand = new RelayCommand<string>(Navigate);
        CurrentView = new MainView();
        ActiveView = "Main";
    }
    
    private object _currentView;
    private string _activeView;

    public object CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public string ActiveView
    {
        get => _activeView;
        set
        {
            _activeView = value;
            OnPropertyChanged();
        }
    }
    
    private void Navigate(string viewName)
    {
        ActiveView = viewName; // Update the active view
        switch (viewName)
        {
            case "Main":
                CurrentView = new MainView();
                break;
            case "History":
                CurrentView = new HistoryView();
                break;
            case "Charts":
                CurrentView = new ChartsView();
                break;
            case "Products":
                CurrentView = new ProductView();
                break;
            case "Reports":
                CurrentView = new ReportsView();
                break;
            case "Settings":
                CurrentView = new SettingsView();
                break;
        }
    }

    public ICommand NavigateCommand { get; }

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