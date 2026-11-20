namespace CareFlow.Data;

public static class DatabasePaths
{
    public static string ConnectionString
    {
        get
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CareFlow");
            Directory.CreateDirectory(folder);
            return $"Data Source={Path.Combine(folder, "careflow.db")}";
        }
    }
}
