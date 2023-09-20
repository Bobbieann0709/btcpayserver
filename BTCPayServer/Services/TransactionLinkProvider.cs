#nullable enable
using BTCPayServer;

namespace BTCPayServer.Services;
public interface TransactionLinkProvider
{
    string? GetTransactionLink(string txId);
}
