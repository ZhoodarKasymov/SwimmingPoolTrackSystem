using System.Windows;

namespace SwimmingTrackSystem.Windows;

public partial class PasswordDialog : Window
{
    public string EnteredPassword { get; private set; }

    public PasswordDialog()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        EnteredPassword = passwordBox.Password;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}