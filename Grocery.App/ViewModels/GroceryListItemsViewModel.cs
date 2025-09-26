using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq; 

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];
        private List<Product> alleAvailableProducts = new();


        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);
        [ObservableProperty]
        string myMessage;

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id))
            {
                MyGroceryListItems.Add(item);
            }
            GetAvailableProducts();
            alleAvailableProducts = AvailableProducts.ToList();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            foreach (Product p in _productService.GetAll())
            {
                if (MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null && p.Stock > 0)
                {
                    AvailableProducts.Add(p);
                }
            }
            alleAvailableProducts = AvailableProducts.ToList();
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }

        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);

            var existingProductInAll = alleAvailableProducts.FirstOrDefault(p => p.Id == product.Id);
            if (existingProductInAll != null)
            {
                alleAvailableProducts.Remove(existingProductInAll);
            }
            
            FilterProducts(null); // Geen zoekterm, dus toon alles opnieuw minus het toegevoegde product

            OnGroceryListChanged(GroceryList); 
        }

        
        [RelayCommand]
        public void SearchProducts(string searchTerm)
        {
            FilterProducts(searchTerm);
        }

        
        private void FilterProducts(string searchTerm)
        {
            AvailableProducts.Clear(); // Maak de huidige lijst leeg

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // Als de zoekterm leeg is, voeg alle beschikbare producten toe
                foreach (var product in alleAvailableProducts)
                {
                    AvailableProducts.Add(product);
                }
            }
            else
            {
                // Filter de producten op basis van de zoekterm
                var filteredProducts = alleAvailableProducts
                                        .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        .ToList();
                foreach (var product in filteredProducts)
                {
                    AvailableProducts.Add(product);
                }
            }
        }


        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }

    }
}