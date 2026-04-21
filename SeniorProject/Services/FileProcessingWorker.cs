using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;
using SeniorProject.Extensions;

namespace SeniorProject.Services
{
    public class FileProcessingWorker : BackgroundService
    {
        private readonly ILogger<FileProcessingWorker> _logger;
        private readonly IBackgroundTaskQueue _taskQueue; 
        private readonly IServiceScopeFactory _serviceScopeFactory; 

        public FileProcessingWorker(
            IBackgroundTaskQueue taskQueue,
            ILogger<FileProcessingWorker> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Processing Worker is starting."); 

            while (!stoppingToken.IsCancellationRequested)
            {
                var filePath = await _taskQueue.DequeueAsync(stoppingToken); 

                try
                {
                    _logger.LogInformation($"Starting to process queued file: {filePath}");
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        await ProcessFileAsync(filePath, db);
                    } 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing file processing.");
                }
            }

            _logger.LogInformation("File Processing Worker is stopping.");
        }

        private async Task ProcessFileAsync(string zipPath, ApplicationDbContext _db)
        {
             var sw = System.Diagnostics.Stopwatch.StartNew();
             string uploadFolder = Path.GetDirectoryName(zipPath);
             string extractPath = Path.Combine(uploadFolder, "extracted");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, true);

            var encoding = System.Text.Encoding.UTF8;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;
            _db.Database.SetCommandTimeout(300);

            try
            {
                // clear all old products to ensure fresh data for the day
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [ImportedProducts]");
               

                var categoryMap = new Dictionary<string, string>
                {
                    { "9", "Milk" },
                    { "24", "Beef" },
                    { "20", "Pork" }
                };

                var townMap = new Dictionary<string, string>
                {
                    { "68134", "Sofia" },
                    { "56784", "Plovdiv" },
                    { "10135", "Varna" },
                    { "07079", "Burgas" }
                };

                var existingTowns = await _db.Towns.ToDictionaryAsync(t => t.Name, t => t.Id); 
                var existingChains = await _db.RetailChains.ToDictionaryAsync(c => c.Name, c => c.Id);
                var newProducts = new List<ImportedProduct>();

                var existingWordsCache = await _db.ImportedProducts
                    .Select(p => new { p.NameHash, p.CleanName })
                    .Distinct()
                    .GroupBy(p => p.NameHash)
                    .Select(g => g.First())
                    .ToDictionaryAsync(p => p.NameHash, p => p.CleanName);
                
                var existingHashes = new HashSet<int>(existingWordsCache.Keys);

                var importTimestamp = DateTime.UtcNow; 
                foreach (var file in Directory.GetFiles(extractPath, "*.csv", SearchOption.AllDirectories))
                {
                    string rawFileName = Path.GetFileNameWithoutExtension(file);
                    string actualChainName = rawFileName;
                    int parenIndex = rawFileName.IndexOf('(');
                    
                    if (parenIndex > 0)
                    {
                        actualChainName = rawFileName.Substring(0, parenIndex).Trim();
                    }
                    else if (rawFileName.Contains("_"))
                    {
                        actualChainName = rawFileName.Substring(0, rawFileName.IndexOf('_')).Trim();
                    }

                    using (var reader = new StreamReader(file, encoding))
                    {
                        reader.ReadLine();

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();

                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var cols = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                            for (int i = 0; i < cols.Length; i++)
                            {
                                cols[i] = cols[i].Trim('"');
                            }

                            if (cols.Length < 7) continue;

                            string townCode = cols[0];
                            string storeAddress = cols[1].Trim(); // individual store location (e.g. "МАГАЗИН 101 МЛАДОСТ 4")
                            string productName = cols[2].TrimStart('=', ',', '+', '.', '*', '-', ' ').Trim();
                            
                            string cleanName = productName.ToCleanSortedString();

                            string categoryCode = cols[4];
                            string priceText = cols[5];
                            string promoText = cols[6];

                            string townName = townCode;
                            if (townMap.ContainsKey(townCode))
                            {
                                townName = townMap[townCode]; 
                            }

                            string category = categoryCode;
                            if (categoryMap.ContainsKey(categoryCode))
                            {
                                category = categoryMap[categoryCode];
                            }

                            if (!decimal.TryParse(priceText, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                            {
                                continue;
                            }

                            decimal? parsedPromoPrice = null;
                            if (!string.IsNullOrEmpty(promoText))
                            {
                                if (decimal.TryParse(promoText, System.Globalization.CultureInfo.InvariantCulture, out decimal promo) && promo > 0)
                                {
                                    parsedPromoPrice = promo;
                                }
                            }

                            if (!existingTowns.ContainsKey(townName))
                            {
                                var newTown = new Town { Name = townName };
                                _db.Towns.Add(newTown);
                                await _db.SaveChangesAsync();
                                existingTowns[townName] = newTown.Id;
                            }
                            int townId = existingTowns[townName];

                            string chainName = actualChainName;
                            if (!existingChains.ContainsKey(chainName))
                            {
                                var newChain = new RetailChain { Name = chainName };
                                _db.RetailChains.Add(newChain);
                                await _db.SaveChangesAsync();
                                existingChains[chainName] = newChain.Id;
                            }
                            int chainId = existingChains[chainName];

                            int generatedHash = cleanName.GetStableHashCode();
                            int nameHash = generatedHash;

                            if (!existingHashes.Contains(generatedHash))
                            {
                                existingHashes.Add(generatedHash);
                                existingWordsCache[generatedHash] = cleanName;
                            }

                            var product = new ImportedProduct
                            {
                                Name = productName,
                                ProductCode = cols[3],
                                StoreAddress = storeAddress,
                                CleanName = cleanName,
                                NameHash = nameHash,
                                Category = category,
                                Price = price,
                                PromoPrice = parsedPromoPrice,
                                TownId = townId,
                                RetailChainId = chainId,
                                ImportDate = importTimestamp
                            };

                            newProducts.Add(product);

                            if (newProducts.Count >= 1000)
                            {
                                await _db.ImportedProducts.AddRangeAsync(newProducts);
                                await _db.SaveChangesAsync();
                                newProducts.Clear();
                            }
                        }
                    }
                }
                
                if (newProducts.Any())
                {
                    await _db.ImportedProducts.AddRangeAsync(newProducts);
                    await _db.SaveChangesAsync();
                }
            }
            finally
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = true;
                sw.Stop();
                _logger.LogInformation($"File upload and background processing completed in {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} seconds).");
            }
        }
    }
}
