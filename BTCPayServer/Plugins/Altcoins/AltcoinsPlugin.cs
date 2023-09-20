using System;
using System.Collections.Generic;
using System.Linq;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Hosting;
using BTCPayServer.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using NBXplorer;

namespace BTCPayServer.Plugins.Altcoins
{
    public partial class AltcoinsPlugin : BaseBTCPayServerPlugin
    {
        public override string Identifier => "BTCPayServer.Plugins.Altcoins";
        public override string Name => "Altcoins";
        public override string Description => "Add altcoins support";

        public ChainName NetworkType { get; private set; }
        public NBXplorerNetworkProvider NBXplorerNetworkProvider { get; private set; }

        List<BTCPayNetworkBase> _Networks = new List<BTCPayNetworkBase>();
        T Add<T>(T network) where T : BTCPayNetworkBase
        {
            _Networks.Add(network);
            return network;
        }
        public override void Execute(IServiceCollection applicationBuilder)
        {
            var services = (PluginServiceCollection)applicationBuilder;
            var onChain = new Payments.PaymentMethodId("BTC", Payments.PaymentTypes.BTCLike);

            NBXplorerNetworkProvider = services.BootstrapServices.GetRequiredService<NBXplorerNetworkProvider>();
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("BTC");
            NetworkType = NBXplorerNetworkProvider.NetworkType;


            InitLiquid(services);
            InitLiquidAssets(services);
            InitLitecoin(services);
            InitDogecoin(services);
            InitBGold(services);
            InitMonacoin(services);
            InitDash(services);
            InitGroestlcoin(services);
            InitMonero(services);
            InitZcash(services);

            var selectedChains = services.BootstrapServices.GetRequiredService<SelectedChains>();
            foreach (var elNetwork in _Networks.OfType<ElementsBTCPayNetwork>().Where(n => selectedChains.Contains(n.CryptoCode)))
            {
                // Always include the native asset
                if (!elNetwork.IsNativeAsset)
                {
                    selectedChains.Add(elNetwork.NetworkCryptoCode);
                }
                // Always include the child assets
                else
                {
                    foreach (var assetNetwork in _Networks.OfType<ElementsBTCPayNetwork>()
                                                       .Where(n => !n.IsNativeAsset && n.NetworkCryptoCode == elNetwork.CryptoCode))
                    {
                        selectedChains.Add(assetNetwork.CryptoCode);
                    }
                }
            }

            foreach (var network in _Networks)
            {
                if (selectedChains.Contains(network.CryptoCode))
                    services.AddBTCPayNetwork(network);
            }

            // Assume that electrum mappings are same as BTC if not specified
            foreach (var network in _Networks.OfType<BTCPayNetwork>())
            {
                if (network.ElectrumMapping.Count == 0)
                {
                    network.ElectrumMapping = NetworkType == ChainName.Mainnet
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
                    };
                    if (!network.NBitcoinNetwork.Consensus.SupportSegwit)
                    {
                        network.ElectrumMapping =
                            network.ElectrumMapping
                            .Where(kv => kv.Value == DerivationType.Legacy)
                            .ToDictionary(k => k.Key, k => k.Value);
                    }
                }
            }


        }
    }
}
