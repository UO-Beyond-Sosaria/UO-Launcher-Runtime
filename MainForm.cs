using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;

namespace UOBeyondSosariaLauncher
{
    public partial class MainForm : Form
    {
        private static readonly string GAME_TITLE = "UO Beyond Sosaria";
        private static readonly string CLIENT_DOWNLOAD_URL = "https://www.beyondsosaria.com/downloads/UO-BeyondSosaria.zip";
        private static readonly string DESTINATION_PATH = Path.Combine(Application.StartupPath, "data", "client");
        private static readonly string ZIP_PATH = Path.Combine(DESTINATION_PATH, "client.zip");
        private static readonly string CLASSICUO_PATH = Path.Combine(DESTINATION_PATH, "ClassicUO.exe");

        private readonly HttpClient httpClient;
        private CancellationTokenSource cancellationTokenSource;
        private TextProgressBar progressBar;
        private Label statusLabel;
        private Button razorEnhancedButton;
        private Button classicAssistButton;
        private Button razorButton;
        private Button installButton;
        private PictureBox logoBox;

        public MainForm()
        {
            // Force modern TLS for HTTPS connections
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Initialize HttpClient
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30); // 30 minute timeout for large files

            InitializeComponent();
            CheckClientInstallation();
        }

        private void InitializeComponent()
        {
            this.Text = GAME_TITLE + " Launcher";
            this.Size = new Size(765, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.Black;

            // Set application icon
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "launcher_icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch
            {
                // Ignore icon loading errors
            }

            // Logo background image
            logoBox = new PictureBox();
            try
            {
                string logoPath = Path.Combine(Application.StartupPath, "launcher_logo.png");
                if (File.Exists(logoPath))
                {
                    logoBox.Image = Image.FromFile(logoPath);
                    logoBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    logoBox.Location = new Point(0, 0);
                    logoBox.Size = this.ClientSize;
                    logoBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    this.Controls.Add(logoBox);
                    logoBox.SendToBack();
                }
            }
            catch
            {
                // Ignore logo loading errors
            }

            // Status label (hidden during download, only for completion)
            statusLabel = new Label();
            statusLabel.Text = "";
            statusLabel.Location = new Point(75, 240);
            statusLabel.Size = new Size(550, 40);
            statusLabel.Font = new Font("Arial", 14f, FontStyle.Bold);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.ForeColor = Color.LightGray;
            statusLabel.BackColor = Color.Black; // Avoid transparency issues
            statusLabel.BorderStyle = BorderStyle.None;
            statusLabel.Visible = false;
            this.Controls.Add(statusLabel);

            // Progress bar with built-in text
            progressBar = new TextProgressBar();
            progressBar.Location = new Point(75, 240);
            progressBar.Size = new Size(550, 40);
            progressBar.Font = new Font("Arial", 12f, FontStyle.Bold);
            progressBar.Visible = false;
            this.Controls.Add(progressBar);

            // Install button - RPG Black/Silver Style
            installButton = new Button();
            installButton.Text = "\u2699 INSTALL CLIENT \u2699";
            installButton.Location = new Point(257, 540);
            installButton.Size = new Size(250, 50);
            installButton.Font = new Font("Times New Roman", 12f, FontStyle.Bold);
            installButton.BackColor = Color.FromArgb(19, 20, 25);
            installButton.ForeColor = Color.LightGray;
            installButton.FlatStyle = FlatStyle.Flat;
            installButton.FlatAppearance.BorderColor = Color.Silver;
            installButton.FlatAppearance.BorderSize = 2;
            installButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 31, 36);
            installButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 11, 16);
            installButton.UseVisualStyleBackColor = false;
            installButton.Cursor = Cursors.Hand;
            installButton.Click += InstallButton_Click;
            installButton.Paint += InstallButton_Paint;
            installButton.Visible = false;
            this.Controls.Add(installButton);

