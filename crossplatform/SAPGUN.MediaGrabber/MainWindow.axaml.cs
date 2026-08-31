using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using SapgunMediaGrabber.Updates;

namespace SapgunMediaGrabber;

public partial class MainWindow : Window
{
    const string XProfileUrl = "https://x.com/caro7370";
    const string KoFiUrl = "https://ko-fi.com/sapgun";

    readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SAPGUN Media Grabber");
    readonly string toolsDir;
    readonly string settingsFile;
    readonly string channelFile;
    readonly string folderFile;
    readonly HttpClient http = CreateHttp();
    readonly AppUpdateService appUpdates;

    UpdateCheckResult? lastAppCheck;
    CancellationTokenSource? appDownloadCts;
    CancellationTokenSource? jobCts;

    TextBox UrlBox => this.FindControl<TextBox>("UrlBox")!;
    TextBox FolderBox => this.FindControl<TextBox>("FolderBox")!;
    TextBox TrimStart => this.FindControl<TextBox>("TrimStart")!;
    TextBox TrimEnd => this.FindControl<TextBox>("TrimEnd")!;
    RadioButton Mp3Profile => this.FindControl<RadioButton>("Mp3Profile")!;
    ToggleSwitch TrimCheck => this.FindControl<ToggleSwitch>("TrimCheck")!;
    RadioButton XProfile => this.FindControl<RadioButton>("XProfile")!;
    RadioButton OriginalProfile => this.FindControl<RadioButton>("OriginalProfile")!;
    RadioButton Mp4Profile => this.FindControl<RadioButton>("Mp4Profile")!;
    ComboBox CookieBox => this.FindControl<ComboBox>("CookieBox")!;
    TextBlock CookieHint => this.FindControl<TextBlock>("CookieHint")!;
    TextBlock PlatformBanner => this.FindControl<TextBlock>("PlatformBanner")!;
    Button DownloadButton => this.FindControl<Button>("DownloadButton")!;
    Button CancelJobButton => this.FindControl<Button>("CancelJobButton")!;
    Button OpenFolderButton => this.FindControl<Button>("OpenFolderButton")!;
    Button ThemeButton => this.FindControl<Button>("ThemeButton")!;
    ProgressBar Progress => this.FindControl<ProgressBar>("Progress")!;
    TextBlock StatusText => this.FindControl<TextBlock>("StatusText")!;
    TextBlock ProgressText => this.FindControl<TextBlock>("ProgressText")!;
    TextBlock InstalledYtDlp => this.FindControl<TextBlock>("InstalledYtDlp")!;
    TextBlock LatestYtDlp => this.FindControl<TextBlock>("LatestYtDlp")!;
    TextBlock AppCurrentVersion => this.FindControl<TextBlock>("AppCurrentVersion")!;
    TextBlock AppLatestVersion => this.FindControl<TextBlock>("AppLatestVersion")!;
    TextBlock AppUpdateStatus => this.FindControl<TextBlock>("AppUpdateStatus")!;
    ComboBox UpdateChannelBox => this.FindControl<ComboBox>("UpdateChannelBox")!;
    Button CheckAppUpdateButton => this.FindControl<Button>("CheckAppUpdateButton")!;
    Button DownloadAppUpdateButton => this.FindControl<Button>("DownloadAppUpdateButton")!;
    Button CancelAppUpdateButton => this.FindControl<Button>("CancelAppUpdateButton")!;
    ProgressBar AppUpdateProgress => this.FindControl<ProgressBar>("AppUpdateProgress")!;
    TextBlock YtDlpBadgeText => this.FindControl<TextBlock>("YtDlpBadgeText")!;
    TextBlock FfmpegBadgeText => this.FindControl<TextBlock>("FfmpegBadgeText")!;
    Border YtDlpBadge => this.FindControl<Border>("YtDlpBadge")!;
    Border FfmpegBadge => this.FindControl<Border>("FfmpegBadge")!;
    Border OriginalCard => this.FindControl<Border>("OriginalCard")!;
    Border XCard => this.FindControl<Border>("XCard")!;
    Border Mp4Card => this.FindControl<Border>("Mp4Card")!;
    Border Mp3Card => this.FindControl<Border>("Mp3Card")!;
    Border AppCard => this.FindControl<Border>("AppCard")!;
    Border EngineCard => this.FindControl<Border>("EngineCard")!;

