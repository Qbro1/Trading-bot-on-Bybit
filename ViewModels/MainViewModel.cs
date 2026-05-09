using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BybitApp1.Service;



namespace BybitApp1.ViewModels
{
   public partial class MainViewModel : INotifyPropertyChanged
    {

       


        [RelayCommand]
        private void BtnText()
        {
            Shell.Current.DisplayAlert("Уведомление", "Бот используется исключительно в позновательных целях", "Я понял");
        }


        private readonly BybitService _bybitService = new BybitService();
        private string _balance = "Нажми для проверки";
        private bool _isBusy;

        public string Balance
        {
            get => _balance;
            set { _balance = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); (GetBalanceCommand as Command)?.ChangeCanExecute(); }
        }

        public ICommand GetBalanceCommand { get; }

        public MainViewModel()
        {
            GetBalanceCommand = new Command(async () => await ExecuteGetBalance(), () => !IsBusy);
        }

        async Task ExecuteGetBalance()
        {
            IsBusy = true;
            Balance = "Связываюсь с Bybit...";
            Balance = await _bybitService.GetWalletBalanceAsync();
            IsBusy = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

      

        

    }
}
