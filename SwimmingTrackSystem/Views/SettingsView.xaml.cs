using System.Windows;
using System.Windows.Controls;
using GolfClubSystem.Data;
using SwimmingTrackSystem.Models;
using SwimmingTrackSystem.Services;
using SwimmingTrackSystem.Windows;

namespace SwimmingTrackSystem.Views;

public partial class SettingsView : UserControl
{
    private readonly UnitOfWork _unitOfWork = new();
    public Setting Setting { get; set; }

    public SettingsView()
    {
        InitializeComponent();
        Setting = _unitOfWork.SettingRepository.GetAll().SingleOrDefault() ?? new Setting();
        DataContext = this;
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var passwordDialog = new PasswordDialog();
        var result = passwordDialog.ShowDialog();

        if (!result.HasValue || !result.Value) return;
        var enteredPassword = passwordDialog.EnteredPassword;

        if (enteredPassword != "admin123")
        {
            new DialogWindow("Ошибка", "Неверный пароль администратора!").ShowDialog();
            return;
        }

        var setting = _unitOfWork.SettingRepository.GetAll().SingleOrDefault();

        if (setting == null)
        {
            await _unitOfWork.SettingRepository.AddAsync(Setting);
        }
        else
        {
            setting.EnterIp = Setting.EnterIp;
            setting.ExitIp = Setting.ExitIp;
            setting.Login = Setting.Login;
            setting.Password = Setting.Password;
            setting.PosTerminalIp = Setting.PosTerminalIp;
            setting.RegNumber = Setting.RegNumber;
            await _unitOfWork.SettingRepository.UpdateAsync(setting);
        }
        
        using var terminal = new TerminalService(Setting.Login, Setting.Password);
        var request = new UserInfoDeleteRequest
        {
            UserInfoDelCond = new UserInfoDelCond
            {
                EmployeeNoList = []
            }
        };
        
        if (setting?.EnterIp != Setting.EnterIp)
        {
            await terminal.DeleteUsersAsync(request, Setting.EnterIp);
        }
        if (setting?.ExitIp != Setting.ExitIp)
        {
            await terminal.DeleteUsersAsync(request, Setting.ExitIp);
        }

        new DialogWindow("Успех", "Настройки сохранены успешно!").ShowDialog();
    }
}