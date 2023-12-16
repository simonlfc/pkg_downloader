namespace PKGDownloader;

public static class Formatters
{
    public static string Size(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        var formattedSize = (double)bytes;
        int order = 0;

        while (formattedSize >= 1024 && order + 1 < sizes.Length)
        {
            order++;
            formattedSize /= 1024;
        }

        return $"{formattedSize:0.##} {sizes[order]}";
    }

    public static string Time(TimeSpan timeSpan) => timeSpan.ToString(@"hh\:mm\:ss");
    public static string URL(string name) => $"https://a0.ww.np.dl.playstation.net/tpl/np/{name}/{name}-ver.xml";
}