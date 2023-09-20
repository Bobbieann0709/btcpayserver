using BTCPayServer.Hosting;
using BTCPayServer.Payments;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;

namespace BTCPayServer.Plugins.Altcoins;

public partial class AltcoinsPlugin
{
    public void InitMonero(IServiceCollection services)
    {
        var network = new MoneroLikeSpecificBtcPayNetwork()
        {
            CryptoCode = "XMR",
            DisplayName = "Monero",
            Divisibility = 12,
            BlockExplorerLink =
                NetworkType == ChainName.Mainnet
                    ? "https://www.exploremonero.com/transaction/{0}"
                    : "https://testnet.xmrchain.net/tx/{0}",
            DefaultRateRules = new[]
            {
                    "XMR_X = XMR_BTC * BTC_X",
                    "XMR_BTC = kraken(XMR_BTC)"
                },
            CryptoImagePath = "/imlegacy/monero.svg",
            UriScheme = "monero"
        };
        Add(network);
        services.AddTransactionLinkProvider(new Payments.PaymentMethodId("XMR", PaymentTypes.BTCLike), new SimpleTransactionLinkProvider(network));
    }
}

