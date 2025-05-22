using EverSneaks.MAUI.Evergine;
using EverSneaks.Services;
using System.Windows.Input;

namespace EverSneaks.MAUI.ViewModels
{
    public class SneakersDetailsViewModel
    {
        public ICommand ColorCommand { get; set; }

        private EvergineView evergineView;
        private ControllerService controllerService;

        public SneakersDetailsViewModel(EvergineView evergineView)
        {
            this.evergineView = evergineView;
            this.controllerService = this.evergineView.Application.Container.Resolve<ControllerService>();
        }
    }
}
