using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GolfClubSystem.Data;
using Microsoft.Win32;
using OfficeOpenXml;
using Serilog;
using SwimmingTrackSystem.Windows;
using LicenseContext = System.ComponentModel.LicenseContext;

namespace SwimmingTrackSystem.Views;

public class Report
{
    public int CountVisitors { get; set; }
    public decimal Sum { get; set; }
    public string ProductName { get; set; }
    public string TypePayment { get; set; }
    public int EnterSum { get; set; }
    public int ExitSum { get; set; }
}

public partial class ReportsView : UserControl, INotifyPropertyChanged
{
    public ObservableCollection<Report> Histories { get; set; }
    private readonly UnitOfWork _unitOfWork = new();

    public List<Item> Statuses { get; set; } = new()
    {
        new() { Name = "Все", Id = -1 },
        new() { Name = "Успешно", Id = 1 },
        new() { Name = "Не успешно", Id = 2 },
    };

    private int _currentPage = 1;
    private const int PageSize = 10;
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

    public ReportsView()
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
            .Where(t => t.ErrorMessage == null)
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

        var groupedHistory = filteredHistories.GroupBy(h => h.ProductName);

        int total = groupedHistory.Count();

        // Paginate the results
        var pagedHistories = groupedHistory
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(g => new Report()
            {
                ProductName = g.Key,
                Sum = g.Sum(t => t.Amount),
                CountVisitors = g.Count(),
                TypePayment = g.First().TypeTransaction,
                ExitSum = g.Count(t => t.Status == 2),
                EnterSum = g.Count(t => t.Status == 1)
            })
            .ToList();

        // Update ObservableCollection
        Histories = new ObservableCollection<Report>(pagedHistories);
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

    private void Export_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Set EPPlus license context (required for non-commercial use)
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            // Create a new Excel package
            using var package = new ExcelPackage();
            // Add a worksheet
            var worksheet = package.Workbook.Worksheets.Add("Отчет");

            worksheet.Cells[1, 1].Value = $"{_startDate:dd/MM/yyyy}-{_endDate:dd/MM/yyyy}";

            // Define headers from your DataGrid
            string[] headers = new[]
            {
                "Кол-во посетителей",
                "Сумма оплат",
                "Тип оплаты",
                "Продукт",
                "Сколько зашло",
                "Сколько вышло"
            };

            // Add headers to the worksheet
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[2, i + 1].Value = headers[i];
                worksheet.Cells[2, i + 1].Style.Font.Bold = true;
                worksheet.Cells[2, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[2, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Add data from Histories collection
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

            // Apply status filter
            if (StatusFilter.SelectedItem != null && (StatusFilter.SelectedItem as Item).Id != -1)
            {
                var status = StatusFilter.SelectedItem as Item;
                filteredHistories = filteredHistories.Where(h => h.TypeTransaction == status.Name);
            }

            var groupedHistory = filteredHistories.GroupBy(h => h.ProductName);
            // Paginate the results
            var pagedHistories = groupedHistory
                .Select(g => new Report()
                {
                    ProductName = g.Key,
                    Sum = g.Sum(t => t.Amount),
                    CountVisitors = g.Count(),
                    TypePayment = g.First().TypeTransaction,
                    ExitSum = g.Count(t => t.Status == 2),
                    EnterSum = g.Count(t => t.Status == 1)
                })
                .ToList();
            
            for (var i = 0; i < pagedHistories.Count; i++)
            {
                var report = pagedHistories[i];
                worksheet.Cells[i + 3, 1].Value = report.CountVisitors;
                worksheet.Cells[i + 3, 2].Value = report.Sum;
                worksheet.Cells[i + 3, 3].Value = report.TypePayment;
                worksheet.Cells[i + 3, 4].Value = report.ProductName;
                worksheet.Cells[i + 3, 5].Value = report.EnterSum;
                worksheet.Cells[i + 3, 6].Value = report.ExitSum;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Add borders to all cells
            using (var range = worksheet.Cells[1, 1,
                       Histories?.Count + 1 ?? 1, headers.Length])
            {
                range.Style.Border.Top.Style =
                    range.Style.Border.Left.Style =
                        range.Style.Border.Right.Style =
                            range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            // Create SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                FileName = $"Отчет_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            // Show dialog and save file
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    package.SaveAs(fileStream);
                }

                new DialogWindow("Успех", "Экспорт успешно завершен!").ShowDialog();
            }
        }
        catch (Exception ex)
        {
            new DialogWindow("Ошибка", "Ошибка при экспорте").ShowDialog();
            Log.Error($"Export error {ex.Message}", ex);
        }
    }
}