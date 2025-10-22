// See https://aka.ms/new-console-template for more information



using System.Text.Json;
using System.Xml.Linq;
using Org.BouncyCastle.Asn1;
using Service.Library;
using TestConsole;

/*
 * This will act as a scratch pad for testing out this code...
 */

List<ComparisonTest> dest = new List<ComparisonTest>()
{
    new ComparisonTest() { Name = "Alice", Age = 30 },
    new ComparisonTest() { Name = "Bob", Age = 25 },
    new ComparisonTest() { Name = "Charlie", Age = 35 }
};

List<ComparisonTest> source = new List<ComparisonTest>()
{
    new ComparisonTest() { Name = "Alice", Age = 31 }, // Age updated
    new ComparisonTest() { Name = "Bob", Age = 25 },   // No change
    new ComparisonTest() { Name = "Diana", Age = 28 },  // New entry
    new ComparisonTest() { Name = "Eve", Age = 22 }  // New entry
};

dest.SyncronizeCollections<ComparisonTest>(source,
    (a, b) => a.Name == b.Name,
    (destItem, sourceItem) =>
    {
        destItem.Age = sourceItem.Age;
       // destItem.SetPropertyValue("Age", sourceItem);
    });

// no charlie, add diana and eve, update alice's age
foreach (var item in dest)
{
    Console.WriteLine($"Name: {item.Name}, Age: {item.Age}");
}



