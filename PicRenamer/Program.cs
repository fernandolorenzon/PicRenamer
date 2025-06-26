using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Main program class for renaming EasyDiffusion JSON and JPEG files to include the model name,
/// or deleting orphaned JSON files if the corresponding JPEG is missing.
/// </summary>
class Program
{
    /// <summary>
    /// Entry point of the application. Prompts the user for a folder path, processes each JSON file
    /// to extract the model name, renames files accordingly, and deletes orphaned JSON files.
    /// </summary>
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

                        string jpegSource = Path.Combine(folderPath, baseFileName + ".jpeg");
                        if (File.Exists(jpegSource) && Path.GetFileNameWithoutExtension(jpegSource).StartsWith(modelName + "_"))
                        {
                            Console.WriteLine($"✔ JPEG already has model name: {Path.GetFileName(jpegSource)}. Skipping...");
                            continue;
                        }

                        string jsonDest = Path.Combine(folderPath, newFileName + ".json");
                        string jpegDest = Path.Combine(folderPath, newFileName + ".jpeg");

                        if (File.Exists(jpegSource))
                        {
                            File.Move(jpegSource, jpegDest, overwrite: true);
                            File.Move(jsonFile, jsonDest, overwrite: true);
                            Console.WriteLine($"✨ Renamed: {jpegSource} -> {jpegDest}");
                            Console.WriteLine($"✨ Renamed: {jsonFile} -> {jsonDest}");
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

    /// <summary>
    /// Extracts the model name from a given model path using a regular expression.
    /// The model name is expected to appear after the last '/' and before the first '_' character.
    /// Additionally, removes trailing "By" if present.
    /// </summary>
    /// <param name="path">The model path string from which to extract the model name.</param>
    /// <returns>The extracted model name, or null if not found.</returns>
    static string ExtractModelName(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // Match after the last '/' and before the first '_'
        var match = Regex.Match(path, @"\/([^\/_]+)_");
        if (!match.Success) return null;

        string modelName = match.Groups[1].Value;

        // Remove trailing "By" if present
        if (modelName.EndsWith("By"))
            modelName = modelName.Substring(0, modelName.Length - 2);

        return modelName;
    }
}
