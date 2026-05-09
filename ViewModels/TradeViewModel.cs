using BybitApp1.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Skender.Stock.Indicators;




public partial class TradeViewModel : ObservableObject
{

    


    private readonly BybitService _bybitService = new BybitService();

    private SQLiteAsyncConnection _db;

    
    [ObservableProperty]
    private ObservableCollection<string> _botLogs = new();

    private void AddLog(string message)
    {
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BotLogs.Insert(0, $"{DateTime.Now:HH:mm:ss} | {message}");
            
            if (BotLogs.Count > 20) BotLogs.RemoveAt(20);
        });
    }

   
     

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyPropertyChangedFor(nameof(ButtonColor))]
    private bool _botRunning;

    private CancellationTokenSource _cts;
    private bool _hasPosition = false;
    private int _positionType = 0;
    private decimal _entryPrice = 0;


    public string ButtonText => BotRunning ? "ОСТАНОВИТЬ" : "ТОРГОВАТЬ";
    public Color ButtonColor => BotRunning ? Colors.DarkBlue : Colors.DeepSkyBlue;



    


    [RelayCommand]
    private async Task ToggleBot()
    {
        if (!BotRunning)
        {

            _db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "bot.db"));
            await _db.CreateTableAsync<BotState>();
           //await _db.DeleteAllAsync<BotState>(); // this 
            var state = await _db.Table<BotState>().FirstOrDefaultAsync();

            
            if (state != null && state.HasPosition == true)
            {
                _hasPosition = state.HasPosition;
                _entryPrice = state.EntryPrice;
                _positionType = state.PositionType;
                AddLog($"[БД] Вспомнил активную сделку: {(_positionType == 1 ? "Long" : "Short")}");
            }
            else
            {
               
                _hasPosition = false;
                _positionType = 0;
                AddLog("Активных сделок в базе не найдено. Начинаю с нуля.");
            }


            BotRunning = true;
            _cts = new CancellationTokenSource();
            _ = Task.Run(async () => await StartTradingLogic(_cts.Token));
        }
        else
        {
            BotRunning = false;
            _cts?.Cancel(); 
        }
    }

   
    public async Task SetLeverage(string symbol, int leverage)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
        var payload = new { category = "linear", symbol = symbol, buyLeverage = leverage.ToString(), sellLeverage = leverage.ToString() };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        
    }

    private async Task StartTradingLogic(CancellationToken token)
    {
        AddLog("Настройка плеча...");

        // Выставляем, например, 10-е плечо
        var levResult = await _bybitService.SetLeverage("XRPUSDT", 25);
        AddLog("Плечо установлено: " + levResult);
        {

        AddLog("Запуск мониторинга XRPUSDT...");

            

            while (BotRunning)
            {
                try
                {
                    
                    var candles = await _bybitService.GetLines("XRPUSDT", "5", 20);
                    decimal currentPrice = candles.Last().Close;
                    decimal high20 = candles.Max(c => c.High);
                    decimal low20 = candles.Min(c => c.Low); 

                    AddLog($"Цена: {currentPrice} | Хай: {high20} | Лоу: {low20}");

                    // rsi
                    var quotes = candles.Select(c => new Quote
                    {

                        Date = DateTime.UtcNow,
                        High = c.High,
                        Low = c.Low,
                        Close = c.Close

                    });

                    var rsiResults = quotes.GetRsi(14);
                    var lastRsi = rsiResults.Last();

                    double rsi = lastRsi.Rsi ?? 50;

                    var macd = _bybitService.CalculateMacd(candles);

                    if (!_hasPosition)
                    {
                        // --- 
                        if (currentPrice >= high20 && rsi > 50 && macd.Histogram > 0 ) 
                        {
                            _hasPosition = true;
                            _positionType = 1; 
                            _entryPrice = currentPrice;

                            await _bybitService.PlaceOrder("XRPUSDT", "Buy", 4);
                            
                            await _db.InsertOrReplaceAsync(new BotState { Id = 1, HasPosition = true, EntryPrice = _entryPrice, PositionType = 1 });

                            AddLog($" ОТКРЫЛ ЛОНГ по: {_entryPrice}");
                        }
                        else if (currentPrice <= low20 && rsi < 50 && macd.Histogram < 0 ) 
                        {
                            _hasPosition = true;
                            _positionType = 2; 
                            _entryPrice = currentPrice;

                            await _bybitService.PlaceOrder("XRPUSDT", "Sell", 4 ); 

                            await _db.InsertOrReplaceAsync(new BotState { Id = 1, HasPosition = true, EntryPrice = _entryPrice, PositionType = 2 });

                            AddLog($" ОТКРЫЛ ШОРТ по: {_entryPrice}");
                        }
                    }
                    else
                    {
                      
                        if (_positionType == 1) 
                        {
                            

                            if (currentPrice >= _entryPrice * 1.02m || currentPrice <= _entryPrice * 1.99m)
                            {
                                await _bybitService.PlaceOrder("XRPUSDT", "Sell", 4);
                                await ResetState(); 
                                AddLog("Лонг закрыт по цели/стопу.");
                            }
                        }
                        else if (_positionType == 2) 
                        {
                            
                           
                            if (currentPrice <= _entryPrice * 0.98m || currentPrice >= _entryPrice * 1.01m)

                            {
                                await _bybitService.PlaceOrder("XRPUSDT", "Buy", 4); 
                                await ResetState();
                                AddLog("Шорт закрыт по цели/стопу.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"Ошибка: {ex.Message}");
                }

                await Task.Delay(10000); 
            }

            AddLog("Бот остановлен.");
    }






        //while (BotRunning)
        //{
        //    try
        //    {
        //        //стратегия
        //        var candles = await _bybitService.GetKlines("XRPUSDT", "5", 20);
        //        var currentPrice = candles.Last().Close;
        //        var high20 = candles.Max(c => c.High);

        //        if (currentPrice >= high20)
        //        {
        //            await _bybitService.PlaceOrder("XRPUSDT", "Buy", 4);
                    
        //            BotRunning = false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
                
        //        System.Diagnostics.Debug.WriteLine($"Ошибка связи: {ex.Message}");
        //    }

        //    await Task.Delay(10000); 
        //}
    }



    private async Task ResetState()
    {
        _hasPosition = false;
        _positionType = 0;
        _entryPrice = 0;
        // Обнуляем данные в базе данных, чтобы при перезагрузке бот не думал, что он в сделке
        await _db.InsertOrReplaceAsync(new BotState
        {
            Id = 1,
            HasPosition = false,
            EntryPrice = 0,
            PositionType = 0
        });
    }

}