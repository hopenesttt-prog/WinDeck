using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinDeck
{
    /// <summary>패널 전체 설정: 단축키, 크기, 배치, 투명도, 자동 실행, 페이지 관리, 백업.</summary>
    public class SettingsForm : Form
    {
        static readonly int[] KeySizes = { 72, 88, 110, 132 };
        static readonly string[] KeySizeNames = { "작게", "보통", "크게", "아주 크게" };

        AppConfig working;
        KeyCaptureBox txtHotkey;
        Button btnRecord;
        ComboBox cboSize, cboLayout;
        TrackBar trkOpacity;
        Label lblOpacity;
        CheckBox chkTopMost, chkAutoStart;
        ListBox lstPages;

        public AppConfig Result { get; private set; }
        public bool AutoStartEnabled { get; private set; }

        public SettingsForm(AppConfig workingCopy, bool autoStart)
        {
            working = workingCopy;
            AutoStartEnabled = autoStart;
            BuildUi();
            LoadValues();
        }

        void BuildUi()
        {
            SuspendLayout();
            Text = "WinDeck 설정  (v" + AppInfo.Version + (ConfigStore.IsPortable ? ", 포터블" : "") + ")";
            Font = new Font(Theme.UiFontName, 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(456, 604);
            var tips = new ToolTip();

            AddHeader("패널", 16, 14);
            AddLabel("보이기/숨기기 단축키", 16, 45);
            txtHotkey = new KeyCaptureBox { Location = new Point(176, 42), Size = new Size(170, 23) };
            Controls.Add(txtHotkey);
            btnRecord = new Button { Text = "키 입력받기", Location = new Point(352, 41), Size = new Size(88, 26) };
            btnRecord.Click += delegate { if (txtHotkey.Capturing) txtHotkey.EndCapture(); else txtHotkey.BeginCapture(); };
            txtHotkey.CaptureChanged += delegate { btnRecord.Text = txtHotkey.Capturing ? "키를 누르세요" : "키 입력받기"; };
            Controls.Add(btnRecord);
            tips.SetToolTip(txtHotkey, "어디서든 이 키를 누르면 패널이 숨겨지거나 다시 나타납니다. 비워 두면 사용하지 않습니다.");

            AddLabel("버튼 크기", 16, 79);
            cboSize = new ComboBox { Location = new Point(176, 76), Size = new Size(170, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboSize.Items.AddRange(KeySizeNames);
            Controls.Add(cboSize);

            AddLabel("배치", 16, 113);
            cboLayout = new ComboBox { Location = new Point(176, 110), Size = new Size(170, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboLayout.Items.AddRange(new object[] { "가로 (4 × 2)", "세로 (2 × 4)" });
            Controls.Add(cboLayout);

            AddLabel("투명도", 16, 150);
            trkOpacity = new TrackBar
            {
                Location = new Point(170, 142),
                Size = new Size(210, 45),
                Minimum = 30,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10
            };
            lblOpacity = new Label { AutoSize = true, Location = new Point(386, 150) };
            trkOpacity.ValueChanged += delegate { lblOpacity.Text = trkOpacity.Value + "%"; };
            Controls.Add(trkOpacity);
            Controls.Add(lblOpacity);

            chkTopMost = new CheckBox { Text = "항상 다른 창 위에 표시", AutoSize = true, Location = new Point(16, 188) };
            chkAutoStart = new CheckBox { Text = "윈도우 시작 시 자동 실행", AutoSize = true, Location = new Point(16, 214) };
            Controls.Add(chkTopMost);
            Controls.Add(chkAutoStart);

            AddHeader("페이지", 16, 250);
            lstPages = new ListBox { IntegralHeight = false, Location = new Point(16, 276), Size = new Size(316, 168) };
            lstPages.DoubleClick += delegate { RenamePage(); };
            Controls.Add(lstPages);
            AddSideButton("추가…", 276, delegate { AddPage(); });
            AddSideButton("이름 변경…", 310, delegate { RenamePage(); });
            AddSideButton("삭제", 344, delegate { DeletePage(); });
            AddSideButton("위로 ▲", 378, delegate { MovePage(-1); });
            AddSideButton("아래로 ▼", 412, delegate { MovePage(1); });

            AddHeader("백업", 16, 462);
            var btnExport = new Button { Text = "설정 내보내기 (백업)…", Location = new Point(16, 488), Size = new Size(206, 30) };
            var btnImport = new Button { Text = "설정 불러오기…", Location = new Point(232, 488), Size = new Size(208, 30) };
            btnExport.Click += delegate { CollectValues(); BackupUi.Export(this, working); };
            btnImport.Click += delegate { ImportConfig(); };
            Controls.Add(btnExport);
            Controls.Add(btnImport);
            tips.SetToolTip(btnExport, "모든 페이지·버튼·아이콘을 .json 파일 하나로 저장합니다. 다른 PC에서 불러올 수 있습니다.");

            var link = new LinkLabel { Text = "설정 파일 폴더 열기", AutoSize = true, Location = new Point(16, 530) };
            link.LinkClicked += delegate
            {
                try { Process.Start("explorer.exe", "\"" + ConfigStore.Dir + "\""); }
                catch (Exception) { }
            };
            Controls.Add(link);

            var ok = new Button { Text = "확인", Location = new Point(248, 560), Size = new Size(94, 32) };
            var cancel = new Button { Text = "취소", Location = new Point(348, 560), Size = new Size(94, 32), DialogResult = DialogResult.Cancel };
            ok.Click += delegate { Accept(); };
            Controls.Add(ok);
            Controls.Add(cancel);
            CancelButton = cancel;

            // DPI 배율은 모든 컨트롤을 추가한 뒤에 지정해야 크기가 두 번 커지지 않는다.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ResumeLayout(false);
            PerformLayout();
        }

        void AddHeader(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y), Font = new Font(Theme.UiFontName, 9f, FontStyle.Bold) });
        }

        void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y) });
        }

        void AddSideButton(string text, int y, EventHandler onClick)
        {
            var b = new Button { Text = text, Location = new Point(340, y), Size = new Size(100, 28) };
            b.Click += onClick;
            Controls.Add(b);
        }

        void LoadValues()
        {
            txtHotkey.Text = working.Hotkey;
            int sizeIdx = 0;
            for (int i = 0; i < KeySizes.Length; i++)
                if (Math.Abs(KeySizes[i] - working.KeySize) < Math.Abs(KeySizes[sizeIdx] - working.KeySize)) sizeIdx = i;
            cboSize.SelectedIndex = sizeIdx;
            cboLayout.SelectedIndex = working.Vertical ? 1 : 0;
            trkOpacity.Value = Math.Max(trkOpacity.Minimum, Math.Min(trkOpacity.Maximum, working.Opacity));
            lblOpacity.Text = trkOpacity.Value + "%";
            chkTopMost.Checked = working.TopMost;
            chkAutoStart.Checked = AutoStartEnabled;
            RefreshPages(working.CurrentPage);
        }

        void RefreshPages(int select)
        {
            lstPages.BeginUpdate();
            lstPages.Items.Clear();
            for (int i = 0; i < working.Pages.Count; i++)
            {
                DeckPage p = working.Pages[i];
                int used = p.Buttons.Count(b => !b.IsEmpty);
                lstPages.Items.Add((i + 1) + ". " + p.Name + "   (" + used + "/" + AppConfig.KeyCount + ")");
            }
            lstPages.EndUpdate();
            if (lstPages.Items.Count > 0) lstPages.SelectedIndex = Math.Max(0, Math.Min(lstPages.Items.Count - 1, select));
        }

        void AddPage()
        {
            string name = InputDialog.Ask(this, "페이지 추가", "새 페이지 이름", "페이지 " + (working.Pages.Count + 1));
            if (name == null) return;
            working.Pages.Add(DeckPage.CreateEmpty(name));
            RefreshPages(working.Pages.Count - 1);
        }

        void RenamePage()
        {
            int i = lstPages.SelectedIndex;
            if (i < 0) return;
            string name = InputDialog.Ask(this, "페이지 이름 변경", "페이지 이름", working.Pages[i].Name);
            if (name == null) return;
            working.Pages[i].Name = name;
            RefreshPages(i);
        }

        void DeletePage()
        {
            int i = lstPages.SelectedIndex;
            if (i < 0) return;
            if (working.Pages.Count <= 1)
            {
                MessageBox.Show(this, "페이지는 최소 한 개가 있어야 합니다.", "WinDeck", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DeckPage p = working.Pages[i];
            int used = p.Buttons.Count(b => !b.IsEmpty);
            string msg = "'" + p.Name + "' 페이지를 삭제할까요?" + (used > 0 ? "\n이 페이지의 버튼 " + used + "개도 함께 삭제됩니다." : "");
            if (MessageBox.Show(this, msg, "페이지 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            working.Pages.RemoveAt(i);
            if (working.CurrentPage >= working.Pages.Count) working.CurrentPage = working.Pages.Count - 1;
            RefreshPages(Math.Min(i, working.Pages.Count - 1));
        }

        void MovePage(int delta)
        {
            int i = lstPages.SelectedIndex;
            int j = i + delta;
            if (i < 0 || j < 0 || j >= working.Pages.Count) return;
            DeckPage current = working.Pages[working.CurrentPage];
            DeckPage p = working.Pages[i];
            working.Pages.RemoveAt(i);
            working.Pages.Insert(j, p);
            working.CurrentPage = working.Pages.IndexOf(current);
            RefreshPages(j);
        }

        void ImportConfig()
        {
            AppConfig imported = BackupUi.Import(this, working);
            if (imported == null) return;
            working = imported;
            CollectAutoStart();
            Result = working;
            DialogResult = DialogResult.OK;
            Close();
        }

        void CollectAutoStart()
        {
            AutoStartEnabled = chkAutoStart.Checked;
        }

        void CollectValues()
        {
            working.Hotkey = txtHotkey.Text.Trim();
            working.KeySize = KeySizes[Math.Max(0, cboSize.SelectedIndex)];
            working.Vertical = cboLayout.SelectedIndex == 1;
            working.Opacity = trkOpacity.Value;
            working.TopMost = chkTopMost.Checked;
            CollectAutoStart();
        }

        void Accept()
        {
            string hotkey = txtHotkey.Text.Trim();
            if (hotkey.Length > 0)
            {
                KeyCombo combo;
                if (hotkey.Contains(",") || !KeyCombo.TryParse(hotkey, out combo))
                {
                    MessageBox.Show(this, "보이기/숨기기 단축키 형식이 올바르지 않습니다.\n예: Ctrl+Alt+Space", "WinDeck", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                txtHotkey.Text = combo.ToString();
                if (!combo.HasModifier
                    && MessageBox.Show(this, "Ctrl·Alt·Shift·Win 없이 '" + combo + "' 하나만 쓰면 평소 입력에 방해가 될 수 있습니다.\n그래도 사용할까요?",
                        "WinDeck", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            CollectValues();
            working.Normalize();
            Result = working;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
