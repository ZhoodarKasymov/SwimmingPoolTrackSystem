using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GolfClubSystem.Data;
using LiveCharts;
using LiveCharts.Wpf;
using SwimmingTrackSystem.Windows;

namespace SwimmingTrackSystem.Views;

public partial class ChartsView : UserControl, INotifyPropertyChanged
{
    private readonly UnitOfWork _unitOfWork = new();
    private DateTime? _startDate;
    private DateTime? _endDate;
    
    public ChartsView()
    {
        InitializeComponent();
        TodayFilter.Background = new SolidColorBrush(Color.FromRgb(46, 87, 230));
        TodayFilter.Foreground = Brushes.White;
        ApplyTodayFilter();
        
        DataContext = this;
        Unloaded += WorkersView_Unloaded;
    }
    
    private void WorkersView_Unloaded(object sender, RoutedEventArgs e)
    {
        _unitOfWork.Dispose();
    }
    
    private void ApplyFilters()
    {
        var filteredHistories = _unitOfWork.TransactionRepository
            .GetAll(true)
            .Where(t => t.ErrorMessage == null)
            .OrderByDescending(x => x.CreateDate)
            .AsQueryable();

        // Apply date filter if startDate and endDate are set
        if (_startDate.HasValue && _endDate.HasValue)
        {
            filteredHistories = filteredHistories.Where(h => h.CreateDate >= _startDate.Value
                                                             && h.CreateDate <= _endDate.Value);
        }

        var groupedTransactions = filteredHistories.GroupBy(h => h.ProductName).ToList();
        
        // Настройка данных для столбчатой диаграммы
        var chartValues = new ChartValues<decimal>();
        var labels = new List<string>();

        foreach (var history in groupedTransactions)
        {
            chartValues.Add(history.Sum(t => t.Amount));
            labels.Add(history.Key ?? string.Empty);
        }

        BarChart.Series = new SeriesCollection
        {
            new ColumnSeries
            {
                Title = "Сумма продукта:",
                Values = chartValues,
                Fill = new SolidColorBrush(Color.FromRgb(121, 135, 255))
            }
        };

        // Установка подписей по осям
        BarChart.AxisX[0].Labels = labels;
        BarChart.AxisY[0].LabelFormatter = value => value.ToString("N0", CultureInfo.InvariantCulture);
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
    
    private void ReloadButton_click(object sender, RoutedEventArgs e)
    {
        var parentWindow = Window.GetWindow(this);
        if (parentWindow is { DataContext: INotifyPropertyChanged vm })
        {
            var commandProperty = vm.GetType().GetProperty("NavigateCommand");
            if (commandProperty != null)
            {
                if (commandProperty.GetValue(vm) is ICommand navigateCommand && navigateCommand.CanExecute("Charts"))
                {
                    navigateCommand.Execute("Charts");
                }
            }
        }
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