using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Grocery.App.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private ObservableCollection<Product> _allProducts;
        private ObservableCollection<Product> _products;
        private string _searchText;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    SearchProductsCommand.Execute(null);
                }
            }
        }

        public ICommand SearchProductsCommand { get; }

        public ProductViewModel(IProductService productService)
        {
            _productService = productService;
            LoadProducts();
            SearchProductsCommand = new Command(ExecuteSearchProductsCommand);
        }

        private void LoadProducts()
        {
            _allProducts = new ObservableCollection<Product>(_productService.GetAll());
            Products = new ObservableCollection<Product>(_allProducts);
        }

        private void ExecuteSearchProductsCommand()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Products = new ObservableCollection<Product>(_allProducts);
            }
            else
            {
                Products = new ObservableCollection<Product>(
                    _allProducts.Where(p => p.Name.ToLower().Contains(SearchText.ToLower()))
                );
            }
        }
    }
}