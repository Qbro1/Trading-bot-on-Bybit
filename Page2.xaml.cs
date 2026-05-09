namespace BybitApp1;

public partial class Page2 : ContentPage
{
	public Page2()
	{
		InitializeComponent();

		BindingContext = new TradeViewModel();

    }
}