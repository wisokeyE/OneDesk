using Microsoft.Graph;

namespace OneDesk.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _counter = 0;

        private List<GraphServiceClient> _clients = [];


        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }


    }
}
