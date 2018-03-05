using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace TurtleBot.Services
{
    public class WalletService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly string _walletEndpoint;
        private readonly string _rpcPassword;

        private int _requestId;

        public WalletService(ILoggerFactory loggerFactory, ConfigModule config)
        {
            _logger = loggerFactory.CreateLogger("wallet");
            _client = new HttpClient();
            _walletEndpoint = $"http://{config["walletdServiceAddress"]}:{config["walletdServicePort"]}/json_rpc";
            _rpcPassword = config["walletdRPCPassword"];

            _requestId = 0;
        }

        public async Task<bool> CheckAddress(string address)
        {
            var response = await SendRPCRequest("getBalance", $"{{\"address\":\"{address}\"}}");

            // If there is no |error| value, the address is a bot address, so it is valid.
            if (!response.TryGetValue("error", out var errorToken)) return true;

            var applicationCode = (int)errorToken["data"]["application_code"];

            // Application code 7 means bad address.
            return applicationCode != 7;
        }

        public async Task<TurtleWallet> GetFirstAddress()
        {
            var response = await SendRPCRequest("getAddresses");
            var firstAddress = (string)response["result"]["addresses"][0];

            return await TurtleWallet.FromString(this, firstAddress);
        }

        public async Task<long> GetBalance(TurtleWallet address)
        {
            var response = await SendRPCRequest("getBalance", $"{{\"address\":\"{address.Address}\"}}");

            return (long)response["result"]["availableBalance"];
        }

        public async Task<string> SendToMany(long amountPerWallet, long fee, IEnumerable<TurtleWallet> wallets)
        {
            var transfersString = wallets.Aggregate("[", (current, wallet) => current + $"{{\"amount\":{amountPerWallet}, \"address\":\"{wallet.Address}\"}},");
            transfersString = transfersString.Remove(transfersString.Length - 1);
            transfersString += "]";
            
            var response = await SendRPCRequest("sendTransaction", $"{{\"fee\":{fee}, \"anonymity\":{0}, \"transfers\":{transfersString}}}");

            return (string)response["result"]["transactionHash"];
        }

        private async Task<JObject> SendRPCRequest(string method, string parameters = "{}")
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _walletEndpoint);
            var content = $"{{ \"jsonrpc\":\"2.0\", \"method\":\"{method}\", \"params\":{parameters}, \"password\":\"{_rpcPassword}\", \"id\":{_requestId++} }}";
            requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{(int) response.StatusCode} {response.ReasonPhrase}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseString);
        }
    }
}
