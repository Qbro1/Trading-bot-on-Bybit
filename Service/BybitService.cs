using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BybitApp1.Service
{
  public  class BybitService
    {

        private const  string ApiKey  = "";
        private const string ApiSecret  = "";

        private  string BaseUrl = "https://api-demo.bybit.com";

        private readonly HttpClient _httpClient = new HttpClient();



        public async Task<string> GetWalletBalanceAsync()   
        {
            try
            {
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                string recvWindow = "5000";
                string queryString = "accountType=UNIFIED&coin=USDT";

                
                string signature = GenerateSignature(timestamp, recvWindow, queryString);

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v5/account/wallet-balance?{queryString}");
                request.Headers.Add("X-BAPI-API-KEY", ApiKey);
                request.Headers.Add("X-BAPI-SIGN", signature);
                request.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
                request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                if (json["retCode"]?.ToString() == "0")
                {
                    var balance = json["result"]?["list"]?[0]?["totalEquity"]?.ToString();
                    return $"{balance} USDT";
                }
                return $"Ошибка: {json["retMsg"]}";
            }
            catch (Exception ex) { return $"Ошибка сети: {ex.Message}"; }
        }

        private string GenerateSignature(string timestamp, string recvWindow, string query)
        {
            string rawData = timestamp + ApiKey + recvWindow + query; 
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://api-demo.bybit.com") };
        private string _apiKey = "";
        private string _apiSecret = "";


        public async Task<List<Candle>> GetLines(string symbol, string interval, int limit)
        {
            var url = $"{BaseUrl}/v5/market/kline?category=linear&symbol={symbol}&interval={interval}&limit={limit}";


            var response = await httpClient.GetFromJsonAsync<BybitResponse<KlineData>>(url);



            return response.Result.List.Select(x => new Candle
            {
                High = decimal.Parse(x[2],
                CultureInfo.InvariantCulture),
                Low = decimal.Parse(x[3],
                CultureInfo.InvariantCulture),
                Close = decimal.Parse(x[4],
                CultureInfo.InvariantCulture)
            }).Reverse().ToList();
        }


        public async Task PlaceOrder(string symbol, string side, decimal qty)
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var recvWindow = "5000";

            
            var payload = new
            {
                category = "linear",
                symbol = symbol,
                side = side,
                orderType = "Market", 
                qty = qty.ToString()
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            
            var signature = CreateSignature(timestamp, recvWindow, jsonPayload);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-demo.bybit.com/v5/order/create");
            request.Headers.Add("X-BAPI-API-KEY", _apiKey);
            request.Headers.Add("X-BAPI-SIGN", signature);
            request.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
            request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            await _httpClient.SendAsync(request);
        }


        public (double Macd, double Signal, double Histogram) CalculateMacd(List<Candle> candles)
        {
            var closePrices = candles.Select(c => (double)c.Close).ToList();

            // Рассчитываем быстрый и медленный EMA
            var ema12 = CalculateEmaList(closePrices, 12);
            var ema26 = CalculateEmaList(closePrices, 26);

            // Рассчитываем MACD линию для всех точек
            var macdLineValues = new List<double>();
            int startIndex = Math.Max(0, ema12.Count - ema26.Count);
            for (int i = startIndex; i < ema12.Count; i++)
            {
                macdLineValues.Add(ema12[i] - ema26[i - startIndex]);
            }

            // Текущее значение MACD линии
            double currentMacdLine = macdLineValues.Last();

            // Рассчитываем сигнальную линию как EMA9 от MACD линии
            var signalLineValues = CalculateEmaList(macdLineValues, 9);
            double currentSignalLine = signalLineValues.Last();

            // Гистограмма = MACD линия - Сигнальная линия
            double histogram = currentMacdLine - currentSignalLine;

            return (currentMacdLine, currentSignalLine, histogram);
        }

        // Вспомогательный метод для расчета EMA списка
        private List<double> CalculateEmaList(List<double> prices, int period)
        {
            if (prices == null || prices.Count == 0 || period <= 0)
                return new List<double>();

            var emaValues = new List<double>();
            double multiplier = 2.0 / (period + 1);

            // Начинаем с SMA для первого значения
            double ema = prices.Take(period).Average();
            emaValues.Add(ema);

            // Рассчитываем EMA для остальных значений
            for (int i = period; i < prices.Count; i++)
            {
                ema = (prices[i] - ema) * multiplier + ema;
                emaValues.Add(ema);
            }

            return emaValues;
        }



        public async Task<string> SetLeverage(string symbol, int leverage)
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var recvWindow = "5000";

            
            var payload = new
            {
                category = "linear",
                symbol = symbol,
                buyLeverage = leverage.ToString(),
                sellLeverage = leverage.ToString()
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            var signature = CreateSignature(timestamp, recvWindow, jsonPayload);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-demo.bybit.com/v5/position/set-leverage");

            request.Headers.Add("X-BAPI-API-KEY", _apiKey);
            request.Headers.Add("X-BAPI-SIGN", signature);
            request.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
            request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);

            request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return content; // Здесь придет JSON с результатом (успех или ошибка)
        }


        private string CreateSignature(string ts, string window, string body)
        {
            var rawData = ts + _apiKey + window + body;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        internal async Task<IEnumerable<object>> Getlines(string v1, string v2, int v3)
        {
            throw new NotImplementedException();
        }
    }

    
    public class Candle { public decimal High { get; set; } public decimal Low { get; set; } public decimal Close { get; set; } }
    public class BybitResponse<T> { public T Result { get; set; } }
    public class KlineData { public List<List<string>> List { get; set; } }


}

