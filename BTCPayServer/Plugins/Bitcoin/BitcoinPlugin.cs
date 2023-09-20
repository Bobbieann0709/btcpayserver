#nullable enable
using System.Collections.Generic;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Hosting;
using BTCPayServer.Services;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer.Plugins.Bitcoin
{
    public class BitcoinPlugin : BaseBTCPayServerPlugin
    {
        public override string Identifier => "BTCPayServer.Plugins.Bitcoin";
        public override string Name => "Bitcoin";
        public override string Description => "Add Bitcoin support";

        public override void Execute(IServiceCollection applicationBuilder)
        {
            var services = (PluginServiceCollection)applicationBuilder;
            var onChain = new Payments.PaymentMethodId("BTC", Payments.PaymentTypes.BTCLike);
            var nbxplorerNetworkProvider = services.BootstrapServices.GetRequiredService<NBXplorerNetworkProvider>();
            var nbxplorerNetwork = nbxplorerNetworkProvider.GetFromCryptoCode("BTC");
            var networkType = nbxplorerNetwork.NBitcoinNetwork.ChainName;
            var network = new BTCPayNetwork()
            {
                CryptoCode = nbxplorerNetwork.CryptoCode,
                DisplayName = "Bitcoin",
                BlockExplorerLink = networkType == ChainName.Mainnet ? "https://mempool.space/tx/{0}" :
                                    networkType == NBitcoin.Bitcoin.Instance.Signet.ChainName ? "https://mempool.space/signet/tx/{0}"
                                    : "https://mempool.space/testnet/tx/{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                CryptoImagePath = "imlegacy/bitcoin.svg",
                LightningImagePath = "imlegacy/bitcoin-lightning.svg",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(networkType),
                CoinType = networkType == ChainName.Mainnet ? new KeyPath("0'") : new KeyPath("1'"),
                SupportRBF = true,
                SupportPayJoin = true,
                VaultSupported = true,
                //https://github.com/spesmilo/electrum/blob/11733d6bc271646a00b69ff07657119598874da4/electrum/constants.py
                ElectrumMapping = networkType == ChainName.Mainnet
                    ? new Dictionary<uint, DerivationType>()
                    {
                        {0x0488b21eU, DerivationType.Legacy }, // xpub
                        {0x049d7cb2U, DerivationType.SegwitP2SH }, // ypub
                        {0x04b24746U, DerivationType.Segwit }, //zpub
                    }
                    : new Dictionary<uint, DerivationType>()
                    {
                        {0x043587cfU, DerivationType.Legacy}, // tpub
                        {0x044a5262U, DerivationType.SegwitP2SH}, // upub
                        {0x045f1cf6U, DerivationType.Segwit} // vpub
                    }
            };
            if (services.BootstrapServices.GetRequiredService<SelectedChains>().Contains(network.CryptoCode))
                applicationBuilder.AddBTCPayNetwork(network);
            applicationBuilder.AddTransactionLinkProvider(onChain, new NetworkTransactionLinkProvider(network));
        }
    }
}
