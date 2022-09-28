using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.Logging;
using BTCPayServer.Services;
using BTCPayServer.Services.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.HostedServices
{
    /// <summary>
    /// In charge of all long running db migrations that we can't execute on startup in MigrationStartupTask
    /// </summary>
    public class DbMigrationsHostedService : BaseAsyncService
    {
        private readonly InvoiceRepository _invoiceRepository;
        private readonly SettingsRepository _settingsRepository;
        private readonly ApplicationDbContextFactory _dbContextFactory;
        private readonly IOptions<DataDirectories> _datadirs;

        public DbMigrationsHostedService(InvoiceRepository invoiceRepository, SettingsRepository settingsRepository, ApplicationDbContextFactory dbContextFactory, IOptions<DataDirectories> datadirs, Logs logs) : base(logs)
        {
            _invoiceRepository = invoiceRepository;
            _settingsRepository = settingsRepository;
            _dbContextFactory = dbContextFactory;
            _datadirs = datadirs;
        }


        internal override Task[] InitializeTasks()
        {
            return new Task[] { ProcessMigration() };
        }

        protected async Task ProcessMigration()
        {

            var settings = await _settingsRepository.GetSettingAsync<MigrationSettings>();
            if (settings.MigratedInvoiceTextSearchPages != int.MaxValue)
            {
                await MigratedInvoiceTextSearchToDb(settings.MigratedInvoiceTextSearchPages ?? 0);
            }
            if (settings.MigratedTransactionLabels != int.MaxValue)
            {
                await MigratedTransactionLabels(settings.MigratedTransactionLabels ?? 0);
            }

            // Refresh settings since these operations may run for very long time
        }

        internal async Task MigratedTransactionLabels(int startFromOffset)
        {
            var serializer = new JsonSerializerSettings() { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
            int batchCount = 2000;
            int total = 0;
            HashSet<(string WalletId, string LabelId)> existingLabels;
            using (var db = _dbContextFactory.CreateContext())
            {
#pragma warning disable CS0612 // Type or member is obsolete
                total = await db.WalletTransactions.CountAsync();
#pragma warning restore CS0612 // Type or member is obsolete
                existingLabels = (await (db.WalletLabels.AsNoTracking()
                    .Select(wl => new { wl.WalletId, wl.LabelId })
                    .ToListAsync()))
                    .Select(o => (o.WalletId, o.LabelId)).ToHashSet();
            }



next:
            using (var db = _dbContextFactory.CreateContext())
            {
                Logs.PayServer.LogInformation($"Wallet transaction label importing transactions {startFromOffset}/{total}");
#pragma warning disable CS0612 // Type or member is obsolete
                var txs = await db.WalletTransactions
                    .OrderByDescending(wt => wt.TransactionId)
                    .Skip(startFromOffset)
                    .Take(batchCount)
                    .ToArrayAsync();
#pragma warning restore CS0612 // Type or member is obsolete

                foreach (var tx in txs)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    var blob = tx.GetBlobInfo();
#pragma warning restore CS0612 // Type or member is obsolete
                    var data = new JObject();
                    data.Add("comment", blob.Comment ?? String.Empty);
                    db.WalletObjects.Add(new Data.WalletObjectData()
                    {
                        WalletId = tx.WalletDataId,
                        ObjectTypeId = Data.WalletObjectData.ObjectTypes.Tx,
                        ObjectId = tx.TransactionId,
                        Data = data.ToString()
                    });
                    foreach (var label in blob.Labels)
                    {
                        if (!existingLabels.Contains((tx.WalletDataId, label.Key)))
                        {
                            JObject labelData = new JObject();
                            labelData.Add("color", "#000");
                            db.WalletLabels.Add(new WalletLabelData()
                            {
                                WalletId = tx.WalletDataId,
                                LabelId = label.Key,
                                Data = labelData.ToString()
                            });
                            existingLabels.Add((tx.WalletDataId, label.Key));
                        }
                        db.WalletTaints.Add(new WalletTaintData()
                        {
                            WalletId = tx.WalletDataId,
                            ObjectTypeId = Data.WalletObjectData.ObjectTypes.Tx,
                            ObjectId = tx.TransactionId,
                            TaintId = label.Value.TaintId,
                            LabelId = label.Key,
                            TaintTypeId = label.Value.Type,
                            Data = JsonConvert.SerializeObject(label.Value, serializer)
                        });
                    }
                }
                await db.SaveChangesAsync();
                if (txs.Length < batchCount)
                {
                    var settings = await _settingsRepository.GetSettingAsync<MigrationSettings>();
                    settings.MigratedTransactionLabels = int.MaxValue;
                    await _settingsRepository.UpdateSetting(settings);
                    Logs.PayServer.LogInformation($"Wallet transaction label successfully migrated");
                    return;
                }
                else
                {
                    startFromOffset += batchCount;
                    var settings = await _settingsRepository.GetSettingAsync<MigrationSettings>();
                    settings.MigratedTransactionLabels = startFromOffset;
                    await _settingsRepository.UpdateSetting(settings);
                    goto next;
                }
            }
        }

        private async Task MigratedInvoiceTextSearchToDb(int startFromPage)
        {
            // deleting legacy DBriize database if present
            var dbpath = Path.Combine(_datadirs.Value.DataDir, "InvoiceDB");
            if (Directory.Exists(dbpath))
            {
                Directory.Delete(dbpath, true);
            }

            var invoiceQuery = new InvoiceQuery { IncludeArchived = true };
            var totalCount = await CountInvoices();
            const int PAGE_SIZE = 1000;
            var totalPages = Math.Ceiling(totalCount * 1.0m / PAGE_SIZE);
            Logs.PayServer.LogInformation($"Importing {totalCount} invoices into the search table in {totalPages - startFromPage} pages");
            for (int i = startFromPage; i < totalPages && !CancellationToken.IsCancellationRequested; i++)
            {
                Logs.PayServer.LogInformation($"Import to search table progress: {i + 1}/{totalPages} pages");
                // migrate data to new table using invoices from database
                using var ctx = _dbContextFactory.CreateContext();
                invoiceQuery.Skip = i * PAGE_SIZE;
                invoiceQuery.Take = PAGE_SIZE;
                var invoices = await _invoiceRepository.GetInvoices(invoiceQuery);

                foreach (var invoice in invoices)
                {
                    var textSearch = new List<string>();

                    // recreating different textSearch.Adds that were previously in DBriize
                    foreach (var paymentMethod in invoice.GetPaymentMethods())
                    {
                        if (paymentMethod.Network != null)
                        {
                            var paymentDestination = paymentMethod.GetPaymentMethodDetails().GetPaymentDestination();
                            textSearch.Add(paymentDestination);
                            textSearch.Add(paymentMethod.Calculate().TotalDue.ToString());
                        }
                    }
                    // 
                    textSearch.Add(invoice.Id);
                    textSearch.Add(invoice.InvoiceTime.ToString(CultureInfo.InvariantCulture));
                    textSearch.Add(invoice.Price.ToString(CultureInfo.InvariantCulture));
                    textSearch.Add(invoice.Metadata.OrderId);
                    textSearch.Add(invoice.StoreId);
                    textSearch.Add(invoice.Metadata.BuyerEmail);
                    //
                    textSearch.Add(invoice.RefundMail);
                    // TODO: Are there more things to cache? PaymentData?
                    InvoiceRepository.AddToTextSearch(ctx,
                        new InvoiceData { Id = invoice.Id, InvoiceSearchData = new List<InvoiceSearchData>() },
                        textSearch.ToArray());
                }

                var settings = await _settingsRepository.GetSettingAsync<MigrationSettings>();
                if (i + 1 < totalPages)
                {
                    settings.MigratedInvoiceTextSearchPages = i;
                }
                else
                {
                    // during final pass we set int.MaxValue so migration doesn't run again
                    settings.MigratedInvoiceTextSearchPages = int.MaxValue;
                }
                // this call triggers update; we're sure that MigrationSettings is already initialized in db 
                // because of logic executed in MigrationStartupTask.cs
                _settingsRepository.UpdateSettingInContext(ctx, settings);
                await ctx.SaveChangesAsync();
                CancellationToken.ThrowIfCancellationRequested();
            }
            Logs.PayServer.LogInformation($"Full invoice search import successful");
        }

        private async Task<int> CountInvoices()
        {
            using var ctx = _dbContextFactory.CreateContext();
            return await ctx.Invoices.CountAsync();
        }
    }
}
