namespace hololive_oficial_cardgame_server;

using ExcelDataReader;
using System.Data;
using System.Text;

public class Record
{
    public string Name { get; set; }
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
}

    public static class FileReader
{
    public static List<Record> result = new List<Record>();

    public static List<Record> ReadFile(string fileName)
    {
        string rootPath = Directory.GetCurrentDirectory();
        string path = Path.Combine(rootPath, fileName);

        // Check if the file exists
        if (!File.Exists(path))
        {
            Console.WriteLine($"File does not exist at path: {path}");
            return null;
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
                        Console.WriteLine($"Invalid data format in line {i + 1}");
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
                        Life = row[13]?.ToString() ?? ""
                    };

                    result.Add(record);
                }
            }
        }

        return result;
    }

    public static List<Record> QueryRecords(string name = null, string type = null)
    {
        var query = result.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(r => r.Name == name);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(r => r.CardType == type);
        }

        return query.ToList();
    }


    public static List<Record> QueryRecordsByNameAndBloom(List<Card> names, string type)
    {
        var query = result.Where(r => names.GroupBy(card => card.cardNumber).Select(group => group.First()).Any(card => card.cardNumber == r.CardNumber) && r.BloomLevel == type);

        return query.ToList();
    }
}
