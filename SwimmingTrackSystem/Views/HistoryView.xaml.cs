using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GolfClubSystem.Data;
using SwimmingTrackSystem.Models;
using SwimmingTrackSystem.Windows;

namespace SwimmingTrackSystem.Views;

public class Item
{
    public string Name { get; set; }
    public int Id { get; set; }
}

public partial class HistoryView : UserControl, INotifyPropertyChanged
{
    public ObservableCollection<Transaction> Histories { get; set; }
    private readonly UnitOfWork _unitOfWork = new();

    public List<Item> Statuses { get; set; } = new()
    {
        new() { Name = "Все", Id = -1 },
        new() { Name = "Успешно", Id = 1 },
        new() { Name = "Не успешно", Id = 2 },
    };

    private int _currentPage = 1;
    private const int PageSize = 13;
    private DateTime? _startDate;
    private DateTime? _endDate;

    private bool _isNextPageEnabled;

    public bool IsNextPageEnabled
    {
        get => _isNextPageEnabled;
        set
        {
            _isNextPageEnabled = value;
            OnPropertyChanged(nameof(IsNextPageEnabled));
        }
    }

    private bool _isPreviousPageEnabled;

    public bool IsPreviousPageEnabled
    {
        get => _isPreviousPageEnabled;
        set
        {
            _isPreviousPageEnabled = value;
            OnPropertyChanged(nameof(IsPreviousPageEnabled));
        }
    }

    public HistoryView()
    {
        InitializeComponent();
        TodayFilter.Background = new SolidColorBrush(Color.FromRgb(46, 87, 230));
        TodayFilter.Foreground = Brushes.White;
        ApplyTodayFilter();
        
        DataContext = this;
        Unloaded += WorkersView_Unloaded;
    }

    private void ApplyFilters()
    {
        var filteredHistories = _unitOfWork.TransactionRepository
            .GetAll(true)
            .OrderByDescending(x => x.CreateDate)
            .AsQueryable();

        // Apply date filter if startDate and endDate are set
        if (_startDate.HasValue && _endDate.HasValue)
        {
            filteredHistories = filteredHistories.Where(h => h.CreateDate >= _startDate.Value
                                                             && h.CreateDate <= _endDate.Value);
        }

        // Apply status filter
        if (StatusFilter.SelectedItem != null && (StatusFilter.SelectedItem as Item).Id != -1)
        {
            var status = StatusFilter.SelectedItem as Item;
            filteredHistories = filteredHistories.Where(h => h.TypeTransaction == status.Name);
        }

        int total = filteredHistories.Count();

        // Paginate the results
        var pagedHistories = filteredHistories
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Update ObservableCollection
        Histories = new ObservableCollection<Transaction>(pagedHistories);
        OnPropertyChanged(nameof(Histories));
        PageNumberText.Text = _currentPage.ToString();

        // Update button states
        IsPreviousPageEnabled = _currentPage > 1;
        IsNextPageEnabled = (_currentPage * PageSize) < total;
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;

        // Reset all button backgrounds to gray
        TodayFilter.Background = Brushes.White;
        TodayFilter.Foreground = Brushes.Black;
        WeekFilter.Background = Brushes.White;
        WeekFilter.Foreground = Brushes.Black;
        MonthFilter.Background = Brushes.White;
        MonthFilter.Foreground = Brushes.Black;

        // Set the clicked button's background to blue
        if (button == TodayFilter)
        {
            TodayFilter.Background = new SolidColorBrush(Color.FromRgb(46, 87, 230));
            TodayFilter.Foreground = Brushes.White;
            ApplyTodayFilter();
        }
        else if (button == WeekFilter)
        {
            WeekFilter.Background = new SolidColorBrush(Color.FromRgb(46, 87, 230));
            WeekFilter.Foreground = Brushes.White;
            ApplyWeekFilter();
        }
        else if (button == MonthFilter)
        {
            MonthFilter.Background = new SolidColorBrush(Color.FromRgb(46, 87, 230));
            MonthFilter.Foreground = Brushes.White;
            ApplyMonthFilter();
        }
    }

    private void ApplyTodayFilter()
    {
        _startDate = DateTime.Today;
        _endDate = DateTime.Today.AddDays(1);
        ApplyFilters();
    }

    private void ApplyWeekFilter()
    {
        _endDate = DateTime.Today.AddDays(1);
        _startDate = _endDate.Value.AddDays(-6);
        ApplyFilters();
    }

    private void ApplyMonthFilter()
    {
        _endDate = DateTime.Today.AddDays(1);
        _startDate = _endDate.Value.AddMonths(-1).AddDays(-1);
        ApplyFilters();
    }
    
    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFilters();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        _currentPage++;
        ApplyFilters();
    }
    
    private void DatePicker_SelectedDateChanged(object sender, RoutedEventArgs e)
    {
        if (StartDatePicker?.SelectedDate is not null && EndDatePicker?.SelectedDate != null)
        {
            _startDate = StartDatePicker.SelectedDate.Value;
            _endDate = EndDatePicker.SelectedDate.Value;

            // Validate date range (must be within 1 month)
            if ((_endDate.Value - _startDate.Value).TotalDays > 30)
            {
                new DialogWindow("Ошибка", "Диапазон дат не должен превышать 1 месяца.").ShowDialog();
                return;
            }

            ApplyFilters();
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
    
    private void ReloadButton_click(object sender, RoutedEventArgs e)
    {
        var parentWindow = Window.GetWindow(this);
        if (parentWindow is { DataContext: INotifyPropertyChanged vm })
        {
            var commandProperty = vm.GetType().GetProperty("NavigateCommand");
            if (commandProperty != null)
            {
                if (commandProperty.GetValue(vm) is ICommand navigateCommand && navigateCommand.CanExecute("History"))
                {
                    navigateCommand.Execute("History");
                }
            }
        }
    }
    
    private void WorkersView_Unloaded(object sender, RoutedEventArgs e)
    {
        _unitOfWork.Dispose();
    }
}