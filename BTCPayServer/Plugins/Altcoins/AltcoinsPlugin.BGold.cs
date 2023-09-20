using BTCPayServer.Hosting;
using BTCPayServer.Payments;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;

namespace BTCPayServer.Plugins.Altcoins;

public partial class AltcoinsPlugin
{
    public void InitBGold(IServiceCollection services)
    {
        var nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("BTG");
        var network = Add(new BTCPayNetwork()
        {
            CryptoCode = nbxplorerNetwork.CryptoCode,
            DisplayName = "BGold",
            BlockExplorerLink = NetworkType == ChainName.Mainnet ? "https://btgexplorer.com/tx/{0}" : "https://testnet.btgexplorer.com/tx/{0}",
            NBXplorerNetwork = nbxplorerNetwork,
            DefaultRateRules = new[]
            {
                "BTG_X = BTG_BTC * BTC_X",
                "BTG_BTC = gate(BTG_BTC)",
            },
            CryptoImagePath = "imlegacy/btg.svg",
            LightningImagePath = "imlegacy/btg-lightning.svg",
            DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
            CoinType = NetworkType == ChainName.Mainnet ? new KeyPath("156'") : new KeyPath("1'")
        });
        services.AddTransactionLinkProvider(new PaymentMethodId(nbxplorerNetwork.CryptoCode, PaymentTypes.BTCLike), new NetworkTransactionLinkProvider(network));
    }
}