    string lastOutput = "";
    bool lightMode;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        toolsDir = Path.Combine(dataDir, "tools");
        settingsFile = Path.Combine(dataDir, "theme.txt");
        channelFile = Path.Combine(dataDir, "update-channel.txt");
        appUpdates = new AppUpdateService(http);
        folderFile = Path.Combine(dataDir, "folder.txt");
        Directory.CreateDirectory(toolsDir);
        SeedBundledTools();

        FolderBox.Text = LoadFolder();

        lightMode = LoadTheme();
        ApplyTheme();
        Title = "SAPGUN Media Grabber v" + AppVersionInfo.Current;
        PlatformBanner.Text = "LOCAL  •  yt-dlp + FFmpeg  •  " + PlatformDetector.DisplayName(PlatformDetector.Detect()) + "  •  v" + AppVersionInfo.Current;
        FillCookieBrowsers();
        AppCurrentVersion.Text = "Current: v" + AppVersionInfo.Current;
        UpdateChannelBox.SelectedIndex = LoadChannel() == UpdateChannel.Stable ? 1 : 0;
        UpdateChannelBox.SelectionChanged += (_, _) => SaveChannel();
        DownloadAppUpdateButton.Content = "Download Update";
        TrimCheck.IsCheckedChanged += (_, _) => ApplyTrimEnabled();
        ApplyTrimEnabled();
        RefreshToolBadges();
        RefreshProfileCards();
        Opened += async (_, _) => await RefreshYtDlpVersions(false);
    }

    static HttpClient CreateHttp()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SAPGUN-Media-Grabber", AppVersionInfo.Current));
        return client;
    }

    string ToolName(string baseName) => OperatingSystem.IsWindows() ? baseName + ".exe" : baseName;
    string YtDlp => Path.Combine(toolsDir, ToolName("yt-dlp"));
    string Ffmpeg => Path.Combine(toolsDir, ToolName("ffmpeg"));
    string Ffprobe => Path.Combine(toolsDir, ToolName("ffprobe"));

    void SeedBundledTools()
    {
        var seed = Path.Combine(AppContext.BaseDirectory, "tools");
        if (!Directory.Exists(seed)) return;
        foreach (var name in new[] { ToolName("yt-dlp"), ToolName("ffmpeg"), ToolName("ffprobe") })
        {
            var source = Path.Combine(seed, name);
            var target = Path.Combine(toolsDir, name);
            if (File.Exists(source) && !File.Exists(target)) File.Copy(source, target);
            EnsureExecutable(target);
        }
    }

    static void EnsureExecutable(string path)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(path)) return;
        try
        {
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch { }
    }

    async void Paste_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null) UrlBox.Text = await clipboard.GetTextAsync() ?? "";
    }

    async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose download folder", AllowMultiple = false });
        if (folders.Count > 0)
        {
            FolderBox.Text = folders[0].Path.LocalPath;
            SaveFolder();
        }
    }

    void OriginalCard_Pressed(object? sender, PointerPressedEventArgs e) => OriginalProfile.IsChecked = true;
    void XCard_Pressed(object? sender, PointerPressedEventArgs e) => XProfile.IsChecked = true;
    void Mp4Card_Pressed(object? sender, PointerPressedEventArgs e) => Mp4Profile.IsChecked = true;
    void Mp3Card_Pressed(object? sender, PointerPressedEventArgs e) => Mp3Profile.IsChecked = true;
    void Profile_Changed(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => RefreshProfileCards();

    void ApplyTrimEnabled()
    {
        var on = TrimCheck.IsChecked == true;
        TrimStart.IsEnabled = on;
        TrimEnd.IsEnabled = on;
    }

    void RefreshToolBadges()
    {
        StyleBadge(YtDlpBadge, YtDlpBadgeText, "yt-dlp", File.Exists(YtDlp));
        StyleBadge(FfmpegBadge, FfmpegBadgeText, "FFmpeg", File.Exists(Ffmpeg));
    }

    void StyleBadge(Border badge, TextBlock label, string name, bool ok)
    {
        label.Text = ok ? name + "  ✓" : name + "  missing";
        badge.Background = new SolidColorBrush(ok ? Color.Parse("#163528") : Color.Parse("#3A1C1C"));
        label.Foreground = new SolidColorBrush(ok ? Color.Parse("#8ED7A7") : Color.Parse("#F0A0A0"));
    }

    void RefreshProfileCards()
    {
        StyleProfileCard(OriginalCard, OriginalProfile.IsChecked == true);
        StyleProfileCard(XCard, XProfile.IsChecked == true);
        StyleProfileCard(Mp4Card, Mp4Profile.IsChecked == true);
        StyleProfileCard(Mp3Card, Mp3Profile.IsChecked == true);
    }

    void StyleProfileCard(Border card, bool selected)
    {
        card.BorderThickness = new Thickness(selected ? 2 : 1);
        if (lightMode)
        {
            card.Background = new SolidColorBrush(selected ? Color.Parse("#EAF0FF") : Color.Parse("#FFFFFF"));
            card.BorderBrush = new SolidColorBrush(selected ? Color.Parse("#4C6FFF") : Color.Parse("#D8DEE8"));
        }
        else
        {
            card.Background = new SolidColorBrush(selected ? Color.Parse("#1A2744") : Color.Parse("#121722"));
            card.BorderBrush = new SolidColorBrush(selected ? Color.Parse("#5B7CFF") : Color.Parse("#2A3140"));
        }
    }
    async void Download_Click(object? sender, RoutedEventArgs e) => await StartDownload();
    void CancelJob_Click(object? sender, RoutedEventArgs e) => jobCts?.Cancel();
    void OpenFolder_Click(object? sender, RoutedEventArgs e) => OpenTarget(File.Exists(lastOutput) ? Path.GetDirectoryName(lastOutput)! : FolderBox.Text ?? "");
    async void CheckYtDlp_Click(object? sender, RoutedEventArgs e) => await RefreshYtDlpVersions(true);

    UpdateChannel SelectedChannel() =>
        UpdateChannelBox.SelectedIndex == 1 ? UpdateChannel.Stable : UpdateChannel.Prerelease;

    UpdateChannel LoadChannel()
    {
        try
        {
            if (File.Exists(channelFile) && File.ReadAllText(channelFile).Trim().Equals("stable", StringComparison.OrdinalIgnoreCase))
                return UpdateChannel.Stable;
        }
        catch { }
        return AppVersionInfo.DefaultChannel;
    }

    void SaveChannel()
    {
        try { File.WriteAllText(channelFile, SelectedChannel() == UpdateChannel.Stable ? "stable" : "prerelease"); }
        catch { }
    }

    string LoadFolder()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var fallback = Directory.Exists(downloads) ? downloads : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        try
        {
            if (File.Exists(folderFile))
            {
                var saved = File.ReadAllText(folderFile).Trim();
                if (saved != "" && Directory.Exists(saved)) return saved;
            }
        }
        catch { }
        return fallback;
    }

    void SaveFolder()
    {
        try
        {
            var path = (FolderBox.Text ?? "").Trim();
            if (path != "" && Directory.Exists(path)) File.WriteAllText(folderFile, path);
        }
        catch { }
    }

    async void CheckAppUpdate_Click(object? sender, RoutedEventArgs e)
    {
        SaveChannel();
        CheckAppUpdateButton.IsEnabled = false;
        DownloadAppUpdateButton.IsEnabled = false;
        AppLatestVersion.Text = "Latest: checking GitHub Releases…";
        AppUpdateStatus.Text = "Checking sapgun/SAPGUN-Media-Grabber releases…";
        try
        {
            lastAppCheck = await appUpdates.CheckAsync(AppVersionInfo.Current, SelectedChannel());
            AppLatestVersion.Text = lastAppCheck.LatestVersion is null ? "Latest: unavailable" : "Latest: v" + lastAppCheck.LatestVersion;
            AppUpdateStatus.Text = lastAppCheck.Message;
            DownloadAppUpdateButton.IsEnabled = lastAppCheck.CanDownload;
            DownloadAppUpdateButton.Content = lastAppCheck.Asset?.ApplyAction == UpdateApplyAction.LaunchInstallerAndExit
                ? "Download & Install"
                : "Download Update";
            if (!string.IsNullOrWhiteSpace(lastAppCheck.ReleaseNotes) && lastAppCheck.CanDownload)
                AppUpdateStatus.Text = lastAppCheck.Message + "\n\n" + lastAppCheck.ReleaseNotes;
        }
        catch (Exception ex)
        {
            lastAppCheck = null;
            AppLatestVersion.Text = "Latest: check failed";
            AppUpdateStatus.Text = ex.Message;
        }
        finally { CheckAppUpdateButton.IsEnabled = true; }
    }

    async void DownloadAppUpdate_Click(object? sender, RoutedEventArgs e)
    {
        if (lastAppCheck is not { CanDownload: true })
        {
            await ShowInfo("Check for an app update first.");
            return;
        }

        appDownloadCts?.Cancel();
        appDownloadCts = new CancellationTokenSource();
        var destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(destDir);

        CheckAppUpdateButton.IsEnabled = false;
        DownloadAppUpdateButton.IsEnabled = false;
        CancelAppUpdateButton.IsVisible = true;
        AppUpdateProgress.IsVisible = true;
        AppUpdateProgress.Value = 0;
        AppUpdateStatus.Text = "Downloading update… this is never installed automatically.";

        try
        {
            var progress = new Progress<UpdateDownloadProgress>(p =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AppUpdateProgress.Value = p.Percent;
                    AppUpdateStatus.Text = p.TotalBytes is > 0
                        ? $"Downloading update — {p.Percent}% ({p.BytesReceived / 1048576.0:0.0} / {p.TotalBytes.Value / 1048576.0:0.0} MB)"
                        : $"Downloading update — {p.BytesReceived / 1048576.0:0.0} MB";
                });
            });
            var downloaded = await appUpdates.DownloadAsync(lastAppCheck, destDir, progress, appDownloadCts.Token);
            AppUpdateProgress.Value = 100;

            if (!downloaded.ChecksumVerified)
            {
                await ShowInfo("The update downloaded, but no SHA-256 was published for this asset.\n\nFile:\n" + downloaded.FilePath + "\n\nComputed SHA-256:\n" + downloaded.Sha256 + "\n\nIt will not be launched automatically.");
                UpdateShell.Reveal(downloaded.FilePath);
                AppUpdateStatus.Text = "Downloaded without a published checksum. File was revealed, not installed.";
                return;
            }

            if (downloaded.ApplyAction == UpdateApplyAction.LaunchInstallerAndExit)
            {
                await ShowInfo("SHA-256 verified.\n\nThe installer will open now. This app will quit so the installer can replace it.\n\nNothing is installed until you continue in the installer.");
                UpdateShell.LaunchInstaller(downloaded.FilePath);
                UpdateShell.ShutdownApp();
                return;
            }

            await ShowInfo("SHA-256 verified.\n\nThe update archive was saved to:\n" + downloaded.FilePath + "\n\nExtract and replace this build yourself. The running app will not overwrite itself.");
            UpdateShell.Reveal(downloaded.FilePath);
            AppUpdateStatus.Text = "Verified update saved to Downloads. Extract it to replace this build.";
        }
        catch (OperationCanceledException)
        {
            AppUpdateStatus.Text = "App update download cancelled.";
        }
        catch (Exception ex)
        {
            AppUpdateStatus.Text = "App update download failed.";
            await ShowInfo(ex.Message);
        }
        finally
        {
            CheckAppUpdateButton.IsEnabled = true;
            DownloadAppUpdateButton.IsEnabled = lastAppCheck?.CanDownload == true;
            CancelAppUpdateButton.IsVisible = false;
            AppUpdateProgress.IsVisible = false;
        }
    }

    void CancelAppUpdate_Click(object? sender, RoutedEventArgs e) => appDownloadCts?.Cancel();

    async void UpdateYtDlp_Click(object? sender, RoutedEventArgs e)
    {
        if (!File.Exists(YtDlp)) { await ShowInfo("yt-dlp is missing from the app tools folder."); return; }
        StatusText.Text = "Updating yt-dlp...";
        var result = await ProcessRunner.RunAsync(YtDlp, new[] { "-U" }, (_, _) => { });
        await RefreshYtDlpVersions(false);
        await ShowInfo(result == 0 ? "yt-dlp update complete." : "yt-dlp update failed. Check your connection and try again.");
    }

    async void Help_Click(object? sender, RoutedEventArgs e)
    {
        await ShowInfo("Paste a media URL, choose a profile, then Download.\n\nX / Twitter 1080p converts to H.264 + AAC for reliable uploads.\n\nCancel stops the current yt-dlp download or FFmpeg conversion and removes leftover .part files from this job.\n\nIf a site returns 403 or requires login, choose the browser where you are signed in under Browser Cookies.\n\nAPP Check App Update looks at GitHub Releases for this application. ENGINE Check / Update yt-dlp only updates the bundled downloader. They are separate.\n\nApp updates are never installed silently. Portable zip/tar.gz builds are saved and revealed. The Windows Setup.exe installer is launched only after you confirm.\n\nTrim is optional. Media conversion is processed locally.");
    }

    void Theme_Click(object? sender, RoutedEventArgs e)
    {
        lightMode = !lightMode;
        ApplyTheme();
        try { File.WriteAllText(settingsFile, lightMode ? "light" : "dark"); } catch { }
    }

    void Feedback_Click(object? sender, RoutedEventArgs e) => OpenTarget(XProfileUrl);
    void Support_Click(object? sender, RoutedEventArgs e) => OpenTarget(KoFiUrl);

    bool LoadTheme()
    {
        try { return File.Exists(settingsFile) && File.ReadAllText(settingsFile).Trim().Equals("light", StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    void ApplyTheme()
    {
        if (Application.Current != null) Application.Current.RequestedThemeVariant = lightMode ? ThemeVariant.Light : ThemeVariant.Dark;
        ThemeButton.Content = lightMode ? "Dark mode" : "Light mode";
        Background = new SolidColorBrush(lightMode ? Color.Parse("#F4F6FA") : Color.Parse("#0B0D12"));
        Foreground = new SolidColorBrush(lightMode ? Color.Parse("#12141A") : Color.Parse("#F2F4F8"));
        DownloadButton.Background = new SolidColorBrush(lightMode ? Color.Parse("#4C6FFF") : Colors.White);
        DownloadButton.Foreground = new SolidColorBrush(lightMode ? Colors.White : Color.Parse("#0B0D12"));
        StylePanelCard(AppCard, accent: true);
        StylePanelCard(EngineCard, accent: false);
        RefreshProfileCards();
    }

    void StylePanelCard(Border card, bool accent)
    {
        if (lightMode)
        {
            card.Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
            card.BorderBrush = new SolidColorBrush(accent ? Color.Parse("#4C6FFF") : Color.Parse("#D8DEE8"));
        }
        else
        {
            card.Background = new SolidColorBrush(Color.Parse("#141822"));
            card.BorderBrush = new SolidColorBrush(accent ? Color.Parse("#4C6FFF") : Color.Parse("#2A3140"));
        }
    }

    string Mode() => OriginalProfile.IsChecked == true ? "original" : Mp4Profile.IsChecked == true ? "mp4" : Mp3Profile.IsChecked == true ? "mp3" : "x";

    void FillCookieBrowsers()
    {
        var platform = PlatformDetector.Detect();
        CookieBox.Items.Clear();
        foreach (var browser in CookieBrowsers.ForCurrentOs())
            CookieBox.Items.Add(new ComboBoxItem { Content = browser.Label, Tag = browser.Id });
        CookieBox.SelectedIndex = 0;
        CookieHint.Text = CookieBrowsers.Hint(platform);
    }

    string? BrowserCookie()
    {
        if (CookieBox.SelectedIndex <= 0) return null;
        if (CookieBox.SelectedItem is ComboBoxItem item && item.Tag is string id && !string.IsNullOrWhiteSpace(id))
            return id;
        return null;
    }

    async Task StartDownload()
    {
        var mediaUrl = (UrlBox.Text ?? "").Trim();
        var outputDir = (FolderBox.Text ?? "").Trim();
        if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")) { await ShowInfo("Enter a valid http(s) URL."); return; }
        if (!Directory.Exists(outputDir)) { await ShowInfo("Choose a valid output folder."); return; }
        if (!File.Exists(YtDlp) || !File.Exists(Ffmpeg)) { await ShowInfo("Platform tools are missing. Reinstall this build or use a release that bundles yt-dlp + FFmpeg."); return; }

        SetBusy(true);
        jobCts?.Cancel();
        jobCts = new CancellationTokenSource();
        var ct = jobCts.Token;
        var startedUtc = DateTimeOffset.UtcNow;
        Progress.Value = 0;
        StatusText.Text = "Downloading — 0%";
        ProgressText.Text = "Starting yt-dlp...";

        try
        {
            var args = new List<string> { "--ignore-config", "--newline", "--no-playlist", "--ffmpeg-location", Ffmpeg,
                "--progress-template", "download:P:%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s|%(progress._downloaded_bytes_str)s|%(progress._total_bytes_str)s",
                "--print", "after_move:F:%(filepath)s", "-P", outputDir };

            var browser = BrowserCookie();
            if (!string.IsNullOrEmpty(browser)) { args.Add("--cookies-from-browser"); args.Add(browser); }

            var mode = Mode();
            if (mode == "original") args.AddRange(new[] { "-f", "bv*+ba/b", "-o", "%(title)s [%(id)s].%(ext)s" });
            else if (mode == "mp3") args.AddRange(new[] { "-x", "--audio-format", "mp3", "--audio-quality", "0", "-o", "%(title)s [%(id)s].%(ext)s" });
            else args.AddRange(new[] { "-f", "bv*[height<=1080]+ba/b[height<=1080]", "--merge-output-format", "mp4", "-o", mode == "x" ? "%(title)s [%(id)s].SOURCE.%(ext)s" : "%(title)s [%(id)s].%(ext)s" });

            if (TrimCheck.IsChecked == true)
            {
                args.Add("--download-sections");
                args.Add("*" + (string.IsNullOrWhiteSpace(TrimStart.Text) ? "0" : TrimStart.Text.Trim()) + "-" + (string.IsNullOrWhiteSpace(TrimEnd.Text) ? "inf" : TrimEnd.Text.Trim()));
                args.Add("--force-keyframes-at-cuts");
            }
            args.Add(mediaUrl);

            var errors = new StringBuilder();
            string finalPath = "";
            var rc = await ProcessRunner.RunAsync(YtDlp, args, (line, isErr) =>
            {
                if (isErr) { lock (errors) errors.AppendLine(line); return; }
                if (line.StartsWith("F:")) { finalPath = line[2..].Trim(); return; }
                if (!line.StartsWith("P:")) return;
                var parts = line[2..].Split('|');
                var pctText = parts.ElementAtOrDefault(0)?.Replace("%", "").Trim();
                if (double.TryParse(pctText, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                {
                    var value = Math.Clamp((int)Math.Round(pct), 0, 100);
                    Dispatcher.UIThread.Post(() => { Progress.Value = value; StatusText.Text = $"Downloading — {value}%"; });
                }
                var speed = Clean(parts.ElementAtOrDefault(1)); var eta = Clean(parts.ElementAtOrDefault(2));
                var got = Clean(parts.ElementAtOrDefault(3)); var total = Clean(parts.ElementAtOrDefault(4));
                var info = new List<string>(); if (speed != "") info.Add(speed); if (eta != "") info.Add("ETA " + eta); if (got != "" && total != "") info.Add(got + " / " + total);
                Dispatcher.UIThread.Post(() => ProgressText.Text = info.Count == 0 ? "Downloading media..." : string.Join("  •  ", info));
            }, ct);

            if (rc != 0) throw new Exception("yt-dlp failed:\n\n" + Tail(errors.ToString(), 2200));
            if (string.IsNullOrWhiteSpace(finalPath)) throw new Exception("Download completed but the output path was not reported.");

            if (mode == "x")
            {
                finalPath = await ConvertForX(finalPath, ct);
            }

            lastOutput = finalPath;
            Progress.Value = 100;
            StatusText.Text = "Done — 100%";
            ProgressText.Text = Path.GetFileName(finalPath);
            OpenFolderButton.IsEnabled = true;
            SaveFolder();
        }
        catch (OperationCanceledException)
        {
            IncompleteDownloadCleanup.DeleteLeftovers(outputDir, startedUtc);
            Progress.Value = 0;
            StatusText.Text = "Cancelled";
            ProgressText.Text = "The download or conversion was stopped.";
        }
        catch (Exception ex)
        {
            Progress.Value = 0;
            StatusText.Text = "Failed";
            ProgressText.Text = "The task did not complete.";
            await ShowInfo(ex.Message);
        }
        finally { SetBusy(false); }
    }

    async Task<string> ConvertForX(string source, CancellationToken cancellationToken)
    {
        var target = XTarget(source);
        var duration = await DurationSeconds(source);
        Dispatcher.UIThread.Post(() => { Progress.Value = 0; StatusText.Text = "Optimizing for X — 0%"; ProgressText.Text = duration > 0 ? "00:00 / " + FormatDuration(duration) : "Starting FFmpeg..."; });

        var errors = new StringBuilder();
        var args = XReadyEncode.ConversionArgs(source, target);

        var rc = await ProcessRunner.RunAsync(Ffmpeg, args, (line, isErr) =>
        {
            if (isErr) { lock (errors) errors.AppendLine(line); return; }
            if (!line.StartsWith("out_time=")) return;
            if (!TimeSpan.TryParse(line[9..].Trim(), CultureInfo.InvariantCulture, out var current)) return;
            var pct = duration > 0 ? Math.Clamp((int)Math.Round(current.TotalSeconds / duration * 100), 0, 99) : 0;
            Dispatcher.UIThread.Post(() => { if (duration > 0) { Progress.Value = pct; StatusText.Text = $"Optimizing for X — {pct}%"; ProgressText.Text = FormatDuration(current.TotalSeconds) + " / " + FormatDuration(duration); } else ProgressText.Text = "Processed " + FormatDuration(current.TotalSeconds); });
        }, cancellationToken);

        if (rc != 0) throw new Exception("FFmpeg conversion failed:\n\n" + Tail(errors.ToString(), 1800));
        try { if (File.Exists(source) && !source.Equals(target, StringComparison.OrdinalIgnoreCase)) File.Delete(source); } catch { }
        return target;
    }

    async Task<double> DurationSeconds(string file)
    {
        if (!File.Exists(Ffprobe)) return 0;
        try
        {
            var value = (await CaptureOutput(Ffprobe, new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", file })).Trim();
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        }
        catch { return 0; }
    }

    string XTarget(string source)
    {
        var stem = Path.GetFileNameWithoutExtension(source);
        if (stem.EndsWith(".SOURCE", StringComparison.OrdinalIgnoreCase)) stem = stem[..^7];
        return Path.Combine(Path.GetDirectoryName(source)!, stem + "_X.mp4");
    }

    async Task RefreshYtDlpVersions(bool pop)
    {
        try
        {
            if (!File.Exists(YtDlp)) { InstalledYtDlp.Text = "Current: missing"; LatestYtDlp.Text = "Latest: unknown"; return; }
            var installed = (await CaptureOutput(YtDlp, new[] { "--version" })).Trim();
            var latest = await LatestYtDlpVersion();
            InstalledYtDlp.Text = "Current: " + installed;
            LatestYtDlp.Text = "Latest: " + latest;
            RefreshToolBadges();
            if (pop) await ShowInfo(installed.TrimStart('v') == latest.TrimStart('v') ? "yt-dlp is up to date." : $"yt-dlp update available: {installed} → {latest}");
        }
        catch (Exception ex) { LatestYtDlp.Text = "Latest: check failed"; if (pop) await ShowInfo(ex.Message); }
    }

    static async Task<string> LatestYtDlpVersion()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SAPGUN-Media-Grabber", AppVersionInfo.Current));
        using var stream = await http.GetStreamAsync("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest");
        using var json = await JsonDocument.ParseAsync(stream);
        return json.RootElement.GetProperty("tag_name").GetString() ?? "unknown";
    }

    async Task<string> CaptureOutput(string exe, IEnumerable<string> args)
    {
        var output = new StringBuilder(); var errors = new StringBuilder();
        var rc = await ProcessRunner.RunAsync(exe, args, (line, err) => { lock (err ? errors : output) (err ? errors : output).AppendLine(line); });
        if (rc != 0) throw new Exception(Path.GetFileName(exe) + " returned " + rc + "\n" + Tail(errors.ToString(), 800));
        return output.ToString();
    }

    void SetBusy(bool busy)
    {
        DownloadButton.IsEnabled = !busy;
        CancelJobButton.IsEnabled = busy;
    }
    static string Clean(string? value) => string.IsNullOrWhiteSpace(value) || value is "NA" or "N/A" or "Unknown" ? "" : value.Trim();
    static string Tail(string value, int max) => value.Length <= max ? value : value[^max..];
    static string FormatDuration(double seconds) { var t = TimeSpan.FromSeconds(Math.Max(0, seconds)); return t.TotalHours >= 1 ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}" : $"{(int)t.TotalMinutes:00}:{t.Seconds:00}"; }

    void OpenTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return;
        try
        {
            if (OperatingSystem.IsMacOS()) Process.Start("open", new[] { target });
            else if (OperatingSystem.IsLinux()) Process.Start("xdg-open", new[] { target });
            else Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch { }
    }

    async Task ShowInfo(string message)
    {
        var dialog = new Window { Title = "SAPGUN Media Grabber", Width = 520, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var close = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, MinWidth = 90 };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel { Margin = new Thickness(20), Spacing = 16, Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, close } };
        await dialog.ShowDialog(this);
    }
}
