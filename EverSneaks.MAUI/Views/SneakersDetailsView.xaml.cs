using EverSneaks.MAUI.ViewModels;

namespace EverSneaks.MAUI.Views;

public partial class SneakersDetailsView : ContentPage
{
    private MyApplication evergineApplication;

    public SneakersDetailsView()
	{
		InitializeComponent();

        this.evergineApplication = new MyApplication();
        this.evergineView.Application = this.evergineApplication;
        this.BindingContext = new SneakersDetailsViewModel(this.evergineView);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
    }

    private void OnColorTapped(object sender, TappedEventArgs args)
    {
        this.HideEveryCheckImage();
        var grid = (Grid)sender;
        var checkImage = (Image)grid.Children.Single(item => item is Image);
        checkImage.IsVisible = true;
    }

    private void HideEveryCheckImage()
    {
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private bool isLoading = false;

    private class ModelToLoad
    {
        public string Description { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; } 
    }
    

    private async void Button_OnClicked(object sender, EventArgs e)
    {
        if (isLoading)
        {
            return;
        }

        try
        {

            List<ModelToLoad> models = new List<ModelToLoad>()
            {
                new ModelToLoad()
                {
                    Description = "Elphinestone (Polycam - Default scene)",
                    FileName = "elphinestone.glb",
                    Url = "https://redpointdemo.blob.core.windows.net/data/elphinestone_fixed.glb"
                },
                new ModelToLoad()
                {
                    Description = "Elphinestone Optmised (Polycam, gltf-transform optimize draco)",
                    FileName = "elphinestone_optimised.glb",
                    Url = "https://redpointdemo.blob.core.windows.net/data/elphinestone_optimised.glb"
                },
                new ModelToLoad()
                {
                    Description = "Gibraltar (Polycam - No default scene)",
                    FileName = "gibraltar.glb",
                    Url = "https://redpointdemo.blob.core.windows.net/data/gibraltar.glb"
                },
                new ModelToLoad()
                {
                    Description = "Helmet (GLB Official Sample)",
                    FileName = "helmet.glb",
                    Url = "https://redpointdemo.blob.core.windows.net/data/DamagedHelmet.glb"
                },
                new ModelToLoad()
                {
                    Description = "Toy Car (GLB Official Sample)",
                    FileName = "ToyCar.glb",
                    Url = "https://redpointdemo.blob.core.windows.net/data/ToyCar.glb"
                },
                new ModelToLoad()
                {
                    Description = "Air Jordan (Evergine EverSneak Sample)",
                    FileName = "airjordan.glb",
                    Url = "https://redpointdemo.blob.core.windows.net/data/AirJordan.glb"
                },
            };
            
            var choice = await DisplayActionSheet("Please select a model to load from blob storage", "Cancel", null, models.Select(item => item.Description).ToArray());
            
            var model = models.FirstOrDefault(item => item.Description == choice);
            if (model == null)
            {
                return;
            }

            isLoading = true;
            
            const string key = "airjordan.glb";
            
            Console.WriteLine($"Loading {model.Description}");

            var fileName = model.FileName;
            var url = model.Url;
            
            string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            if (File.Exists(filePath))
            {
                
                var shouldDelete = await DisplayAlert("Model Already Downloaded", "Do you want to delete the existing local file and redownload it?", "Delete and re-download", "Continue with existing file");
                if (shouldDelete)
                {
                    File.Delete(filePath);
                }
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Downloading {model.Description} from {url} to {filePath}");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, content);
                }
                Console.WriteLine($"Download complete! ✅");
            }

            Console.WriteLine($"Loading model into Evergine scene");
            await evergineApplication.LoadGlb(filePath);

        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            isLoading = false;
        }
    }
}