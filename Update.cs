namespace PKGDownloader;

public class Update(string game, string version, string url)
{
    public string Game = game;
    public string Version = version;
    public string URL = url;
    public string Filename => Path.GetFileName(new Uri(URL).AbsolutePath);
}