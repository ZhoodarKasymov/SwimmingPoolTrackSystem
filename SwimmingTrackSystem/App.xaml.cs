using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using GolfClubSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using SwimmingTrackSystem.Services;
using SwimmingTrackSystem.Windows;
using Transaction = SwimmingTrackSystem.Models.Transaction;

namespace SwimmingTrackSystem;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IConfigurationRoot _configuration;
    private CancellationTokenSource _cts;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        CultureInfo culture = new CultureInfo("ru-RU");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag))
        );

        // Инициализация логгера
        Logger.Initialize();

        // Глобальная обработка ошибок
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        StartBackgroundTask();
    }

    private void StartBackgroundTask()
    {
        _cts = new CancellationTokenSource();

        // Existing background task (every 60 seconds)
        Task.Run(() => BackgroundTask(_cts.Token), _cts.Token);

        // New daily task (runs at 00:00 every day)
        Task.Run(() => DailyMidnightTask(_cts.Token), _cts.Token);
    }

    private async Task DailyMidnightTask(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next midnight
                var now = DateTime.Now;
                var midnight = DateTime.Today.AddDays(1);
                var timeToMidnight = midnight - now;
                
                if (timeToMidnight.TotalMilliseconds < 0)
                {
                    // If it's already past midnight, set to next day's midnight
                    timeToMidnight = TimeSpan.FromDays(1) + timeToMidnight;
                }
                
                // Wait until midnight
                await Task.Delay(timeToMidnight, token);

                // Execute the daily task
                await ExecuteDailyTask();

                // Wait 24 hours for the next execution
                await Task.Delay(TimeSpan.FromDays(1), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in DailyMidnightTask");
                Console.WriteLine($"Ошибка в ежедневной задаче: {ex.Message}");
                // Optionally, wait a short time before retrying to avoid tight loop on failure
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
        }
    }

    private async Task ExecuteDailyTask()
    {
        try
        {
            Log.Information("Daily task executed at {Time}", DateTime.Now);

            using var unitOfWork = new UnitOfWork();
            var setting = unitOfWork.SettingRepository.GetAll().SingleOrDefault();

            if (setting is not null)
            {
                using var terminalService = new TerminalService(setting.Login, setting.Password);

                // Step 1: Fetch expired transactions
                var expiredTransactions = await unitOfWork.TransactionRepository
                    .GetAll(true)
                    .Where(t => t.DeletedAt == null && t.ExpireDate < DateTime.Now)
                    .ToListAsync();

                if (expiredTransactions.Any())
                {
                    // Step 2: Prepare EmployeeNoList for deletion
                    var expiredIds = expiredTransactions
                        .Select(t => new EmployeeNoList
                        {
                            EmployeeNo = t.Id.ToString()
                        })
                        .ToArray();

                    var request = new UserInfoDeleteRequest
                    {
                        UserInfoDelCond = new UserInfoDelCond
                        {
                            EmployeeNoList = expiredIds
                        }
                    };

                    // Step 3: Delete from terminals
                    await terminalService.DeleteUsersAsync(request, setting.EnterIp);
                    await terminalService.DeleteUsersAsync(request, setting.ExitIp);

                    // Step 4: Update DeletedAt for the expired transactions
                    foreach (var transaction in expiredTransactions)
                    {
                        transaction.DeletedAt = DateTime.Now;
                    }

                    // Step 5: Save changes to the database
                    await unitOfWork.TransactionRepository.UpdateRangeAsync(expiredTransactions);

                    Log.Information("Successfully deleted {Count} expired transactions and marked them as deleted.", expiredTransactions.Count);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing daily task");
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private async Task BackgroundTask(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var unitOfWork = new UnitOfWork();
                var setting = unitOfWork.SettingRepository.GetAll().SingleOrDefault();
                
                if (setting is not null)
                {
                    using var terminalService = new TerminalService(setting.Login, setting.Password);

                    await ProcessTerminal(setting.EnterIp, unitOfWork, terminalService, 1);
                    await ProcessTerminal(setting.ExitIp, unitOfWork, terminalService, 2);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(60), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled domain exception occurred.");
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    private async Task ProcessTerminal(string ipAddress, UnitOfWork unitOfWork, TerminalService terminalService,
        int status)
    {
        var nowDate = DateTime.Now;
        var ip = ipAddress;

        var histories = await terminalService.GetFilteredUserHistoriesAsync(ip, nowDate.ToString("yyyy-MM-dd"));
        if (histories.Count == 0) return;

        // Группируем события по сотрудникам
        var groupedHistories = histories
            .Where(h => !string.IsNullOrEmpty(h.employeeNoString) && !string.IsNullOrEmpty(h.time))
            .GroupBy(h => int.Parse(h.employeeNoString))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => DateTimeOffset.Parse(h.time).DateTime).ToList());

        // Получаем данные сотрудников из базы
        var workerIds = groupedHistories.Keys.ToList();
        var transactions = await unitOfWork.TransactionRepository
            .GetAll(true)
            .Where(w => workerIds.Contains(w.Id) && w.ExpireDate >= nowDate.Date)
            .ToListAsync();

        var updatedHistories = new List<Transaction>();

        foreach (var transaction in transactions)
        {
            if (!groupedHistories.TryGetValue(transaction.Id, out var workerHistories)) continue;

            var latestHistory = workerHistories.First();
            var terminalHistoryDateTime = DateTimeOffset.Parse(latestHistory.time).DateTime;

            // Обновляем существующую запись
            if (transaction.TrackDate >= terminalHistoryDateTime) continue;
            transaction.Status = status;
            transaction.TrackDate = terminalHistoryDateTime;
            updatedHistories.Add(transaction);
        }

        if (updatedHistories.Any())
        {
            await unitOfWork.TransactionRepository.UpdateRangeAsync(updatedHistories);
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Error(ex, "Unhandled domain exception occurred.");
        ShowErrorMessage("Произошла критическая ошибка. Приложение будет закрыто.");
    }

    private void App_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled dispatcher exception occurred.");
        ShowErrorMessage("Произошла ошибка. Попробуйте снова.");
        e.Handled = true; // Не завершать приложение
    }

    private void ShowErrorMessage(string message)
    {
        new DialogWindow("Ошибка", message).ShowDialog();
    }
}