#nullable enable
using System.Collections.Generic;
using System;
using BTCPayServer.Payments;

namespace BTCPayServer.Services;

public class TransactionLinkProviders : Dictionary<PaymentMethodId, TransactionLinkProvider>
{
    public record Entry(PaymentMethodId PaymentMethodId, TransactionLinkProvider Provider);
    public TransactionLinkProviders(IEnumerable<Entry> entries)
    {
        foreach (var e in entries)
        {
            Add(e.PaymentMethodId, e.Provider);
        }
    }

    public string? GetTransactionLink(PaymentMethodId paymentMethodId, string txId)
    {
        ArgumentNullException.ThrowIfNull(paymentMethodId);
        ArgumentNullException.ThrowIfNull(txId);
        TryGetValue(paymentMethodId, out var p);
        return p?.GetTransactionLink(txId);
    }
}
