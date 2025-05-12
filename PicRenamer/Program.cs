using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter the full path to the folder with the EasyDiffusion files:");
        string folderPath = Console.ReadLine();

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Folder does not exist.");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonFile);
                using JsonDocument doc = JsonDocument.Parse(jsonContent);

                if (doc.RootElement.TryGetProperty("use_stable_diffusion_model", out JsonElement modelElement))
                {
                    string modelPath = modelElement.GetString();
                    string modelName = ExtractModelName(modelPath);

                    if (!string.IsNullOrEmpty(modelName))
                    {
                        string baseFileName = Path.GetFileNameWithoutExtension(jsonFile);
                        string newFileName = $"{modelName}_{baseFileName}";

                        if (baseFileName.StartsWith(modelName + "_"))
                        {
                            Console.WriteLine($"✔ File already has model name: {baseFileName}. Skipping...");
                            continue;
                        }

                        string jsonDest = Path.Combine(folderPath, newFileName + ".json");
                        string jpegSource = Path.Combine(folderPath, baseFileName + ".jpeg");
                        string jpegDest = Path.Combine(folderPath, newFileName + ".jpeg");

                        if (File.Exists(jpegSource))
                        {
                            File.Move(jpegSource, jpegDest, overwrite: true);
                            Console.WriteLine($"✨ Renamed: {jpegSource} -> {jpegDest}");
                        }
                        else
                        {
                            Console.WriteLine($"🗑 JPEG not found for: {baseFileName}. Sending orphaned JSON to Recycle Bin.");
                            try
                            {
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                                    jsonFile,
                                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                                );
                            }
                            catch (Exception deleteEx)
                            {
                                Console.WriteLine($"Failed to delete {jsonFile}: {deleteEx.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {jsonFile}: {ex.Message}");
            }
        }

        Console.WriteLine("Done!");
    }

    static string ExtractModelName(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var match = Regex.Match(path, @"\/([^\/_]+)_");
        return match.Success ? match.Groups[1].Value : null;
    }
}
