using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderArchiveTool
{
    public class MainForm : Form
    {
        Panel headerPanel = null!;
        Label titleLabel = null!;
        Label subtitleLabel = null!;
        Button btnThemeToggle = null!;
        TextBox txtSource = null!, txtDestination = null!;
        Button btnBrowseSource = null!, btnBrowseDest = null!, btnScan = null!, btnExecute = null!;
        TreeView treeCandidates = null!;
        TextBox txtPreview = null!, txtLog = null!;
        ProgressBar progressBar = null!;
        CheckBox chkDryRun = null!, chkRemoveEmpty = null!, chkApplyChoiceToAll = null!;
        bool darkMode = false;

        // New UI for compression and filters
        CheckBox chkCompressFolders = null!;
        TextBox txtIncludeExt = null!, txtExcludeExt = null!, txtMinSizeMb = null!, txtMaxSizeMb = null!, txtExcludePaths = null!;

        DateTime thresholdDateUtc;
        string sourceRoot = string.Empty;

        // Move prompt state
        bool lastChoiceMove = false;
        bool applyChoiceToAll = false;

        // Overwrite prompt state
        bool lastOverwriteChoice = false;
        bool applyOverwriteToAll = false;

        FilterSettings currentFilters = new FilterSettings();

        public MainForm()
        {
            Text = "Folder Archive Tool";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(241, 245, 249);
            InitializeComponents();
            ApplyTheme(darkMode);
        }

        void InitializeComponents()
        {
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.FromArgb(15, 118, 110),
                Padding = new Padding(18, 0, 0, 0)
            };

            titleLabel = new Label
            {
                Text = "Folder Archive Tool",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point(22, 18)
            };

            subtitleLabel = new Label
            {
                Text = "Archive stale files and folders with smarter filters",
                AutoSize = true,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(24, 48)
            };

            btnThemeToggle = new Button
            {
                Text = "🌙 Dark",
                FlatStyle = FlatStyle.Flat,
                Width = 100,
                Height = 32,
                Location = new Point(850, 24),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                FlatAppearance = { BorderSize = 0 }
            };
            btnThemeToggle.Click += (s, e) =>
            {
                darkMode = !darkMode;
                ApplyTheme(darkMode);
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(btnThemeToggle);
            Controls.Add(headerPanel);

            Label lblSource = new Label() { Text = "Source:", Left = 14, Top = 100, Width = 58, Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point) };
            txtSource = new TextBox() { Left = 78, Top = 96, Width = 710, Height = 30, BorderStyle = BorderStyle.FixedSingle };
            btnBrowseSource = new Button() { Text = "Browse...", Left = 804, Top = 94, Width = 82, Height = 32, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
            btnBrowseSource.Click += (s, e) => { using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtSource.Text = dlg.SelectedPath; };

            Label lblDest = new Label() { Text = "Destination:", Left = 14, Top = 138, Width = 76, Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point) };
            txtDestination = new TextBox() { Left = 98, Top = 134, Width = 690, Height = 30, BorderStyle = BorderStyle.FixedSingle };
            btnBrowseDest = new Button() { Text = "Browse...", Left = 804, Top = 132, Width = 82, Height = 32, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
            btnBrowseDest.Click += (s, e) => { using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtDestination.Text = dlg.SelectedPath; };

            btnScan = new Button() { Text = "Scan for candidates", Left = 14, Top = 176, Width = 170, Height = 36, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
            btnScan.Click += async (s, e) => await ScanAsync();
            chkDryRun = new CheckBox() { Text = "Dry run", Left = 198, Top = 182, Width = 110, Checked = true, Font = new Font("Segoe UI", 10F, GraphicsUnit.Point) };
            chkRemoveEmpty = new CheckBox() { Text = "Remove empty folders", Left = 320, Top = 182, Width = 180, Checked = true, Font = new Font("Segoe UI", 10F, GraphicsUnit.Point) };

            chkCompressFolders = new CheckBox() { Text = "Compress .zip", Left = 520, Top = 182, Width = 150, Checked = false, Font = new Font("Segoe UI", 10F, GraphicsUnit.Point) };

            var lblInclude = new Label() { Text = "Include extensions:", Left = 12, Top = 220, Width = 150, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point) };
            txtIncludeExt = new TextBox() { Left = 170, Top = 216, Width = 270, Height = 30, BorderStyle = BorderStyle.FixedSingle };

            var lblExclude = new Label() { Text = "Exclude extensions:", Left = 456, Top = 220, Width = 150, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point) };
            txtExcludeExt = new TextBox() { Left = 610, Top = 216, Width = 270, Height = 30, BorderStyle = BorderStyle.FixedSingle };

            var lblMinSize = new Label() { Text = "Min size (MB):", Left = 12, Top = 258, Width = 120, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point) };
            txtMinSizeMb = new TextBox() { Left = 140, Top = 254, Width = 110, Height = 30, BorderStyle = BorderStyle.FixedSingle };

            var lblMaxSize = new Label() { Text = "Max size (MB):", Left = 270, Top = 258, Width = 120, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point) };
            txtMaxSizeMb = new TextBox() { Left = 396, Top = 254, Width = 110, Height = 30, BorderStyle = BorderStyle.FixedSingle };

            var lblExcludePaths = new Label() { Text = "Exclude paths:", Left = 530, Top = 258, Width = 120, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point) };
            txtExcludePaths = new TextBox() { Left = 650, Top = 254, Width = 230, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle };

            treeCandidates = new TreeView() { Left = 12, Top = 340, Width = 450, Height = 230, CheckBoxes = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5F, GraphicsUnit.Point) };
            treeCandidates.AfterSelect += (s, e) => UpdatePreviewForSelectedNode();

            txtPreview = new TextBox() { Left = 472, Top = 340, Width = 490, Height = 180, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5F, GraphicsUnit.Point) };

            progressBar = new ProgressBar() { Left = 472, Top = 530, Width = 490, Height = 20, Style = ProgressBarStyle.Blocks };

            btnExecute = new Button() { Text = "Execute Move/Archive", Left = 472, Top = 560, Width = 180, Height = 34, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
            btnExecute.Click += async (s, e) => await ExecuteMoveAsync();

            chkApplyChoiceToAll = new CheckBox() { Text = "Apply choice to all prompts", Left = 670, Top = 566, Width = 260, Font = new Font("Segoe UI", 9.5F, GraphicsUnit.Point) };

            txtLog = new TextBox() { Left = 12, Top = 596, Width = 950, Height = 64, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9F, GraphicsUnit.Point) };

            Controls.AddRange(new Control[] { lblSource, txtSource, btnBrowseSource, lblDest, txtDestination, btnBrowseDest, btnScan, chkDryRun, chkRemoveEmpty, chkCompressFolders, lblInclude, txtIncludeExt, lblExclude, txtExcludeExt, lblMinSize, txtMinSizeMb, lblMaxSize, txtMaxSizeMb, lblExcludePaths, txtExcludePaths, treeCandidates, txtPreview, progressBar, btnExecute, chkApplyChoiceToAll, txtLog });
        }

        void ApplyTheme(bool useDarkMode)
        {
            Color bg = useDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(245, 247, 250);
            Color panel = useDarkMode ? Color.FromArgb(30, 41, 59) : Color.FromArgb(255, 255, 255);
            Color field = useDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(255, 255, 255);
            Color text = useDarkMode ? Color.FromArgb(226, 232, 240) : Color.FromArgb(15, 23, 42);
            Color muted = useDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105);
            Color border = useDarkMode ? Color.FromArgb(71, 85, 105) : Color.FromArgb(209, 219, 230);
            Color accent = useDarkMode ? Color.FromArgb(96, 165, 250) : Color.FromArgb(14, 116, 144);
            Color secondaryAccent = useDarkMode ? Color.FromArgb(59, 130, 246) : Color.FromArgb(8, 145, 178);
            Color success = useDarkMode ? Color.FromArgb(16, 185, 129) : Color.FromArgb(16, 185, 129);
            Color softPanel = useDarkMode ? Color.FromArgb(20, 31, 49) : Color.FromArgb(241, 245, 249);

            BackColor = bg;
            headerPanel.BackColor = useDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(15, 118, 110);
            titleLabel.ForeColor = Color.White;
            subtitleLabel.ForeColor = useDarkMode ? muted : Color.FromArgb(220, 252, 231);
            btnThemeToggle.Text = useDarkMode ? "☀️ Light" : "🌙 Dark";
            btnThemeToggle.BackColor = useDarkMode ? panel : Color.FromArgb(13, 148, 136);
            btnThemeToggle.ForeColor = Color.White;
            btnThemeToggle.FlatAppearance.BorderColor = border;

            foreach (Control control in Controls)
            {
                if (control is TextBox txt)
                {
                    txt.BackColor = field;
                    txt.ForeColor = text;
                    txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is Label label)
                {
                    label.ForeColor = text;
                }
                else if (control is CheckBox check)
                {
                    check.ForeColor = text;
                    check.BackColor = bg;
                }
                else if (control is TreeView tree)
                {
                    tree.BackColor = softPanel;
                    tree.ForeColor = text;
                    tree.LineColor = border;
                }
                else if (control is Button button)
                {
                    if (button == btnScan || button == btnBrowseSource)
                    {
                        button.BackColor = accent;
                        button.ForeColor = Color.White;
                    }
                    else if (button == btnBrowseDest)
                    {
                        button.BackColor = secondaryAccent;
                        button.ForeColor = Color.White;
                    }
                    else if (button == btnExecute)
                    {
                        button.BackColor = success;
                        button.ForeColor = Color.White;
                    }
                    else if (button != btnThemeToggle)
                    {
                        button.BackColor = useDarkMode ? panel : Color.FromArgb(237, 242, 247);
                        button.ForeColor = text;
                    }

                    button.FlatAppearance.BorderColor = border;
                    button.FlatAppearance.MouseOverBackColor = useDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(224, 233, 244);
                    button.FlatAppearance.BorderSize = 0;
                }
                else if (control is ProgressBar pb)
                {
                    pb.BackColor = softPanel;
                    pb.ForeColor = accent;
                }
            }

            txtSource.BackColor = field;
            txtDestination.BackColor = field;
            txtIncludeExt.BackColor = field;
            txtExcludeExt.BackColor = field;
            txtMinSizeMb.BackColor = field;
            txtMaxSizeMb.BackColor = field;
            txtExcludePaths.BackColor = field;
            txtPreview.BackColor = panel;
            txtLog.BackColor = panel;

            txtSource.ForeColor = text;
            txtDestination.ForeColor = text;
            txtIncludeExt.ForeColor = text;
            txtExcludeExt.ForeColor = text;
            txtMinSizeMb.ForeColor = text;
            txtMaxSizeMb.ForeColor = text;
            txtExcludePaths.ForeColor = text;
            txtPreview.ForeColor = text;
            txtLog.ForeColor = text;

            treeCandidates.BackColor = softPanel;
            treeCandidates.ForeColor = text;
            treeCandidates.LineColor = border;

            btnBrowseSource.BackColor = accent;
            btnBrowseDest.BackColor = secondaryAccent;
            btnScan.BackColor = accent;
            btnExecute.BackColor = success;
            btnBrowseSource.ForeColor = Color.White;
            btnBrowseDest.ForeColor = Color.White;
            btnScan.ForeColor = Color.White;
            btnExecute.ForeColor = Color.White;

            chkDryRun.ForeColor = text;
            chkRemoveEmpty.ForeColor = text;
            chkCompressFolders.ForeColor = text;
            chkApplyChoiceToAll.ForeColor = text;
        }

        async Task ScanAsync()
        {
            txtLog.Clear();
            treeCandidates.Nodes.Clear();
            sourceRoot = txtSource.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sourceRoot) || !Directory.Exists(sourceRoot))
            {
                MessageBox.Show("Please specify a valid source folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            thresholdDateUtc = DateTime.UtcNow.AddDays(-365); // 1 year

            // Parse filters from UI
            currentFilters = FilterSettings.ParseFromInputs(txtIncludeExt.Text, txtExcludeExt.Text, txtMinSizeMb.Text, txtMaxSizeMb.Text, txtExcludePaths.Text);

            AppendLog($"Scanning {sourceRoot} for items where both last write AND last access are older than {thresholdDateUtc:u} (UTC)");
            btnScan.Enabled = false;
            try
            {
                var rootNode = new TreeNode(Path.GetFileName(sourceRoot)) { Tag = sourceRoot };
                progressBar.Style = ProgressBarStyle.Marquee;

                await Task.Run(() =>
                {
                    bool rootQualifies = ArchiveScanner.BuildNodeRecursive(sourceRoot, rootNode, thresholdDateUtc, currentFilters);
                });

                treeCandidates.Nodes.Add(rootNode);
                rootNode.Expand();
                AppendLog("Scan complete.");
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                btnScan.Enabled = true;
            }
        }

        bool BuildNodeRecursive(string path, TreeNode node)
        {
            // Delegate to ArchiveScanner but keep same signature for compatibility
            try
            {
                return ArchiveScanner.BuildNodeRecursive(path, node, thresholdDateUtc, currentFilters);
            }
            catch (Exception ex)
            {
                AppendLog($"Error scanning {path}: {ex.Message}");
                return false;
            }
        }

        void UpdatePreviewForSelectedNode()
        {
            if (treeCandidates.SelectedNode == null) return;
            var path = treeCandidates.SelectedNode.Tag as string;
            if (path == null) return;
            try
            {
                if (File.Exists(path))
                {
                    var fi = new FileInfo(path);
                    txtPreview.Text = $"File: {fi.FullName}{Environment.NewLine}Size: {fi.Length} bytes{Environment.NewLine}Last Write (UTC): {fi.LastWriteTimeUtc:u}{Environment.NewLine}Last Access (UTC): {fi.LastAccessTimeUtc:u}";
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray();
                    long totalSize = files.Select(f => new FileInfo(f).Length).Sum();
                    txtPreview.Text = $"Folder: {dirInfo.FullName}{Environment.NewLine}Files (rec): {files.Length}{Environment.NewLine}Total size: {totalSize} bytes";
                }
                else
                {
                    txtPreview.Text = "Item not found.";
                }
            }
            catch (Exception ex)
            {
                txtPreview.Text = "Error reading item: " + ex.Message;
            }
        }

        async Task ExecuteMoveAsync()
        {
            if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
            {
                MessageBox.Show("Please scan a valid source first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var destRoot = txtDestination.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(destRoot) || !Directory.Exists(destRoot))
            {
                MessageBox.Show("Please specify a valid destination folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            chkApplyChoiceToAll.Enabled = false;
            applyChoiceToAll = false;
            lastChoiceMove = false;
            applyOverwriteToAll = false;
            lastOverwriteChoice = false;

            var nodesToProcess = new List<string>(); // paths
            CollectCheckedPaths(treeCandidates.Nodes, nodesToProcess);

            if (nodesToProcess.Count == 0)
            {
                MessageBox.Show("No checked candidates to move.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                chkApplyChoiceToAll.Enabled = true;
                return;
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = nodesToProcess.Count;
            progressBar.Value = 0;

            bool dryRun = chkDryRun.Checked;
            bool removeEmpty = chkRemoveEmpty.Checked;
            bool compress = chkCompressFolders.Checked;

            AppendLog($"Preparing to process {nodesToProcess.Count} items. Dry-run: {dryRun}");

            await Task.Run(() =>
            {
                int processed = 0;
                foreach (var path in nodesToProcess)
                {
                    processed++;
                    Invoke(() => progressBar.Value = processed);

                    try
                    {
                        string relative = Path.GetRelativePath(sourceRoot, path);
                        string destPath = Path.Combine(destRoot, relative);

                        if (File.Exists(path))
                        {
                            // Ensure destination folder exists
                            var destDir = Path.GetDirectoryName(destPath);
                            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                            bool doMove = PromptForAction(path);
                            if (!doMove)
                            {
                                AppendLog($"Skipped: {path}");
                                continue;
                            }

                            if (dryRun)
                            {
                                AppendLog($"[DRY] Move file: {path} -> {destPath}");
                            }
                            else
                            {
                                if (File.Exists(destPath))
                                {
                                    var overwrite = PromptOverwrite(destPath);
                                    if (!overwrite)
                                    {
                                        AppendLog($"Skipped existing destination: {destPath}");
                                        continue;
                                    }
                                }
                                File.Move(path, destPath);
                                AppendLog($"Moved: {path} -> {destPath}");
                            }
                        }
                        else if (Directory.Exists(path))
                        {
                            // Ensure destination parent exists
                            string destFull = Path.Combine(destRoot, relative);
                            var destParent = Path.GetDirectoryName(destFull);
                            if (!Directory.Exists(destParent)) Directory.CreateDirectory(destParent);

                            bool doMove = PromptForAction(path);
                            if (!doMove)
                            {
                                AppendLog($"Skipped: {path}");
                                continue;
                            }

                            if (compress)
                            {
                                // Create zip at destination: destFull + ".zip"
                                string destZip = destFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".zip";
                                if (dryRun)
                                {
                                    AppendLog($"[DRY] Compress folder: {path} -> {destZip}");
                                }
                                else
                                {
                                    if (File.Exists(destZip))
                                    {
                                        var overwrite = PromptOverwrite(destZip);
                                        if (!overwrite)
                                        {
                                            AppendLog($"Skipped existing destination archive: {destZip}");
                                            continue;
                                        }
                                        File.Delete(destZip);
                                    }
                                    ZipFile.CreateFromDirectory(path, destZip, CompressionLevel.Optimal, includeBaseDirectory: true);
                                    // After successful zip, remove original folder
                                    Directory.Delete(path, recursive: true);
                                    AppendLog($"Compressed folder: {path} -> {destZip}");
                                }
                            }
                            else
                            {
                                if (dryRun)
                                {
                                    AppendLog($"[DRY] Move folder: {path} -> {destFull}");
                                }
                                else
                                {
                                    if (Directory.Exists(destFull))
                                    {
                                        var overwrite = PromptOverwrite(destFull);
                                        if (!overwrite)
                                        {
                                            AppendLog($"Skipped existing destination folder: {destFull}");
                                            continue;
                                        }
                                        Directory.Delete(destFull, recursive: true);
                                    }
                                    Directory.Move(path, destFull);
                                    AppendLog($"Moved folder: {path} -> {destFull}");
                                }
                            }
                        }

                        if (removeEmpty && !dryRun)
                        {
                            TryRemoveEmptyParentFolders(Path.GetDirectoryName(path));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error processing {path}: {ex.Message}");
                    }
                }
            });

            AppendLog("Operation complete.");
            chkApplyChoiceToAll.Enabled = true;
        }

        void CollectCheckedPaths(TreeNodeCollection nodes, List<string> outPaths)
        {
            foreach (TreeNode node in nodes)
            {
                var path = node.Tag as string;
                if (node.Checked && path != null)
                {
                    outPaths.Add(path);
                }
                if (node.Nodes.Count > 0)
                {
                    CollectCheckedPaths(node.Nodes, outPaths);
                }
            }
        }

        bool PromptForAction(string path)
        {
            if (applyChoiceToAll)
            {
                return lastChoiceMove;
            }

            DialogResult res = DialogResult.No;
            // Show on UI thread
            Invoke(new Action(() =>
            {
                res = MessageBox.Show($"Move '{path}'?\n(Yes = move, No = skip)", "Confirm move", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (chkApplyChoiceToAll.Checked)
                {
                    applyChoiceToAll = true;
                    lastChoiceMove = (res == DialogResult.Yes);
                }
            }));

            return res == DialogResult.Yes;
        }

        bool PromptOverwrite(string destPath)
        {
            // Use ConflictPromptForm to get choices: Yes/YesToAll/No/NoToAll
            if (applyOverwriteToAll)
            {
                return lastOverwriteChoice;
            }

            ConflictDecision decision = ConflictDecision.No;
            // Show dialog on UI thread
            Invoke(new Action(() =>
            {
                decision = ConflictPromptForm.ShowDialogChoice(this, $"Destination exists: '{destPath}'\nChoose action:");
            }));

            switch (decision)
            {
                case ConflictDecision.Yes:
                    return true;
                case ConflictDecision.YesToAll:
                    applyOverwriteToAll = true;
                    lastOverwriteChoice = true;
                    return true;
                case ConflictDecision.No:
                    return false;
                case ConflictDecision.NoToAll:
                    applyOverwriteToAll = true;
                    lastOverwriteChoice = false;
                    return false;
                default:
                    return false;
            }
        }

        void TryRemoveEmptyParentFolders(string? startDir)
        {
            try
            {
                var dir = startDir;
                while (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && dir.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                        AppendLog($"Removed empty folder: {dir}");
                        dir = Path.GetDirectoryName(dir);
                    }
                    else break;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error removing empty folders: {ex.Message}");
            }
        }

        void AppendLog(string message)
        {
            Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:u}] {message}{Environment.NewLine}");
            });
        }
    }
}
