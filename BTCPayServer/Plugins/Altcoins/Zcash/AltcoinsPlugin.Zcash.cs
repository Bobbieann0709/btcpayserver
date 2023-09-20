#nullable enable
using BTCPayServer.Services;
using System.Globalization;
using System.Linq;
using NBitcoin;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using BTCPayServer.Hosting;
using BTCPayServer.Payments;

namespace BTCPayServer.Plugins.Altcoins;

public partial class AltcoinsPlugin
{
    // Change this if you want another zcash coin
    public void InitZcash(IServiceCollection services)
    {
        var network = new ZcashLikeSpecificBtcPayNetwork()
        {
            CryptoCode = "ZEC",
            DisplayName = "Zcash",
            Divisibility = 8,
            BlockExplorerLink =
                NetworkType == ChainName.Mainnet
                    ? "https://www.exploreZcash.com/transaction/{0}"
                    : "https://testnet.xmrchain.net/tx/{0}",
            DefaultRateRules = new[]
            {
                    "ZEC_X = ZEC_BTC * BTC_X",
                    "ZEC_BTC = kraken(ZEC_BTC)"
                },
            CryptoImagePath = "/imlegacy/zcash.png",
            UriScheme = "zcash"
        };
        Add(network);
        services.AddTransactionLinkProvider(new Payments.PaymentMethodId("ZEC", PaymentTypes.BTCLike), new SimpleTransactionLinkProvider(network));
    }
    class SimpleTransactionLinkProvider : TransactionLinkProvider
    {
        public SimpleTransactionLinkProvider(BTCPayNetworkBase network)
        {
            Network = network;
        }

        public BTCPayNetworkBase Network { get; }

        public string? GetTransactionLink(string txId)
        {
            return string.Format(CultureInfo.InvariantCulture, Network.BlockExplorerLink, txId);
        }
    }
}

