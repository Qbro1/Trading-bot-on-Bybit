using BybitApp1.ViewModels;

namespace BybitApp1
{
    public partial class MainPage : ContentPage
    {
      

        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }

       
    }
}