            // RazorEnhanced button (left) - RPG Black/Silver Style
            razorEnhancedButton = new Button();
            razorEnhancedButton.Text = "⚔ RAZORENHANCED ⚔";
            razorEnhancedButton.Location = new Point(15, 560);
            razorEnhancedButton.Size = new Size(220, 50);
            razorEnhancedButton.Font = new Font("Times New Roman", 11f, FontStyle.Bold);
            razorEnhancedButton.BackColor = Color.FromArgb(19, 20, 25);
            razorEnhancedButton.ForeColor = Color.LightGray;
            razorEnhancedButton.FlatStyle = FlatStyle.Flat;
            razorEnhancedButton.FlatAppearance.BorderColor = Color.Silver;
            razorEnhancedButton.FlatAppearance.BorderSize = 2;
            razorEnhancedButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 31, 36);
            razorEnhancedButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 11, 16);
            razorEnhancedButton.UseVisualStyleBackColor = false;
            razorEnhancedButton.Cursor = Cursors.Hand;
            razorEnhancedButton.Click += RazorEnhancedButton_Click;
            razorEnhancedButton.Visible = false;
            this.Controls.Add(razorEnhancedButton);

            // ClassicAssist button (center) - RPG Black/Silver Style
            classicAssistButton = new Button();
            classicAssistButton.Text = "◈ CLASSICASSIST ◈";
            classicAssistButton.Location = new Point(265, 560);
            classicAssistButton.Size = new Size(220, 50);
            classicAssistButton.Font = new Font("Times New Roman", 11f, FontStyle.Bold);
            classicAssistButton.BackColor = Color.FromArgb(19, 20, 25);
            classicAssistButton.ForeColor = Color.LightGray;
            classicAssistButton.FlatStyle = FlatStyle.Flat;
            classicAssistButton.FlatAppearance.BorderColor = Color.Silver;
            classicAssistButton.FlatAppearance.BorderSize = 2;
            classicAssistButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 31, 36);
            classicAssistButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 11, 16);
            classicAssistButton.UseVisualStyleBackColor = false;
            classicAssistButton.Cursor = Cursors.Hand;
            classicAssistButton.Click += ClassicAssistButton_Click;
            classicAssistButton.Visible = false;
            this.Controls.Add(classicAssistButton);

            // Razor button (right) - RPG Black/Silver Style
            razorButton = new Button();
            razorButton.Text = "⚡ RAZOR ⚡";
            razorButton.Location = new Point(515, 560);
            razorButton.Size = new Size(220, 50);
            razorButton.Font = new Font("Times New Roman", 11f, FontStyle.Bold);
            razorButton.BackColor = Color.FromArgb(19, 20, 25);
            razorButton.ForeColor = Color.LightGray;
            razorButton.FlatStyle = FlatStyle.Flat;
            razorButton.FlatAppearance.BorderColor = Color.Silver;
            razorButton.FlatAppearance.BorderSize = 2;
            razorButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 31, 36);
            razorButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 11, 16);
            razorButton.UseVisualStyleBackColor = false;
            razorButton.Cursor = Cursors.Hand;
            razorButton.Click += RazorButton_Click;
            razorButton.Visible = false;
            this.Controls.Add(razorButton);


            // Bring controls to front
            installButton.BringToFront();
            razorEnhancedButton.BringToFront();
            classicAssistButton.BringToFront();
            razorButton.BringToFront();
            statusLabel.BringToFront();
            progressBar.BringToFront();
        }

        private void CheckClientInstallation()
        {
            if (File.Exists(CLASSICUO_PATH))
            {
                // Show all 3 client buttons since they all use ClassicUO.exe
                razorEnhancedButton.Visible = true;
                classicAssistButton.Visible = true;
                razorButton.Visible = true;
            }
            else
            {
                installButton.Visible = true;
            }
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            StartClientDownload();
        }

        private void InstallButton_Paint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            if (!btn.Enabled)
            {
                // Draw custom text when disabled to maintain light gray color
                using (Brush textBrush = new SolidBrush(Color.LightGray))
                {
                    SizeF textSize = e.Graphics.MeasureString(btn.Text, btn.Font);
                    PointF textLocation = new PointF(
                        (btn.Width - textSize.Width) / 2,
                        (btn.Height - textSize.Height) / 2
                    );
                    e.Graphics.DrawString(btn.Text, btn.Font, textBrush, textLocation);
                }
            }
        }

        private void RazorEnhancedButton_Click(object sender, EventArgs e)
        {
            LaunchArguments launchArguments = new LaunchArguments();
            launchArguments.Append("ip", "play.beyondsosaria.com");
            launchArguments.Append("port", "2593");
            launchArguments.Append("plugins", ".//RazorEnhanced//RazorEnhanced.exe");
            LaunchClassicUO(launchArguments);
        }

        private void ClassicAssistButton_Click(object sender, EventArgs e)
        {
            LaunchArguments launchArguments = new LaunchArguments();
            launchArguments.Append("ip", "play.beyondsosaria.com");
            launchArguments.Append("port", "2593");
            launchArguments.Append("plugins", ".//ClassicAssist//ClassicAssist.dll");
            LaunchClassicUO(launchArguments);
        }

        private void RazorButton_Click(object sender, EventArgs e)
        {
            LaunchArguments launchArguments = new LaunchArguments();
            launchArguments.Append("ip", "play.beyondsosaria.com");
            launchArguments.Append("port", "2593");
            launchArguments.Append("plugins", ".//RazorCE//Razor.exe");
            LaunchClassicUO(launchArguments);
        }

        private void LaunchClassicUO(LaunchArguments launchArguments)
        {
            try
            {
                if (!File.Exists(CLASSICUO_PATH))
                {
                    MessageBox.Show($"ClassicUO.exe not found at:\n{CLASSICUO_PATH}\n\nPlease reinstall the client.",
                                  "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CheckClientInstallation();
                    statusLabel.Text = "";
                    return;
                }

                string clientDirectory = Path.GetDirectoryName(CLASSICUO_PATH);
                string arguments = launchArguments.ToString();

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WorkingDirectory = clientDirectory,
                    Arguments = arguments,
                    CreateNoWindow = false,
                    FileName = CLASSICUO_PATH
                };

                Process.Start(startInfo);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching ClassicUO: {ex.Message}\n\nPath: {CLASSICUO_PATH}",
                              "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void StartClientDownload()
        {
            statusLabel.Visible = false;
            progressBar.DisplayText = "Downloading UO Beyond Sosaria client...";
            progressBar.Visible = true;
            installButton.Text = "⚙ INSTALLING... ⚙";
            installButton.Enabled = false;

            Directory.CreateDirectory(DESTINATION_PATH);
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await DownloadFileWithResumeAsync(CLIENT_DOWNLOAD_URL, ZIP_PATH, cancellationTokenSource.Token);
                ExtractClient();
            }
            catch (OperationCanceledException)
            {
                // Download was cancelled
                installButton.Text = "⚙ INSTALL CLIENT ⚙";
                installButton.Enabled = true;
                progressBar.Visible = false;
            }
            catch (Exception ex)
            {
                ShowRetryDialog(ex);
            }
        }

        private async Task DownloadFileWithResumeAsync(string url, string destinationPath, CancellationToken cancellationToken)
        {
            long existingFileSize = 0;
            if (File.Exists(destinationPath))
            {
                existingFileSize = new FileInfo(destinationPath).Length;
            }

            int retryCount = 0;
            const int maxRetries = 5;

            while (retryCount < maxRetries)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        progressBar.DisplayText = $"Retrying download... (Attempt {retryCount + 1}/{maxRetries})";
                        Application.DoEvents();
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                    }

                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        if (existingFileSize > 0)
                        {
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingFileSize, null);
                            progressBar.DisplayText = $"Resuming download from {existingFileSize / 1024 / 1024:F1} MB...";
                        }

                        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                        {
                            response.EnsureSuccessStatusCode();

                            long? totalBytes = response.Content.Headers.ContentLength;
                            if (response.StatusCode == HttpStatusCode.PartialContent)
                            {
                                totalBytes = existingFileSize + response.Content.Headers.ContentLength;
                            }

                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = new FileStream(destinationPath,
                                existingFileSize > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write))
                            {
                                var buffer = new byte[8192];
                                long totalBytesRead = existingFileSize;
                                int bytesRead;

                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                    totalBytesRead += bytesRead;

                                    if (totalBytes.HasValue)
                                    {
                                        int progressPercentage = (int)((totalBytesRead * 100) / totalBytes.Value);
                                        double receivedMB = (double)totalBytesRead / 1024 / 1024;
                                        double totalMB = (double)totalBytes.Value / 1024 / 1024;

                                        progressBar.Value = Math.Min(progressPercentage, 100);
                                        progressBar.DisplayText = $"Downloading... {progressPercentage}% ({receivedMB:F1} MB / {totalMB:F1} MB)";
                                        Application.DoEvents();
                                    }
                                }
                            }
                        }
                    }
                    return; // Success, exit retry loop
                }
                catch (HttpRequestException) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    // Will retry
                }
                catch (TaskCanceledException) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    // Will retry
                }
            }

            // If we get here, all retries failed
            throw new Exception("Download failed after " + maxRetries + " attempts");
        }

        private void ShowRetryDialog(Exception ex)
        {
            DialogResult result = MessageBox.Show(
                "Download failed: " + ex.Message + "\n\nWould you like to retry?",
                "Download Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);

            if (result == DialogResult.Yes)
            {
                StartClientDownload(); // This will resume from where it left off
            }
            else
            {
                // Cancel download
                installButton.Text = "⚙ INSTALL CLIENT ⚙";
                installButton.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private bool VerifyZipFile(string zipPath)
        {
            try
            {
                // Check if file exists
                if (!File.Exists(zipPath))
                {
                    return false;
                }

                // Check file size (should be > 1MB for a valid client)
                var fileInfo = new FileInfo(zipPath);
                if (fileInfo.Length < 1048576) // 1MB minimum
                {
                    return false;
                }

                // Verify ZIP file integrity by attempting to read the central directory
                using (var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                {
                    using (var zipStream = new ZipInputStream(fileStream))
                    {
                        // Try to read at least one entry to verify ZIP structure
                        var firstEntry = zipStream.GetNextEntry();
                        return firstEntry != null;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void ExtractClient()
        {
            try
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.DisplayText = "Verifying download...";
                Application.DoEvents();

                // Verify ZIP file before extraction
                if (!VerifyZipFile(ZIP_PATH))
                {
                    throw new Exception("Downloaded file is corrupted or invalid. Please try downloading again.");
                }

                progressBar.DisplayText = "Extracting client files...";

                using (FileStream fileStream = new FileStream(ZIP_PATH, FileMode.Open, FileAccess.Read))
                {
                    using (ZipInputStream zipStream = new ZipInputStream(fileStream))
                    {
                        ZipEntry entry;
                        while ((entry = zipStream.GetNextEntry()) != null)
                        {
                            if (entry.IsFile)
                            {
                                // Update progress bar to show current file being extracted
                                string fileName = Path.GetFileName(entry.Name);
                                progressBar.DisplayText = $"Extracting: {fileName}";
                                Application.DoEvents(); // Allow UI to update

                                string entryPath = Path.Combine(DESTINATION_PATH, entry.Name);
                                string directoryPath = Path.GetDirectoryName(entryPath);

                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }

                                using (FileStream outputStream = File.Create(entryPath))
                                {
                                    zipStream.CopyTo(outputStream);
                                }
                            }
                            else if (entry.IsDirectory)
                            {
                                string directoryPath = Path.Combine(DESTINATION_PATH, entry.Name);
                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                            }
                        }
                    }
                }

                // Clean up zip file
                File.Delete(ZIP_PATH);

                // Show completion message
                progressBar.DisplayText = "Installation complete!";
                progressBar.Style = ProgressBarStyle.Continuous;
                Application.DoEvents();
                System.Threading.Thread.Sleep(1500); // Show message for 1.5 seconds

                // Check installation and show available clients
                installButton.Visible = false;
                CheckClientInstallation();
                statusLabel.Text = "Installation complete! Choose your client:";
                statusLabel.Visible = true;
                progressBar.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Extraction failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                installButton.Text = "⚙ INSTALL CLIENT ⚙";
                installButton.Enabled = true;
                progressBar.Visible = false;
                progressBar.Style = ProgressBarStyle.Continuous;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                httpClient?.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class LaunchArguments
        {
            private readonly Dictionary<string, string> _args = new Dictionary<string, string>();

            public void Append(string key, string value)
            {
                _args[key] = value;
            }

            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (KeyValuePair<string, string> arg in _args)
                {
                    stringBuilder.AppendFormat("-{0} \"{1}\"", arg.Key, arg.Value);
                    stringBuilder.Append(" ");
                }
                return stringBuilder.ToString();
            }
        }
    }

    public class TextProgressBar : ProgressBar
    {
        public string DisplayText { get; set; } = "";

        public TextProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                Rectangle rect = ClientRectangle;
                Graphics g = e.Graphics;

                // Use custom drawing to avoid theme issues
                using (Brush backBrush = new SolidBrush(SystemColors.Control))
                {
                    g.FillRectangle(backBrush, rect);
                }

                // Draw border
                using (Pen borderPen = new Pen(SystemColors.ControlDark))
                {
                    g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                }

                // Calculate filled portion
                if (Maximum > 0)
                {
                    int fillWidth = (int)(rect.Width * ((double)Value / Maximum));
                    if (fillWidth > 0)
                    {
                        Rectangle fillRect = new Rectangle(rect.X + 1, rect.Y + 1,
                                                          Math.Min(fillWidth - 2, rect.Width - 2),
                                                          rect.Height - 2);

                        // Use gradient for progress fill
                        using (Brush fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                            fillRect,
                            Color.FromArgb(0, 120, 215), // Windows 10 blue
                            Color.FromArgb(0, 100, 195),
                            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(fillBrush, fillRect);
                        }
                    }
                }

                // Draw text with better contrast
                if (!string.IsNullOrEmpty(DisplayText))
                {
                    // Draw text shadow for better readability
                    using (Brush shadowBrush = new SolidBrush(Color.White))
                    using (Brush textBrush = new SolidBrush(Color.Black))
                    {
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;

                            // Draw white shadow
                            RectangleF textRect = new RectangleF(rect.X + 1, rect.Y + 1, rect.Width, rect.Height);
                            g.DrawString(DisplayText, Font, shadowBrush, textRect, sf);

                            // Draw black text
                            textRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                            g.DrawString(DisplayText, Font, textBrush, textRect, sf);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If custom rendering fails, draw a simple progress bar
                Rectangle rect = ClientRectangle;
                Graphics g = e.Graphics;

                // Draw simple background
                g.FillRectangle(SystemBrushes.Control, rect);
                g.DrawRectangle(SystemPens.ControlDark, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

                // Draw simple progress fill
                if (Maximum > 0 && Value > 0)
                {
                    int fillWidth = (int)(rect.Width * ((double)Value / Maximum));
                    if (fillWidth > 0)
                    {
                        Rectangle fillRect = new Rectangle(rect.X + 1, rect.Y + 1, Math.Min(fillWidth - 2, rect.Width - 2), rect.Height - 2);
                        g.FillRectangle(SystemBrushes.Highlight, fillRect);
                    }
                }

                // Draw simple text
                if (!string.IsNullOrEmpty(DisplayText))
                {
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(DisplayText, Font, SystemBrushes.ControlText, rect, sf);
                    }
                }
            }
        }
    }
}