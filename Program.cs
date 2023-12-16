namespace PKGDownloader;

public class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Scanning RPCS3 library...");
        var queue = new List<Update>();
        using var reader = File.OpenText("../games.yml");
        var deserializer = new Deserializer().Deserialize<Dictionary<string, string>>(reader);

        // sony didn't renew the certificate, so we need to bypass it
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true
        };
        var client = new HttpClient(handler);

        foreach (var entry in deserializer)
        {
            Console.WriteLine($"Found {entry.Key}");

            var url = Formatters.URL(entry.Key);
            using HttpResponseMessage response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            if (result.Length == 0)
                continue;

            var package = GetTitleInfo(result);
            if (package == null)
                continue;

            var gameName = "";

            // not every package has a paramsfo, so we need to find one that has the title name
            foreach (var pkg in package.Tag.Packages)
            {
                if (pkg.Params != null)
                {
                    gameName = pkg.Params.Title;
                    break;
                }
            }

            // we don't want to re-download an existing package, check if it exists and that it was completed
            foreach (var update in package.Tag.Packages)
            {
                var enqueue = new Update(gameName, update.Version, update.URL);
                var filename = $"Updates/{enqueue.Game}/{enqueue.Filename}";
                if (File.Exists(filename))
                {
                    var info = new FileInfo(filename);
                    if (info.Length == update.Size)
                        continue;
                }

                queue.Add(enqueue);
            }
        }

        // begin download manager UI
        Application.Init();
        Application.IsMouseDisabled = true;

        var win = new Window("Download Manager")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = Colors.TopLevel
        };
        Application.Top.Add(win);

        var packageNameLabel = new Label("")
        {
            X = 5,
            Y = 2,
            AutoSize = true,
            Height = 1,
        };
        Application.Top.Add(packageNameLabel);

        var percentLabel = new Label("")
        {
            X = 5,
            Y = Pos.Bottom(packageNameLabel) + 2,
            AutoSize = true,
            Height = 1,
            Border = new Border()
            {
                BorderStyle = BorderStyle.Rounded,
                Title = "Progress"
            }
        };
        Application.Top.Add(percentLabel);

        var speedLabel = new Label("")
        {
            X = 5,
            Y = Pos.Bottom(percentLabel) + 2,
            AutoSize = true,
            Height = 1,
            Border = new Border()
            {
                BorderStyle = BorderStyle.Rounded,
                Title = "Speed"
            }
        };
        Application.Top.Add(speedLabel);

        var sizeLabel = new Label("")
        {
            X = 5,
            Y = Pos.Bottom(speedLabel) + 2,
            AutoSize = true,
            Height = 1,
            Border = new Border()
            {
                BorderStyle = BorderStyle.Rounded,
                Title = "Size"
            }
        };
        Application.Top.Add(sizeLabel);

        var timeLabel = new Label("")
        {
            X = 5,
            Y = Pos.Bottom(sizeLabel) + 2,
            AutoSize = true,
            Height = 1,
            Border = new Border()
            {
                BorderStyle = BorderStyle.Rounded,
                Title = "ETA"
            }
        };
        Application.Top.Add(timeLabel);

        _ = Task.Run(async () =>
        {
            foreach (var (update, index) in queue.WithIndex())
            {
                await Task.Run(async () =>
                {
                    win.Title = $"Update {index + 1} of {queue.Count}";
                    packageNameLabel.Text = $"Downloading update {update.Version} for {update.Game}...";

                    Directory.CreateDirectory($"Updates/{update.Game}");
                    using var response = await client.GetAsync(update.URL, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        var size = response.Content.Headers.ContentLength ?? -1;
                        var bytesRead = 0L;

                        using var output = new FileStream($"Updates/{update.Game}/{update.Filename}", FileMode.Create, FileAccess.Write);
                        using var input = await response.Content.ReadAsStreamAsync();
                        var buffer = new byte[8192];
                        int bytes;
                        var finished = false;
                        var stopwatch = Stopwatch.StartNew();
                        var startTime = DateTime.Now;

                        do
                        {
                            bytes = await input.ReadAsync(buffer);
                            finished = !(bytes > 0);
                            if (bytes > 0)
                            {
                                bytesRead += bytes;
                                var progress = (int)((double)bytesRead / size * 100);

                                // download speed
                                var elapsedTime = DateTime.Now - startTime;
                                var speed = bytesRead / elapsedTime.TotalSeconds;

                                // time remaining
                                var remainingBytes = size - bytesRead;
                                var remainingTime = remainingBytes / speed;

                                // update UI labels
                                Application.MainLoop.Invoke(() =>
                                {
                                    percentLabel.Text = $" {progress}% complete ";
                                    percentLabel.SetNeedsDisplay();
                                    speedLabel.Text = $" {Formatters.Size((long)speed)}/s ";
                                    speedLabel.SetNeedsDisplay();
                                    sizeLabel.Text = $" {Formatters.Size(bytesRead)} / {Formatters.Size(size)} ";
                                    sizeLabel.SetNeedsDisplay();
                                    timeLabel.Text = $" {Formatters.Time(TimeSpan.FromSeconds(remainingTime))} ";
                                    timeLabel.SetNeedsDisplay();
                                });

                                await output.WriteAsync(buffer.AsMemory(0, bytes));
                            }
                        } while (!finished);

                        await output.FlushAsync();
                        output.Close();
                    }
                });
            }

            Application.RequestStop();
        });

        Application.Run();
    }
    

    static TitlePatch? GetTitleInfo(string stream)
    {
        var serializer = new XmlSerializer(typeof(TitlePatch));
        using var reader = new StringReader(stream);
        return serializer.Deserialize(reader) as TitlePatch;
    }
}