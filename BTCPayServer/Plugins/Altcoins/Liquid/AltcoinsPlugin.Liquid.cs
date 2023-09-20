using BTCPayServer.Hosting;
using BTCPayServer.Payments;
using BTCPayServer.Services;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.Altcoins;
using NBitcoin.Altcoins.Elements;
using NBXplorer;

namespace BTCPayServer.Plugins.Altcoins;
public partial class AltcoinsPlugin
{
    public void InitLiquid(IServiceCollection services)
    {
        var nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("LBTC");
        if (nbxplorerNetwork is null)
            return;
        var network = Add(new ElementsBTCPayNetwork()
        {
            AssetId = NetworkType == ChainName.Mainnet ? ElementsParams<Liquid>.PeggedAssetId : ElementsParams<Liquid.LiquidRegtest>.PeggedAssetId,
            CryptoCode = "LBTC",
            NetworkCryptoCode = "LBTC",
            DisplayName = "Liquid Bitcoin",
            DefaultRateRules = new[]
            {
                    "LBTC_X = LBTC_BTC * BTC_X",
                    "LBTC_BTC = 1",
                },
            BlockExplorerLink = NetworkType == ChainName.Mainnet ? "https://liquid.network/tx/{0}" : "https://liquid.network/testnet/tx/{0}",
            NBXplorerNetwork = nbxplorerNetwork,
            CryptoImagePath = "imlegacy/liquid.png",
            DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
            CoinType = NetworkType == ChainName.Mainnet ? new KeyPath("1776'") : new KeyPath("1'"),
            SupportRBF = true
        });
        services.AddTransactionLinkProvider(new PaymentMethodId(nbxplorerNetwork.CryptoCode, PaymentTypes.BTCLike), new NetworkTransactionLinkProvider(network));
    }
}
