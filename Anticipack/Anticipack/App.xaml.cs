using Anticipack.Storage;
using Anticipack.Resources.Localization;

namespace Anticipack
{
    public partial class App : Application
    {
        private readonly IPackingRepository _packingRepository;

        public App(IPackingRepository packingRepository)
        {
            InitializeComponent();
            _packingRepository = packingRepository;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "Anticipack" };

            return window;
        }

        protected override async void OnStart()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            }

            if (status != PermissionStatus.Granted)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Current?.MainPage is not null)
                    {
                        await Current.MainPage.DisplayAlert(
                            Anticipack.Resources.Localization.AppResources.ResourceManager.GetString("StoragePermissionRequiredTitle") ?? "Permission Required",
                            Anticipack.Resources.Localization.AppResources.ResourceManager.GetString("StoragePermissionRequiredMessage") ?? "Storage permission is required to save and load your packing data. You can enable it later in system settings.",
                            Anticipack.Resources.Localization.AppResources.Confirm);
                    }
                });

                return;
            }

            await _packingRepository.InitializeAsync();

        }
    }
}
