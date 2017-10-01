using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ScrapySharp.Extensions;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ScrapyScraper.Console
{

    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            try
            {
                log.Info("Scraping started");
                var pathBin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var pathFileScrape = Path.Combine(pathBin, ConfigurationManager.AppSettings["SETTING_URLS"]);
                if (string.IsNullOrEmpty(pathFileScrape) || !File.Exists(pathFileScrape))
                {
                    log.Error("File not found");
                    return;
                }

                // Read file
                using (var streamReader = File.OpenText(pathFileScrape))
                {
                    var urls = streamReader.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    var itemCollection = new List<Items.HonestbeePh>();

                    // Loop through each URL on file
                    foreach (var url in urls)
                    {
                        if (string.IsNullOrEmpty(url)) continue;

                        var webGetCategory = new HtmlWeb();
                        if (webGetCategory.Load(url) is HtmlDocument category)
                        {
                            log.InfoFormat(" Scraping {0}", url);
                            var arrUrl = url.Split('/');
                            var currCategory = arrUrl[arrUrl.Length - 3];
                            var currSubCategory = arrUrl[arrUrl.Length - 1];

                            var pageItems = category.DocumentNode.CssSelect("._21fv8iCnSiWMpLxNvsklkl").ToList();

                            // Loop through each items on page
                            var pageItemCount = 0;
                            foreach (var pageItem in pageItems)
                            {
                                var item = new Items.HonestbeePh
                                {
                                    Category = currCategory,
                                    SubCategory = currSubCategory
                                };

                                var link = pageItem.Attributes["href"].Value;
                                var domain = ConfigurationManager.AppSettings["SETTING_DOMAIN"];

                                // Get items
                                var img = pageItem.CssSelect("._1fOAPaWUraT3tkmwTy8ymX > img");
                                if (img.Any()) item.ImageSrc = img.FirstOrDefault().Attributes["src"].Value;
                                else item.ImageSrc = string.Empty;

                                var title = pageItem.CssSelect("div._2UCShViKs8ydkfj-XuvUhM");
                                if (title.Any()) item.Title = title.FirstOrDefault().InnerText;
                                else item.Title = string.Empty;

                                var description = pageItem.CssSelect("div._3MvGCVMGqgv4KoGQ2wGzfk");
                                if (description.Any()) item.Description = description.FirstOrDefault().InnerText;
                                else item.Description = string.Empty;

                                var price = pageItem.CssSelect("div._23g1UkP8VGFqvGuLjUsc-H");
                                if (price.Any()) item.Price = price.FirstOrDefault().InnerText;
                                else item.Price = string.Empty;

                                var priceOrig = pageItem.CssSelect("del._1cBPpMK9Rz7AJ9O6CEdCsE");
                                if (priceOrig.Any()) item.PriceSaved = priceOrig.FirstOrDefault().InnerText;
                                else item.PriceSaved = string.Empty;

                                item.Url = domain + link;

                                pageItemCount++;
                                itemCollection.Add(item);
                            }
                            log.InfoFormat(" Scraped {0} of {1}", pageItemCount, pageItems.Count);
                        }
                    }

                    // Output to file
                    var serializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),

                    };
                    var json = JsonConvert.SerializeObject(itemCollection, Formatting.Indented, serializerSettings);
                    var fileOut = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), string.Format(ConfigurationManager.AppSettings["SETTING_OUTPUT_DIR"], DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss")));

                    if (!string.IsNullOrEmpty(json) && !File.Exists(fileOut))
                    {
                        using (var writer = File.CreateText(fileOut))
                        {
                            writer.WriteLine(json);
                            log.InfoFormat("Data file created: {0}", fileOut);
                        }
                    }
                }
                log.Info("Scraping completed");
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error: {0}; StackTrace: {1}", ex.InnerException, ex.StackTrace);
            }
        }
    }
}
