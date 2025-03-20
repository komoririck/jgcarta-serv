namespace hololive_oficial_cardgame_server;

using ExcelDataReader;
using hololive_oficial_cardgame_server.SerializableObjects;
using Org.BouncyCastle.Asn1.X509;
using System.Data;
using System.Text;

public class Record
{
    public string Name { get; set; }
    public string OtherNameOne { get; set; }
    public string OtherNameTwo { get; set; }
    public string CardType { get; set; }
    public string Rarity { get; set; }
    public string Product { get; set; }
    public string Color { get; set; }
    public string HP { get; set; }
    public string BloomLevel { get; set; }
    public string Arts { get; set; }
    public string OshiSkill { get; set; }
    public string SPOshiSkill { get; set; }
    public string AbilityText { get; set; }
    public string Illustrator { get; set; }
    public string CardNumber { get; set; }
    public string Life { get; set; }
    public string Tag { get; set; }
}

    public static class FileReader
{
    public static Dictionary<string, Record> result = new ();

    public static void ReadFile(string fileName)
    {
        string rootPath = Directory.GetCurrentDirectory();
        string path = Path.Combine(rootPath, fileName);

        // Check if the file exists
        if (!File.Exists(path))
        {
            Lib.WriteConsoleMessage($"File does not exist at path: {path}");
            return;
        }
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // Open the Excel file and read it using ExcelDataReader
        using (
            var stream = File.Open(path, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                // Convert the reader data into a DataSet
                var resultDataSet = reader.AsDataSet();

                // Access the first sheet in the workbook
                DataTable table = resultDataSet.Tables[0];

                // Skip the first row (header) and process the rest
                for (int i = 1; i < table.Rows.Count; i++)
                {
                    DataRow row = table.Rows[i];

                    // Ensure there are at least 13 columns
                    if (row.ItemArray.Length < 14)
                    {
                        Lib.WriteConsoleMessage($"Invalid data format in line {i + 1}");
                        continue;
                    }

                    // Create a new Record and populate it
                    Record record = new Record
                    {
                        Name = row[0]?.ToString() ?? "",
                        CardType = row[1]?.ToString() ?? "",
                        Rarity = row[2]?.ToString() ?? "",
                        Product = row[3]?.ToString() ?? "",
                        Color = row[4]?.ToString() ?? "",
                        HP = row[5]?.ToString() ?? "",
                        BloomLevel = row[6]?.ToString() ?? "",
                        Arts = row[7]?.ToString() ?? "",
                        OshiSkill = row[8]?.ToString() ?? "",
                        SPOshiSkill = row[9]?.ToString() ?? "",
                        AbilityText = row[10]?.ToString() ?? "",
                        Illustrator = row[11]?.ToString() ?? "",
                        CardNumber = row[12]?.ToString() ?? "",
                        Life = row[13]?.ToString() ?? "",
                        Tag = row[14]?.ToString() ?? "",
                        OtherNameOne = row[15]?.ToString() ?? "",
                        OtherNameTwo = row[15]?.ToString() ?? ""
                    };
                    try
                    {
                        result.Add(row[12]?.ToString(), record);
                    }
                    catch (Exception ex) 
                    { 
                    }
                }
            }
        }
    }

    public static List<Record> QueryRecords(string name = null, string type = null, string bloomLevel = null, string cardNumber = null, string tag = null)
    {
        var query = result.AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(r => r.Value.Name == name);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(r => r.Value.CardType == type);

        if (!string.IsNullOrEmpty(bloomLevel))
            query = query.Where(r => r.Value.BloomLevel == bloomLevel);

        if (!string.IsNullOrEmpty(cardNumber))
            query = query.Where(r => r.Value.CardNumber == cardNumber);

        if (!string.IsNullOrEmpty(tag))
            query = query.Where(r => r.Value.Tag.Contains($"#{tag}"));

        return query.Select(r => r.Value).ToList();
    }
    public static List<Record> QueryRecordsByNameAndBloom(List<Card> cards, string bloomLevel)
    {
        var uniqueCardNumbers = cards.GroupBy(card => card.cardNumber).Select(group => group.Key).ToList();
        return result.Where(r => uniqueCardNumbers.Contains(r.Value.CardNumber) && r.Value.BloomLevel == bloomLevel).Select(r => r.Value).ToList();
    }
    public static List<Record> QueryBloomableCard(string name, string bloomLevel)
    {
        string nextBloomLevel = bloomLevel.Equals("Debut") ? "1st" : "2nd";

        if (string.IsNullOrEmpty(nextBloomLevel))
            return new List<Record>(); 

        var query = new List<Record>();

        if (name.Equals("SorAZ", StringComparison.OrdinalIgnoreCase))
        {
            query.AddRange(result.Where(r =>(r.Value.Name == "AZKi" || r.Value.Name == "ときのそら") && r.Value.BloomLevel == nextBloomLevel).Select(r => r.Value).ToList());
        }
        else
        {
            query.AddRange(result.Where(r => r.Value.Name == name && r.Value.BloomLevel == nextBloomLevel).Select(r => r.Value).ToList());
        }

        return query;
    }
}

