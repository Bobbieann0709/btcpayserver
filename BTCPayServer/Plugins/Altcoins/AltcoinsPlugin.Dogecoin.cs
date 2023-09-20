using BTCPayServer.Hosting;
using BTCPayServer.Payments;
using BTCPayServer.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer.Plugins.Altcoins;

public partial class AltcoinsPlugin
{
    public void InitDogecoin(IServiceCollection services)
    {
        var nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("DOGE");
        var network = Add(new BTCPayNetwork()
        {
            CryptoCode = nbxplorerNetwork.CryptoCode,
            DisplayName = "Dogecoin",
            BlockExplorerLink = NetworkType == ChainName.Mainnet ? "https://dogechain.info/tx/{0}" : "https://dogechain.info/tx/{0}",
            NBXplorerNetwork = nbxplorerNetwork,
            DefaultRateRules = new[]
            {
                                "DOGE_X = DOGE_BTC * BTC_X",
                                "DOGE_BTC = bittrex(DOGE_BTC)"
                },
            CryptoImagePath = "imlegacy/dogecoin.png",
            DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
            CoinType = NetworkType == ChainName.Mainnet ? new KeyPath("3'") : new KeyPath("1'")
        });
        services.AddTransactionLinkProvider(new PaymentMethodId(nbxplorerNetwork.CryptoCode, PaymentTypes.BTCLike), new NetworkTransactionLinkProvider(network));
    }
}

