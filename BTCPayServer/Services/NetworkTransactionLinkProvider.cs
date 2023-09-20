#nullable enable
using NBitcoin;
using System.Globalization;
using System.Linq;

namespace BTCPayServer.Services;

public class NetworkTransactionLinkProvider : TransactionLinkProvider
{
    public NetworkTransactionLinkProvider(BTCPayNetwork network)
    {
        Network = network;
    }

    public BTCPayNetwork Network { get; }

    public string? GetTransactionLink(string txId)
    {
        if (Network.BlockExplorerLink == null)
            return null;
        txId = txId.Split('-').First();
        return string.Format(CultureInfo.InvariantCulture, Network.BlockExplorerLink, txId);
    }
}
