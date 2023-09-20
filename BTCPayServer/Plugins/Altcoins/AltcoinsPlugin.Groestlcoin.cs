using BTCPayServer.Hosting;
using BTCPayServer.Payments;
using BTCPayServer.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;

namespace BTCPayServer.Plugins.Altcoins;

public partial class AltcoinsPlugin
{
    public void InitGroestlcoin(IServiceCollection services)
    {
        var nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("GRS");
        var network = Add(new BTCPayNetwork()
        {
            CryptoCode = nbxplorerNetwork.CryptoCode,
            DisplayName = "Groestlcoin",
            BlockExplorerLink = NetworkType == ChainName.Mainnet
                ? "https://chainz.cryptoid.info/grs/tx.dws?{0}.htm"
                : "https://chainz.cryptoid.info/grs-test/tx.dws?{0}.htm",
            NBXplorerNetwork = nbxplorerNetwork,
            DefaultRateRules = new[]
            {
                    "GRS_X = GRS_BTC * BTC_X",
                    "GRS_BTC = bittrex(GRS_BTC)"
                },
            CryptoImagePath = "imlegacy/groestlcoin.png",
            LightningImagePath = "imlegacy/groestlcoin-lightning.svg",
            DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
            CoinType = NetworkType == ChainName.Mainnet ? new KeyPath("17'") : new KeyPath("1'"),
            SupportRBF = true,
            SupportPayJoin = true,
            VaultSupported = true
        });
        services.AddTransactionLinkProvider(new PaymentMethodId(nbxplorerNetwork.CryptoCode, PaymentTypes.BTCLike), new NetworkTransactionLinkProvider(network));
    }
}

