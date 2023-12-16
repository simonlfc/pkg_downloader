using System.Xml.Serialization;
using YamlDotNet.Serialization;

var queue = new List<Update>();
using var reader = File.OpenText(@"G:\RPCS3\0.0.29-15468\games.yml");
var deserializer = new Deserializer().Deserialize<Dictionary<string, string>>(reader);

// Sony didn't renew the certificate, so we need to bypass it
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true
};
var client = new HttpClient(handler);

Console.WriteLine("Scanning RPCS3 games...");

foreach (var entry in deserializer)
{
    var url = BuildPlaystationURL(entry.Key);
    try
    {
        using HttpResponseMessage response = await client.GetAsync(url);
        var result = await response.Content.ReadAsStringAsync();
        if (result.Length == 0)
        {
            Console.WriteLine($"0 update(s) available for {entry.Key}");
            continue;
        }

        var package = GetTitleInfo(result);
        if (package == null)
            continue;

        Console.WriteLine($"{package.Tag.Packages.Count} update(s) available for {entry.Key}");
        foreach (var update in package.Tag.Packages)
        {
            queue.Add(new Update(update.Params.Title, update.Version, update.URL));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}

foreach (var (update, index) in queue.WithIndex())
{
    Directory.CreateDirectory($"Updates/{update.Game}");
    var name = $"{index + 1} - {update.Filename}";

    using var response = await client.GetAsync(update.URL, HttpCompletionOption.ResponseHeadersRead);
    if (response.IsSuccessStatusCode)
    {
        var size = response.Content.Headers.ContentLength ?? -1;
        var bytesRead = 0L;

        using var output = new FileStream($"Updates/{update.Game}/{name}", FileMode.Create, FileAccess.Write);
        using var input = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        int bytes;
        var finished = false;

        do
        {
            bytes = await input.ReadAsync(buffer);
            finished = !(bytes > 0);
            if (bytes > 0)
            {
                bytesRead += bytes;
                var progress = (int)((double)bytesRead / size * 100);

                Console.Write($"\rDownloading update {index + 1} for {update.Game}... {progress}%");
                await output.WriteAsync(buffer.AsMemory(0, bytes));
            }
        } while (!finished);

        await output.FlushAsync();
        output.Close();
    }
    else
    {
        Console.WriteLine($"Failed to download package [{response.StatusCode}]");
    }
}

static TitlePatch? GetTitleInfo(string stream)
{
    var serializer = new XmlSerializer(typeof(TitlePatch));
    using var reader = new StringReader(stream);
    return serializer.Deserialize(reader) as TitlePatch;
}

static string BuildPlaystationURL(string name) => $"https://a0.ww.np.dl.playstation.net/tpl/np/{name}/{name}-ver.xml";