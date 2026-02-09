using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

// Manually register provider
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
// Try to force the console to handle Unicode if possible, or stick to default
Console.OutputEncoding = Encoding.UTF8;

string dataPath = @"c:\Users\Dell\Desktop\Senior Project_EkaterinaValeva\SeniorProject\SeniorProject\uploads\extracted";
if (!Directory.Exists(dataPath)) 
{
    Console.WriteLine($"Directory not found: {dataPath}");
    return;
}

var files = Directory.GetFiles(dataPath, "*.csv", SearchOption.AllDirectories).Take(20);
var encoding = Encoding.GetEncoding(1251);

var catMap = new Dictionary<string, List<string>>();
var catCount = new Dictionary<string, int>();

foreach (var file in files)
{
    try 
    {
        var lines = File.ReadAllLines(file, encoding);
        foreach (var line in lines.Skip(1)) // Header
        {
            var cols = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
            if (cols.Length < 7) continue;
            
            var town = cols[0].Replace("\"", "").Trim();

            if (string.IsNullOrWhiteSpace(town)) continue;

            if (!catMap.ContainsKey(town)) 
            {
                catMap[town] = new List<string>();
                catCount[town] = 0;
            }
            
            catCount[town]++;
            // Collecting store names as examples for the town
            var store = cols[1].Replace("\"", "").Trim();
            if (catMap[town].Count < 5 && !catMap[town].Contains(store)) 
                catMap[town].Add(store);
        }
    }
    catch {}
}

Console.WriteLine("--- TOWN ANALYSIS ---");
foreach (var kvp in catMap.OrderByDescending(k => catCount[k.Key]))
{
    Console.WriteLine($"Town [{kvp.Key}] (Count: {catCount[kvp.Key]}): " + string.Join(", ", kvp.Value));
}
