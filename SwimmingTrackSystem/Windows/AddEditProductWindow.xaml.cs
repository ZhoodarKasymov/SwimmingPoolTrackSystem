using System.Windows;
using System.Windows.Documents;
using GolfClubSystem.Data;
using SwimmingTrackSystem.Models;

namespace SwimmingTrackSystem.Windows;

public enum ActionType
{
    Add,
    Edit
}

public class Item
{
    public string Name { get; set; }
    public string Id { get; set; }
}

public partial class AddEditProductWindow : Window
{
    public Product Product { get; set; }
    public ActionType ActionType { get; set; }
    private readonly UnitOfWork _unitOfWork = new();

    public List<Item> Times { get; set; } =
    [
        new()
        {
            Name = "12 Часов",
            Id = "12hour"
        },
        new()
        {
            Name = "24 Часа",
            Id = "24hour"
        },
        new()
        {
            Name = "Неделя",
            Id = "week"
        },
        new()
        {
            Name = "Месяц",
            Id = "month"
        },
    ];

    public AddEditProductWindow(Product? product)
    {
        InitializeComponent();

        if (product is not null)
        {
            Product = product;
            ActionType = ActionType.Edit;
        }
        else
        {
            Product = new Product();
            ActionType = ActionType.Add;
        }

        DataContext = this;
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Product.Time))
        {
            new DialogWindow("Ошибка", "Выберите время пропуска!").ShowDialog();
            return;
        }
        
        switch (ActionType)
        {
            case ActionType.Add:
                await _unitOfWork.ProductRepository.AddAsync(Product);
                break;
            case ActionType.Edit:
            {
                var currentProduct = _unitOfWork.ProductRepository.GetAll()
                    .FirstOrDefault(w => w.Id == Product.Id);

                if (currentProduct is not null)
                {
                    currentProduct.ProductName = Product.ProductName;
                    currentProduct.Price = Product.Price;
                    currentProduct.Time = Product.Time;
                    await _unitOfWork.ProductRepository.UpdateAsync(currentProduct);
                }

                break;
            }
        }

        Close();
    }
}