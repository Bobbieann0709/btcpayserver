using BTCPayServer.Hosting;
using BTCPayServer.Payments;
using BTCPayServer.Services;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer.Plugins.Altcoins;

public partial class AltcoinsPlugin
{
    public void InitMonacoin(IServiceCollection services)
    {
        var nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("MONA");
        var network = Add(new BTCPayNetwork()
        {
            CryptoCode = nbxplorerNetwork.CryptoCode,
            DisplayName = "Monacoin",
            BlockExplorerLink = NetworkType == ChainName.Mainnet ? "https://mona.insight.monaco-ex.org/insight/tx/{0}" : "https://testnet-mona.insight.monaco-ex.org/insight/tx/{0}",
            NBXplorerNetwork = nbxplorerNetwork,
            DefaultRateRules = new[]
            {
                                "MONA_X = MONA_BTC * BTC_X",
                                "MONA_BTC = bittrex(MONA_BTC)"
                },
            CryptoImagePath = "imlegacy/monacoin.png",
            LightningImagePath = "imlegacy/mona-lightning.svg",
            DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
            CoinType = NetworkType == ChainName.Mainnet ? new KeyPath("22'") : new KeyPath("1'")
        });
        services.AddTransactionLinkProvider(new PaymentMethodId(nbxplorerNetwork.CryptoCode, PaymentTypes.BTCLike), new NetworkTransactionLinkProvider(network));
    }
}

