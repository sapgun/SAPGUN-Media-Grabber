using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SapgunMediaGrabber
{
    public class MainForm : Form
    {
        const string AppVersion = "0.2.2";

        readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        readonly string YtDlp;
        readonly string Ffmpeg;
        readonly string Ffprobe;

        TextBox url = new TextBox(), folder = new TextBox(), trimA = new TextBox(), trimB = new TextBox();
        RadioButton original = new RadioButton(), x1080 = new RadioButton(), mp4 = new RadioButton(), mp3 = new RadioButton();
        ComboBox cookies = new ComboBox();
        CheckBox trim = new CheckBox(), autoUpdate = new CheckBox();
        Button download = new Button(), open = new Button(), check = new Button(), update = new Button(), help = new Button();
        ProgressBar bar = new ProgressBar();
        Label status = new Label(), progressInfo = new Label(), installed = new Label(), latest = new Label();
        string lastOutput = "";

        public MainForm()
        {
            YtDlp = Path.Combine(BaseDir, "bin", "yt-dlp.exe");
            Ffmpeg = Path.Combine(BaseDir, "bin", "ffmpeg.exe");
            Ffprobe = Path.Combine(BaseDir, "bin", "ffprobe.exe");
            Text = "SAPGUN Media Grabber v" + AppVersion;
            Size = new Size(740, 850);
            MinimumSize = new Size(690, 790);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(14, 16, 21);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            BuildUi();
            Shown += async delegate { await RefreshVersions(false); if (autoUpdate.Checked) await UpdateYtDlp(true); };
        }

        Label L(string t, int s, bool bold)
        {
            return new Label { Text = t, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", s, bold ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 4, 0, 4) };
        }

        TextBox T()
        {
            return new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 27, 34), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        }

        Button B(string t)
        {
            var b = new Button { Text = t, Height = 36, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 34, 43), ForeColor = Color.White };
            b.FlatAppearance.BorderColor = Color.FromArgb(72, 79, 92);
            return b;
        }

        void Gap(TableLayoutPanel p, int h) { p.Controls.Add(new Panel { Height = h, Dock = DockStyle.Top }); }
        RadioButton R(string t, bool c) { return new RadioButton { Text = t, Checked = c, AutoSize = true, ForeColor = Color.White, Margin = new Padding(0, 4, 0, 7) }; }

        void BuildUi()
        {
            var head = new Panel { Dock = DockStyle.Top, Height = 108, Padding = new Padding(28, 16, 28, 4) };
            var e = L("LOCAL  •  yt-dlp + FFmpeg  •  v" + AppVersion, 9, true); e.ForeColor = Color.FromArgb(126, 164, 255); e.Location = new Point(28, 14); head.Controls.Add(e);
            var title = L("SAPGUN Media Grabber", 23, true); title.Location = new Point(26, 40); head.Controls.Add(title);
            var sub = L("Paste a URL. Pick a profile. Download.", 9, false); sub.ForeColor = Color.FromArgb(169, 176, 190); sub.Location = new Point(29, 79); head.Controls.Add(sub);
            Controls.Add(head);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 4, 28, 20), AutoScroll = true };
            Controls.Add(body); body.BringToFront();
            var p = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.Controls.Add(p);

            var u = L("MEDIA URL", 9, true); u.ForeColor = Color.Silver; p.Controls.Add(u);
            var ur = new TableLayoutPanel { Dock = DockStyle.Top, Height = 42, ColumnCount = 2 };
            ur.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); ur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            url = T(); url.Margin = new Padding(0, 3, 8, 3);
            var paste = B("Paste"); paste.Click += delegate { try { if (Clipboard.ContainsText()) url.Text = Clipboard.GetText(); } catch { } };
            ur.Controls.Add(url, 0, 0); ur.Controls.Add(paste, 1, 0); p.Controls.Add(ur);

            Gap(p, 12); var pl = L("OUTPUT PROFILE", 9, true); pl.ForeColor = Color.Silver; p.Controls.Add(pl);
            var pf = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
            original = R("Original Best — highest available quality", false);
            x1080 = R("X / Twitter 1080p — H.264 + AAC, upload-safe", true);
            mp4 = R("1080p MP4 — quick MP4 download / merge", false);
            mp3 = R("Audio MP3 — high quality extraction", false);
            pf.Controls.Add(original); pf.Controls.Add(x1080); pf.Controls.Add(mp4); pf.Controls.Add(mp3); p.Controls.Add(pf);

            Gap(p, 8); var cl = L("BROWSER COOKIES", 9, true); cl.ForeColor = Color.Silver; p.Controls.Add(cl);
            cookies = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(24, 27, 34), ForeColor = Color.White };
            cookies.Items.AddRange(new object[] { "None (public media)", "Edge", "Chrome", "Brave", "Firefox" }); cookies.SelectedIndex = 0; p.Controls.Add(cookies);
            var ch = L("403 / sign-in errors? Select the browser where you are already signed in.", 8, false); ch.ForeColor = Color.Gray; p.Controls.Add(ch);

            Gap(p, 10); trim = new CheckBox { Text = "Trim / download a section", AutoSize = true, ForeColor = Color.White }; p.Controls.Add(trim);
            var tr = new TableLayoutPanel { Dock = DockStyle.Top, Height = 38, ColumnCount = 3 };
            tr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); tr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32)); tr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            trimA = T(); trimA.Text = "00:00"; trimA.Enabled = false;
            trimB = T(); trimB.Enabled = false;
            var ar = L("→", 12, true); ar.Dock = DockStyle.Fill; ar.TextAlign = ContentAlignment.MiddleCenter;
            tr.Controls.Add(trimA, 0, 0); tr.Controls.Add(ar, 1, 0); tr.Controls.Add(trimB, 2, 0); p.Controls.Add(tr);
            trim.CheckedChanged += delegate { trimA.Enabled = trim.Checked; trimB.Enabled = trim.Checked; };

            Gap(p, 10); var sl = L("SAVE TO", 9, true); sl.ForeColor = Color.Silver; p.Controls.Add(sl);
            var fr = new TableLayoutPanel { Dock = DockStyle.Top, Height = 42, ColumnCount = 2 };
            fr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); fr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            folder = T(); folder.ReadOnly = true; folder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"); folder.Margin = new Padding(0, 3, 8, 3);
            var browse = B("Browse"); browse.Click += delegate { using (var d = new FolderBrowserDialog()) { d.SelectedPath = folder.Text; if (d.ShowDialog(this) == DialogResult.OK) folder.Text = d.SelectedPath; } };
            fr.Controls.Add(folder, 0, 0); fr.Controls.Add(browse, 1, 0); p.Controls.Add(fr);

            Gap(p, 15); download = B("DOWNLOAD"); download.Height = 48; download.BackColor = Color.FromArgb(76, 111, 255); download.Font = new Font("Segoe UI", 11, FontStyle.Bold); download.Click += async delegate { await StartDownload(); }; p.Controls.Add(download);
            Gap(p, 10); bar = new ProgressBar { Dock = DockStyle.Top, Height = 12, Minimum = 0, Maximum = 100 }; p.Controls.Add(bar);
            status = L("Ready", 9, true); status.ForeColor = Color.LightGreen; p.Controls.Add(status);
            progressInfo = L("Waiting for a download.", 8, false); progressInfo.ForeColor = Color.FromArgb(158, 166, 180); progressInfo.AutoEllipsis = true; progressInfo.MaximumSize = new Size(650, 0); p.Controls.Add(progressInfo);
            open = B("Open Output Folder"); open.Enabled = false; open.Click += delegate { string d = File.Exists(lastOutput) ? Path.GetDirectoryName(lastOutput) : folder.Text; if (Directory.Exists(d)) Process.Start("explorer.exe", Q(d)); }; p.Controls.Add(open);

            Gap(p, 14); var tv = L("TOOLS & VERSION", 9, true); tv.ForeColor = Color.Silver; p.Controls.Add(tv);
            var vr = new TableLayoutPanel { Dock = DockStyle.Top, Height = 28, ColumnCount = 2 };
            vr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); vr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            installed = L("Installed yt-dlp: checking...", 8, false); latest = L("Latest: checking...", 8, false); latest.Dock = DockStyle.Fill; latest.TextAlign = ContentAlignment.MiddleRight;
            vr.Controls.Add(installed, 0, 0); vr.Controls.Add(latest, 1, 0); p.Controls.Add(vr);
            var br = new TableLayoutPanel { Dock = DockStyle.Top, Height = 42, ColumnCount = 3 };
            br.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); br.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); br.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            check = B("Check version"); update = B("Update yt-dlp"); help = B("How to use");
            check.Click += async delegate { await RefreshVersions(true); }; update.Click += async delegate { await UpdateYtDlp(false); }; help.Click += delegate { ShowHelp(); };
            br.Controls.Add(check, 0, 0); br.Controls.Add(update, 1, 0); br.Controls.Add(help, 2, 0); p.Controls.Add(br);
            autoUpdate = new CheckBox { Text = "Auto-update yt-dlp when the app starts", AutoSize = true, ForeColor = Color.Silver }; p.Controls.Add(autoUpdate);
        }

        string Mode() { if (original.Checked) return "original"; if (mp4.Checked) return "mp4"; if (mp3.Checked) return "mp3"; return "x"; }
        string Browser() { return cookies.SelectedIndex <= 0 ? null : cookies.SelectedItem.ToString().ToLowerInvariant(); }
        string Norm(string v) { return (v ?? "").Trim().TrimStart('v'); }

        async Task<string> LatestVersion()
        {
            return await Task.Run(delegate
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var r = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest");
                r.UserAgent = "SAPGUN-Media-Grabber/" + AppVersion;
                using (var res = r.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream()))
                {
                    var m = Regex.Match(sr.ReadToEnd(), "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    if (!m.Success) throw new Exception("Could not read latest yt-dlp version.");
                    return m.Groups[1].Value;
                }
            });
        }

        async Task RefreshVersions(bool pop)
        {
            try
            {
                string a = File.Exists(YtDlp) ? (await CaptureOutput(YtDlp, new[] { "--version" })).Trim() : "missing";
                string b = await LatestVersion();
                installed.Text = "Installed yt-dlp: " + a; latest.Text = "Latest: " + b;
                bool old = Norm(a) != Norm(b); installed.ForeColor = old ? Color.Orange : Color.LightGreen; update.Enabled = old;
                if (pop) MessageBox.Show(old ? "A newer yt-dlp version is available." : "yt-dlp is up to date.");
            }
            catch (Exception ex) { latest.Text = "Latest: check failed"; if (pop) MessageBox.Show(ex.Message); }
        }

        async Task UpdateYtDlp(bool quiet)
        {
            try
            {
                string cur = (await CaptureOutput(YtDlp, new[] { "--version" })).Trim(); string lv = await LatestVersion();
                if (Norm(cur) == Norm(lv)) { if (!quiet) MessageBox.Show("yt-dlp is already up to date."); return; }
                status.Text = "Updating yt-dlp..."; progressInfo.Text = cur + " → " + lv;
                await Task.Run(delegate
                {
                    string tmp = YtDlp + ".new";
                    using (var wc = new WebClient()) { wc.Headers.Add("User-Agent", "SAPGUN-Media-Grabber/" + AppVersion); wc.DownloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", tmp); }
                    File.Copy(tmp, YtDlp, true); File.Delete(tmp);
                });
                status.Text = "yt-dlp updated"; progressInfo.Text = "Current version: " + lv; await RefreshVersions(false);
                if (!quiet) MessageBox.Show("yt-dlp updated to " + lv + ".");
            }
            catch (Exception ex) { if (!quiet) MessageBox.Show(ex.Message, "Update failed"); }
        }

        void ShowHelp()
        {
            MessageBox.Show("1. Paste a media URL.\r\n\r\n2. Choose a profile.\r\nX / Twitter 1080p converts to H.264 + AAC.\r\n\r\n3. Progress shows percentage, speed, ETA and downloaded size.\r\nFor X conversion, it also shows conversion percentage and elapsed media time.\r\n\r\n4. For HTTP 403 or sign-in errors, choose Edge/Chrome/Brave/Firefox under Browser Cookies and retry.\r\n\r\n5. Use Check version / Update yt-dlp when sites change.\r\n\r\n6. Trim is optional, e.g. 00:30 to 01:15.\r\n\r\nOnly download media you are allowed to download.", "How to use");
        }

        async Task StartDownload()
        {
            if (!url.Text.StartsWith("http://") && !url.Text.StartsWith("https://")) { MessageBox.Show("Enter a valid URL."); return; }
            if (!Directory.Exists(folder.Text)) { MessageBox.Show("Choose an output folder."); return; }
            if (!File.Exists(YtDlp) || !File.Exists(Ffmpeg)) { MessageBox.Show("Bundled yt-dlp or FFmpeg is missing. Reinstall the app."); return; }

            Busy(true); bar.Style = ProgressBarStyle.Continuous; bar.Value = 0; status.Text = "Downloading — 0%"; status.ForeColor = Color.FromArgb(126, 164, 255); progressInfo.Text = "Starting yt-dlp...";

            try
            {
                var a = new List<string>
                {
                    "--ignore-config", "--newline", "--no-playlist", "--ffmpeg-location", Ffmpeg,
                    "--progress-template", "download:P:%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s|%(progress._downloaded_bytes_str)s|%(progress._total_bytes_str)s",
                    "--print", "after_move:F:%(filepath)s", "-P", folder.Text
                };

                string b = Browser(); if (b != null) { a.Add("--cookies-from-browser"); a.Add(b); }
                string m = Mode();
                if (m == "original") a.AddRange(new[] { "-f", "bv*+ba/b", "-o", "%(title)s [%(id)s].%(ext)s" });
                else if (m == "mp3") a.AddRange(new[] { "-x", "--audio-format", "mp3", "--audio-quality", "0", "-o", "%(title)s [%(id)s].%(ext)s" });
                else a.AddRange(new[] { "-f", "bv*[height<=1080]+ba/b[height<=1080]", "--merge-output-format", "mp4", "-o", m == "x" ? "%(title)s [%(id)s].SOURCE.%(ext)s" : "%(title)s [%(id)s].%(ext)s" });

                if (trim.Checked) { a.Add("--download-sections"); a.Add("*" + (trimA.Text.Trim() == "" ? "0" : trimA.Text.Trim()) + "-" + (trimB.Text.Trim() == "" ? "inf" : trimB.Text.Trim())); a.Add("--force-keyframes-at-cuts"); }
                a.Add(url.Text.Trim());

                var err = new StringBuilder(); string final = "";
                int rc = await Run(YtDlp, a, delegate(string line, bool er)
                {
                    if (er) { err.AppendLine(line); return; }
                    if (line.StartsWith("F:")) { final = line.Substring(2).Trim(); status.Text = "Download complete"; progressInfo.Text = "Preparing output..."; bar.Value = 100; return; }
                    if (line.StartsWith("P:"))
                    {
                        string[] parts = line.Substring(2).Split('|');
                        double pct; string pctText = parts.Length > 0 ? parts[0].Replace("%", "").Trim() : "";
                        if (Double.TryParse(pctText, NumberStyles.Any, CultureInfo.InvariantCulture, out pct)) { int value = Math.Max(0, Math.Min(100, (int)Math.Round(pct))); bar.Value = value; status.Text = "Downloading — " + value + "%"; }
                        else status.Text = "Downloading";

                        string speed = parts.Length > 1 ? CleanMetric(parts[1]) : "";
                        string eta = parts.Length > 2 ? CleanMetric(parts[2]) : "";
                        string got = parts.Length > 3 ? CleanMetric(parts[3]) : "";
                        string total = parts.Length > 4 ? CleanMetric(parts[4]) : "";
                        var info = new List<string>();
                        if (speed != "") info.Add(speed); if (eta != "") info.Add("ETA " + eta);
                        if (got != "" && total != "") info.Add(got + " / " + total); else if (got != "") info.Add(got);
                        progressInfo.Text = info.Count > 0 ? String.Join("  •  ", info.ToArray()) : "Downloading media...";
                    }
                });

                if (rc != 0)
                {
                    string e = Tail(err.ToString(), 2200);
                    if (e.IndexOf("403", StringComparison.OrdinalIgnoreCase) >= 0 && cookies.SelectedIndex == 0) e += "\r\n\r\nTIP: Select Browser Cookies and retry. Also update yt-dlp.";
                    if (e.IndexOf("WinError 448", StringComparison.OrdinalIgnoreCase) >= 0) e += "\r\n\r\nTIP: Windows blocked an external junction. This app isolates its bundled FFmpeg and ignores user yt-dlp config.";
                    throw new Exception("yt-dlp failed:\r\n\r\n" + e);
                }

                if (final == "") throw new Exception("Output file was not reported.");
                if (m == "x") { await ConvertForX(final); final = XTarget(final); }

                lastOutput = final; bar.Style = ProgressBarStyle.Continuous; bar.Value = 100; status.Text = "Done — 100%"; status.ForeColor = Color.LightGreen; progressInfo.Text = Path.GetFileName(final); open.Enabled = true;
            }
            catch (Exception ex)
            {
                bar.Style = ProgressBarStyle.Continuous; bar.Value = 0; status.Text = "Failed"; status.ForeColor = Color.Salmon; progressInfo.Text = "The task did not complete."; MessageBox.Show(ex.Message, "SAPGUN Media Grabber");
            }
            finally { Busy(false); }
        }

        async Task ConvertForX(string source)
        {
            string target = XTarget(source); double duration = await GetDurationSeconds(source);
            status.Text = "Optimizing for X — 0%"; status.ForeColor = Color.FromArgb(126, 164, 255); progressInfo.Text = duration > 0 ? "00:00 / " + FormatDuration(duration) : "Starting FFmpeg..."; bar.Style = ProgressBarStyle.Continuous; bar.Value = 0;

            var ff = new List<string>
            {
                "-y", "-hide_banner", "-loglevel", "error", "-i", source,
                "-map", "0:v:0", "-map", "0:a:0?", "-c:v", "libx264", "-preset", "medium", "-profile:v", "high", "-level", "4.1", "-pix_fmt", "yuv420p",
                "-vf", "scale='min(1920,iw)':-2:force_original_aspect_ratio=decrease", "-fpsmax", "30", "-crf", "20", "-maxrate", "8M", "-bufsize", "16M",
                "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2", "-movflags", "+faststart", "-progress", "pipe:1", "-nostats", "-f", "mp4", target
            };

            var fe = new StringBuilder();
            int frc = await Run(Ffmpeg, ff, delegate(string line, bool er)
            {
                if (er) { fe.AppendLine(line); return; }
                if (line.StartsWith("out_time="))
                {
                    TimeSpan current; string value = line.Substring("out_time=".Length).Trim();
                    if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out current))
                    {
                        if (duration > 0) { int pct = Math.Max(0, Math.Min(99, (int)Math.Round(current.TotalSeconds / duration * 100.0))); bar.Value = pct; status.Text = "Optimizing for X — " + pct + "%"; progressInfo.Text = FormatDuration(current.TotalSeconds) + " / " + FormatDuration(duration); }
                        else progressInfo.Text = "Processed " + FormatDuration(current.TotalSeconds);
                    }
                }
                else if (line == "progress=end") { bar.Value = 100; status.Text = "Optimizing for X — 100%"; progressInfo.Text = duration > 0 ? FormatDuration(duration) + " / " + FormatDuration(duration) : "Conversion complete"; }
            });

            if (frc != 0) throw new Exception("FFmpeg failed:\r\n" + Tail(fe.ToString(), 1800));
            try { if (File.Exists(source) && !String.Equals(source, target, StringComparison.OrdinalIgnoreCase)) File.Delete(source); } catch { }
        }

        async Task<double> GetDurationSeconds(string file)
        {
            if (!File.Exists(Ffprobe)) return 0;
            try
            {
                string value = (await CaptureOutput(Ffprobe, new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", file })).Trim();
                double d; if (Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            }
            catch { }
            return 0;
        }

        string XTarget(string s) { string stem = Path.GetFileNameWithoutExtension(s); if (stem.EndsWith(".SOURCE", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 7); return Path.Combine(Path.GetDirectoryName(s), stem + "_X.mp4"); }
        void Busy(bool b) { download.Enabled = !b; url.Enabled = !b; cookies.Enabled = !b; original.Enabled = !b; x1080.Enabled = !b; mp4.Enabled = !b; mp3.Enabled = !b; trim.Enabled = !b; check.Enabled = !b; update.Enabled = !b; }

        async Task<string> CaptureOutput(string exe, string[] args)
        {
            var sb = new StringBuilder(); var err = new StringBuilder();
            int rc = await Run(exe, new List<string>(args), delegate(string l, bool e) { if (e) err.AppendLine(l); else sb.AppendLine(l); });
            if (rc != 0) throw new Exception(Path.GetFileName(exe) + " returned " + rc + "\r\n" + Tail(err.ToString(), 800));
            return sb.ToString();
        }

        async Task<int> Run(string exe, IList<string> args, Action<string, bool> onLine)
        {
            var ps = new ProcessStartInfo { FileName = exe, Arguments = Join(args), UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
            string inheritedPath = ps.EnvironmentVariables["PATH"] ?? "";
            var safePath = new List<string>(); string bundledBin = Path.Combine(BaseDir, "bin"); safePath.Add(bundledBin);
            foreach (string raw in inheritedPath.Split(';'))
            {
                string entry = raw.Trim().Trim('"'); if (entry.Length == 0) continue;
                string lower = entry.ToLowerInvariant();
                if (lower.Contains(@"\scoop\apps\ffmpeg\current") || lower.Contains(@"\scoop\apps\ffmpeg-shared\current")) continue;
                if (String.Equals(entry, bundledBin, StringComparison.OrdinalIgnoreCase)) continue;
                safePath.Add(entry);
            }
            ps.EnvironmentVariables["PATH"] = String.Join(";", safePath.ToArray());

            using (var p = new Process())
            {
                p.StartInfo = ps; p.Start();
                var ot = Task.Run(delegate { string l; while ((l = p.StandardOutput.ReadLine()) != null) { string c = l; try { Invoke((MethodInvoker)delegate { onLine(c, false); }); } catch { } } });
                var et = Task.Run(delegate { string l; while ((l = p.StandardError.ReadLine()) != null) { string c = l; try { Invoke((MethodInvoker)delegate { onLine(c, true); }); } catch { } } });
                await Task.Run(delegate { p.WaitForExit(); }); await Task.WhenAll(ot, et); return p.ExitCode;
            }
        }

        static string CleanMetric(string s) { if (s == null) return ""; s = s.Trim(); if (s == "" || s == "NA" || s == "N/A" || s == "Unknown") return ""; return s; }
        static string FormatDuration(double seconds) { if (seconds < 0) seconds = 0; TimeSpan t = TimeSpan.FromSeconds(seconds); if (t.TotalHours >= 1) return String.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds); return String.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", (int)t.TotalMinutes, t.Seconds); }

        static string Join(IList<string> a) { var s = new StringBuilder(); for (int i = 0; i < a.Count; i++) { if (i > 0) s.Append(' '); s.Append(Q(a[i])); } return s.ToString(); }
        static string Q(string a)
        {
            if (a == null) return "\"\"";
            if (a.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return a;
            var sb = new StringBuilder(); sb.Append('"'); int slash = 0;
            for (int i = 0; i < a.Length; i++)
            {
                char c = a[i];
                if (c == '\\') { slash++; continue; }
                if (c == '"') { sb.Append('\\', slash * 2 + 1); sb.Append('"'); slash = 0; continue; }
                if (slash > 0) { sb.Append('\\', slash); slash = 0; }
                sb.Append(c);
            }
            if (slash > 0) sb.Append('\\', slash * 2); sb.Append('"'); return sb.ToString();
        }
        static string Tail(string s, int n) { if (String.IsNullOrEmpty(s)) return ""; return s.Length <= n ? s : s.Substring(s.Length - n); }

        [STAThread]
        public static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
    }
}
