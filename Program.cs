using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace GambonanzaSaveManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class BackupItem
    {
        public static bool English = false;
        public string Path;
        public DateTime Time;
        public string State="?", Wave="?", Coins="?";
        public int White, Black;
        public override string ToString()
        {
            return string.Format("{0:MM-dd HH:mm:ss} | {1,-15} | 波{2,-3} | 金币{3,-5} | 白{4} 黑{5} | {6}", Time, State, Wave, Coins, White, Black, System.IO.Path.GetFileName(Path));
        }
    }

    class Snapshot
    {
        public string[] Pieces = Enumerable.Repeat("", 40).ToArray();
        public string State="?", Wave="?", Coins="?", Difficulty="?", Run="?";
        public List<string> Gambits = new List<string>();
        public List<string> Stock = new List<string>();
        public int Rows = 5, Cols = 8;
        public int White { get { return Pieces.Count(p => Regex.IsMatch(p ?? "", "^[A-Z]_W[^_]*(_|$)")); } }
        public int Black { get { return Pieces.Count(p => Regex.IsMatch(p ?? "", "^[A-Z]_B[^_]*(_|$)")); } }
    }

    class MainForm : Form
    {
        TextBox savePath = new TextBox(), backupDir = new TextBox(), gambitsBox = new TextBox(), stockBox = new TextBox();
        ListBox backups = new ListBox();
        TableLayoutPanel board = new TableLayoutPanel();
        Panel boardHost = new Panel();
        Label summary = new Label(), status = new Label();
        CheckBox nonCombat = new CheckBox();
        Button autoBtn = new Button();
        Timer timer = new Timer();
        NumericUpDown coins = new NumericUpDown();
        ToolTip tip = new ToolTip();
        ComboBox langBox = new ComboBox();
        string lastStamp = null;
        bool autoOn = false;
        int currentRows = 5, currentCols = 8;
        int currentCell = 40;

        public MainForm()
        {
            Text = "Gambonanza 存档管理器 / SL / 棋盘查看 / 修改器";
            Width = 1280; Height = 820; MinimumSize = new Size(1050, 680);
            Font = new Font("Microsoft YaHei UI", 9F);
            BuildUI(); Hook(); ApplyLanguage(); LoadDefaults(); RefreshList(); RenderCurrent();
        }

        void BuildUI()
        {
            var root = new TableLayoutPanel{Dock=DockStyle.Fill, RowCount=5, ColumnCount=1, Padding=new Padding(10)};
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(root);

            var paths = new TableLayoutPanel{Dock=DockStyle.Fill, RowCount=2, ColumnCount=4};
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));
            paths.Controls.Add(new Label{Text="存档", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft},0,0);
            savePath.Dock=DockStyle.Fill; paths.Controls.Add(savePath,1,0);
            var pickSave = new Button{Text="选择save.json", Dock=DockStyle.Fill}; paths.Controls.Add(pickSave,2,0);
            var defSave = new Button{Text="默认路径", Dock=DockStyle.Fill}; paths.Controls.Add(defSave,3,0);
            paths.Controls.Add(new Label{Text="备份", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft},0,1);
            backupDir.Dock=DockStyle.Fill; paths.Controls.Add(backupDir,1,1);
            var pickBak = new Button{Text="选择目录", Dock=DockStyle.Fill}; paths.Controls.Add(pickBak,2,1);
            var openBak = new Button{Text="打开目录", Dock=DockStyle.Fill}; paths.Controls.Add(openBak,3,1);
            root.Controls.Add(paths,0,0);

            var bar = new FlowLayoutPanel{Dock=DockStyle.Fill, WrapContents=false};
            autoBtn.Text="开启自动备份"; autoBtn.Width=115;
            var manual = new Button{Text="手动备份", Width=90};
            var restore = new Button{Text="还原选中", Width=90};
            var refresh = new Button{Text="刷新", Width=70};
            langBox.DropDownStyle=ComboBoxStyle.DropDownList; langBox.Width=120; langBox.Items.AddRange(new object[]{"\u4e2d\u6587","English"}); langBox.SelectedIndex=0;
            nonCombat.Text="只看非战斗"; nonCombat.AutoSize=true; nonCombat.Padding=new Padding(8,7,0,0);
            coins.Maximum=999999; coins.Value=99; coins.Width=85;
            var setCoins = new Button{Text="改当前金币", Width=100};
            var deleteSelected = new Button{Text="删除选中备份", Width=110};
            var deleteAll = new Button{Text="删除全部备份", Width=110};
            bar.Controls.AddRange(new Control[]{autoBtn,manual,restore,refresh,new Label{Text="\u8bed\u8a00",AutoSize=true,Padding=new Padding(12,7,0,0)},langBox,nonCombat,new Label{Text="\u91d1\u5e01",AutoSize=true,Padding=new Padding(12,7,0,0)},coins,setCoins,deleteSelected,deleteAll});
            root.Controls.Add(bar,0,1);

            var split = new SplitContainer{Dock=DockStyle.Fill, SplitterDistance=660};
            backups.Dock=DockStyle.Fill; backups.Font = new Font("Consolas", 9F); split.Panel1.Controls.Add(backups);
            var right = new TableLayoutPanel{Dock=DockStyle.Fill, RowCount=2, ColumnCount=1, Padding=new Padding(10,0,0,0)};
            right.RowStyles.Add(new RowStyle(SizeType.Absolute,60)); right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            summary.Dock=DockStyle.Fill; summary.TextAlign=ContentAlignment.MiddleLeft; right.Controls.Add(summary,0,0);
            board.CellBorderStyle=TableLayoutPanelCellBorderStyle.Single; board.Dock=DockStyle.None;
            boardHost.Dock=DockStyle.Fill; boardHost.BackColor=Color.FromArgb(245,245,245); boardHost.Controls.Add(board); right.Controls.Add(boardHost,0,1); split.Panel2.Controls.Add(right); root.Controls.Add(split,0,2);

            var editors = new TableLayoutPanel{Dock=DockStyle.Fill, RowCount=2, ColumnCount=4};
            editors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90)); editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); editors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130)); editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            editors.Controls.Add(new Label{Text="当前奇兵/Gambit\r\n每行一个",Dock=DockStyle.Fill},0,0);
            gambitsBox.Multiline=true; gambitsBox.ScrollBars=ScrollBars.Vertical; gambitsBox.Dock=DockStyle.Fill; editors.Controls.Add(gambitsBox,1,0);
            var applyGambits = new Button{Text="应用奇兵到当前存档", Dock=DockStyle.Fill}; editors.Controls.Add(applyGambits,2,0);
            editors.Controls.Add(new Label{Text="库存棋子\r\n每行一个",Dock=DockStyle.Fill},0,1);
            stockBox.Multiline=true; stockBox.ScrollBars=ScrollBars.Vertical; stockBox.Dock=DockStyle.Fill; editors.Controls.Add(stockBox,1,1);
            var applyStock = new Button{Text="应用库存到当前存档", Dock=DockStyle.Fill}; editors.Controls.Add(applyStock,2,1);
            var help = new Label{Dock=DockStyle.Fill, Text="奇兵字段来自 <CurrentGambits>，例如 thunder_name / superhero-cape_name。\r\n库存字段来自 <PiecesInStock>，例如 P_W_0 / R_W_0。修改前自动 .bak 备份。", TextAlign=ContentAlignment.MiddleLeft};
            editors.Controls.Add(help,3,0); editors.SetRowSpan(help,2);
            root.Controls.Add(editors,0,3);

            status.Dock=DockStyle.Fill; status.BorderStyle=BorderStyle.FixedSingle; status.TextAlign=ContentAlignment.MiddleLeft; root.Controls.Add(status,0,4);

            pickSave.Click += delegate { BrowseSave(); };
            defSave.Click += delegate { savePath.Text = DefaultSave(); RenderCurrent(); };
            pickBak.Click += delegate { BrowseDir(); };
            openBak.Click += delegate { Directory.CreateDirectory(BakDir); Process.Start(new ProcessStartInfo{FileName=BakDir,UseShellExecute=true}); };
            manual.Click += delegate { ManualBackup(); };
            restore.Click += delegate { RestoreSelected(); };
            refresh.Click += delegate { RefreshList(); };
            setCoins.Click += delegate { SetCoins(); };
            deleteSelected.Click += delegate { DeleteSelectedBackup(); };
            deleteAll.Click += delegate { DeleteAllBackups(); };
            applyGambits.Click += delegate { ApplyStringList("CurrentGambits", Lines(gambitsBox.Text)); };
            applyStock.Click += delegate { ApplyStringList("PiecesInStock", Lines(stockBox.Text)); };
        }

        void Hook(){ langBox.SelectedIndexChanged += delegate { ApplyLanguage(); RefreshList(); RenderCurrent(); }; autoBtn.Click += delegate { ToggleAuto(); }; backups.SelectedIndexChanged += delegate { var b=backups.SelectedItem as BackupItem; if(b!=null) RenderFile(b.Path,"备份: "+Path.GetFileName(b.Path)); }; nonCombat.CheckedChanged += delegate { RefreshList(); }; timer.Interval=2000; timer.Tick += delegate { AutoTick(); }; boardHost.Resize += delegate { ResizeBoardSquare(); }; savePath.Leave += delegate { RenderCurrent(); }; backupDir.Leave += delegate { RefreshList(); }; }
        string Save { get { return savePath.Text.Trim(); } } string BakDir { get { return backupDir.Text.Trim(); } }
        static string DefaultSave(){ return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Blukulélé", "Gambonanza", "save.json"); }
        bool EN(){ return langBox!=null && langBox.SelectedIndex==1; }
        string T(string zh,string en){ return EN()?en:zh; }
        void ApplyLanguage(){
            BackupItem.English = EN();
            Text = T("Gambonanza \u5b58\u6863\u7ba1\u7406\u5668 / SL / \u68cb\u76d8\u67e5\u770b / \u4fee\u6539\u5668", "Gambonanza Save Manager / SL / Board Viewer / Editor");
            ApplyLanguageToControls(this);
            autoBtn.Text = autoOn ? T("\u505c\u6b62\u81ea\u52a8\u5907\u4efd","Stop Auto Backup") : T("\u5f00\u542f\u81ea\u52a8\u5907\u4efd","Start Auto Backup");
        }
        void ApplyLanguageToControls(Control root){
            foreach(Control c in root.Controls){
                if(c is Label || c is Button || c is CheckBox){
                    string s=c.Text;
                    if(s=="\u5b58\u6863" || s=="Save") c.Text=T("\u5b58\u6863","Save");
                    else if(s=="\u9009\u62e9save.json" || s=="Choose save.json") c.Text=T("\u9009\u62e9save.json","Choose save.json");
                    else if(s=="\u9ed8\u8ba4\u8def\u5f84" || s=="Default Path") c.Text=T("\u9ed8\u8ba4\u8def\u5f84","Default Path");
                    else if(s=="\u5907\u4efd" || s=="Backups") c.Text=T("\u5907\u4efd","Backups");
                    else if(s=="\u9009\u62e9\u76ee\u5f55" || s=="Choose Folder") c.Text=T("\u9009\u62e9\u76ee\u5f55","Choose Folder");
                    else if(s=="\u6253\u5f00\u76ee\u5f55" || s=="Open Folder") c.Text=T("\u6253\u5f00\u76ee\u5f55","Open Folder");
                    else if(s=="\u5f00\u542f\u81ea\u52a8\u5907\u4efd" || s=="Start Auto Backup") c.Text=T("\u5f00\u542f\u81ea\u52a8\u5907\u4efd","Start Auto Backup");
                    else if(s=="\u505c\u6b62\u81ea\u52a8\u5907\u4efd" || s=="Stop Auto Backup") c.Text=T("\u505c\u6b62\u81ea\u52a8\u5907\u4efd","Stop Auto Backup");
                    else if(s=="\u624b\u52a8\u5907\u4efd" || s=="Manual Backup") c.Text=T("\u624b\u52a8\u5907\u4efd","Manual Backup");
                    else if(s=="\u8fd8\u539f\u9009\u4e2d" || s=="Restore Selected") c.Text=T("\u8fd8\u539f\u9009\u4e2d","Restore Selected");
                    else if(s=="\u5237\u65b0" || s=="Refresh") c.Text=T("\u5237\u65b0","Refresh");
                    else if(s=="\u8bed\u8a00" || s=="Language") c.Text=T("\u8bed\u8a00","Language");
                    else if(s=="\u53ea\u770b\u975e\u6218\u6597" || s=="Non-combat only") c.Text=T("\u53ea\u770b\u975e\u6218\u6597","Non-combat only");
                    else if(s=="\u91d1\u5e01" || s=="Coins") c.Text=T("\u91d1\u5e01","Coins");
                    else if(s=="\u6539\u5f53\u524d\u91d1\u5e01" || s=="Set Coins") c.Text=T("\u6539\u5f53\u524d\u91d1\u5e01","Set Coins");
                    else if(s=="\u5220\u9664\u9009\u4e2d\u5907\u4efd" || s=="Delete Selected") c.Text=T("\u5220\u9664\u9009\u4e2d\u5907\u4efd","Delete Selected");
                    else if(s=="\u5220\u9664\u5168\u90e8\u5907\u4efd" || s=="Delete All") c.Text=T("\u5220\u9664\u5168\u90e8\u5907\u4efd","Delete All");
                    else if(s.StartsWith("\u5f53\u524d\u5947\u5175") || s.StartsWith("Current Gambits")) c.Text=T("\u5f53\u524d\u5947\u5175/Gambit\\r\\n\u6bcf\u884c\u4e00\u4e2a","Current Gambits\r\nOne per line");
                    else if(s=="\u5e94\u7528\u5947\u5175\u5230\u5f53\u524d\u5b58\u6863" || s=="Apply Gambits") c.Text=T("\u5e94\u7528\u5947\u5175\u5230\u5f53\u524d\u5b58\u6863","Apply Gambits");
                    else if(s.StartsWith("\u5e93\u5b58\u68cb\u5b50") || s.StartsWith("Pieces in Stock")) c.Text=T("\u5e93\u5b58\u68cb\u5b50\\r\\n\u6bcf\u884c\u4e00\u4e2a","Pieces in Stock\r\nOne per line");
                    else if(s=="\u5e94\u7528\u5e93\u5b58\u5230\u5f53\u524d\u5b58\u6863" || s=="Apply Stock") c.Text=T("\u5e94\u7528\u5e93\u5b58\u5230\u5f53\u524d\u5b58\u6863","Apply Stock");
                    else if(s.StartsWith("??????") || s.StartsWith("Gambits are from")) c.Text=T("\u5947\u5175\u5b57\u6bb5\u6765\u81ea <CurrentGambits>\uff0c\u4f8b\u5982 thunder_name / superhero-cape_name\u3002\\r\\n\u5e93\u5b58\u5b57\u6bb5\u6765\u81ea <PiecesInStock>\uff0c\u4f8b\u5982 P_W_0 / R_W_0\u3002\u4fee\u6539\u524d\u81ea\u52a8 .bak \u5907\u4efd\u3002","Gambits are from <CurrentGambits>, e.g. thunder_name / superhero-cape_name.\r\nStock pieces are from <PiecesInStock>, e.g. P_W_0 / R_W_0. A .bak is created before edits.");
                }
                if(c.HasChildren) ApplyLanguageToControls(c);
            }
        }
        void LoadDefaults(){ savePath.Text = File.Exists(DefaultSave()) ? DefaultSave() : ""; var old=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Gambonanza_SL","backups"); backupDir.Text=Directory.Exists(old)?old:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"GambonanzaSaveManager","backups"); Directory.CreateDirectory(BakDir); }
        void BrowseSave(){ using(var d=new OpenFileDialog{Filter="save.json|save.json|XML/JSON|*.xml;*.json|All|*.*"}){ if(File.Exists(Save)) d.InitialDirectory=Path.GetDirectoryName(Save); if(d.ShowDialog(this)==DialogResult.OK){ savePath.Text=d.FileName; RenderCurrent(); } } }
        void BrowseDir(){ using(var d=new FolderBrowserDialog()){ if(Directory.Exists(BakDir)) d.SelectedPath=BakDir; if(d.ShowDialog(this)==DialogResult.OK){ backupDir.Text=d.SelectedPath; Directory.CreateDirectory(BakDir); RefreshList(); } } }
        void ToggleAuto(){ if(!autoOn){ if(!File.Exists(Save)){ Msg("请先选择有效 save.json"); return; } Directory.CreateDirectory(BakDir); lastStamp=null; autoOn=true; timer.Start(); autoBtn.Text="停止自动备份"; AutoTick(); Stat("自动备份已开启"); } else { autoOn=false; timer.Stop(); autoBtn.Text="开启自动备份"; Stat("自动备份已停止"); } }
        void AutoTick(){ try{ if(!File.Exists(Save))return; var f=new FileInfo(Save); var s=f.LastWriteTimeUtc.Ticks+":"+f.Length; if(s==lastStamp)return; lastStamp=s; Backup("save"); Trim(300); }catch(Exception ex){ Stat("自动备份失败: "+ex.Message); } }
        void ManualBackup(){ try{ Backup("manual"); RefreshList(); }catch(Exception ex){ Msg("备份失败: "+ex.Message); } }
        string Backup(string prefix){ Directory.CreateDirectory(BakDir); var dest=Path.Combine(BakDir,prefix+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".xml"); File.Copy(Save,dest,true); Stat("已备份: "+Path.GetFileName(dest)); RefreshList(false); return dest; }
        void Trim(int keep){ foreach(var f in Directory.GetFiles(BakDir,"*.xml").Select(p=>new FileInfo(p)).OrderByDescending(f=>f.LastWriteTime).Skip(keep)){ try{f.Delete();}catch{} } }
        void RefreshList(bool selectFirst=true){ var selected=(backups.SelectedItem as BackupItem)?.Path; var list=new List<BackupItem>(); if(Directory.Exists(BakDir)) foreach(var f in Directory.GetFiles(BakDir,"*.xml").Select(p=>new FileInfo(p)).OrderByDescending(f=>f.LastWriteTime)){ var bi=Info(f.FullName); if(nonCombat.Checked && bi.State=="INGAME") continue; list.Add(bi); } backups.DataSource=null; backups.DataSource=list; if(selected!=null){ for(int i=0;i<list.Count;i++) if(list[i].Path==selected){ backups.SelectedIndex=i; break; } } if(selectFirst && backups.SelectedIndex<0 && list.Count>0) backups.SelectedIndex=0; Stat("备份数量: "+list.Count); }
        BackupItem Info(string p){ var fi=new FileInfo(p); var bi=new BackupItem{Path=p,Time=fi.LastWriteTime}; try{ var s=Read(p); bi.State=s.State; bi.Wave=s.Wave; bi.Coins=s.Coins; bi.White=s.White; bi.Black=s.Black; }catch{ bi.State="读取失败";} return bi; }
        Snapshot Read(string p){ var doc=LoadDoc(p); var data=doc.SelectSingleNode("/Data"); if(data==null) throw new Exception("不是Data XML"); var s=new Snapshot(); s.State=Txt(data,"CurrentRunState"); s.Wave=Txt(data,"CurrentWave"); s.Coins=Txt(data,"Coins"); s.Difficulty=Txt(data,"CurrentDifficulty"); s.Run=Txt(data,"RunInProgress"); var ns=doc.SelectNodes("/Data/PiecesOnBoard/string"); if(ns!=null){ int count=ns.Count; var dims=InferDims(count); s.Rows=dims.Item1; s.Cols=dims.Item2; s.Pieces=Enumerable.Repeat("", Math.Max(1,s.Rows*s.Cols)).ToArray(); for(int i=0;i<Math.Min(ns.Count,s.Pieces.Length);i++){ s.Pieces[i]=ns[i].InnerText; } } var gs=doc.SelectNodes("/Data/CurrentGambits/string"); if(gs!=null) foreach(XmlNode n in gs) if(!string.IsNullOrWhiteSpace(n.InnerText)) s.Gambits.Add(n.InnerText); var st=doc.SelectNodes("/Data/PiecesInStock/string"); if(st!=null) foreach(XmlNode n in st) if(!string.IsNullOrWhiteSpace(n.InnerText)) s.Stock.Add(n.InnerText); return s; }
        static string Txt(XmlNode data,string name){ var n=data.SelectSingleNode(name); return n==null?"?":n.InnerText; }
        Tuple<int,int> InferDims(int count){ if(count<=0) return Tuple.Create(5,8); if(count==40) return Tuple.Create(5,8); if(count%8==0) return Tuple.Create(count/8,8); if(count%7==0) return Tuple.Create(count/7,7); if(count%6==0) return Tuple.Create(count/6,6); if(count%5==0) return Tuple.Create(count/5,5); int cols=(int)Math.Ceiling(Math.Sqrt(count)); int rows=(int)Math.Ceiling((double)count/cols); return Tuple.Create(rows,cols); }
        void RenderCurrent(){ if(File.Exists(Save)) RenderFile(Save,"当前存档"); else Clear("未选择有效 save.json"); }
        void BuildBoard(int rows,int cols){
            currentRows=rows; currentCols=cols;
            board.SuspendLayout(); board.Controls.Clear(); board.RowStyles.Clear(); board.ColumnStyles.Clear();
            board.RowCount=rows; board.ColumnCount=cols;
            for(int i=0;i<cols;i++) board.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/Math.Max(1,cols)));
            for(int i=0;i<rows;i++) board.RowStyles.Add(new RowStyle(SizeType.Percent,100f/Math.Max(1,rows)));
            float fontSize = cols>=10 || rows>=8 ? 20F : 28F;
            for(int r=0;r<rows;r++) for(int c=0;c<cols;c++) board.Controls.Add(new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI Symbol",12F,FontStyle.Regular,GraphicsUnit.Pixel),Margin=Padding.Empty},c,r);
            board.ResumeLayout();
            ResizeBoardSquare();
        }
        void AdjustBoardFonts(){
            float size = Math.Max(10F, currentCell * 0.62F);
            foreach(Control c in board.Controls){
                c.Font = new Font("Segoe UI Symbol", size, FontStyle.Regular, GraphicsUnit.Pixel);
            }
        }        void ResizeBoardSquare(){
            if(boardHost==null || currentRows<=0 || currentCols<=0) return;
            int cell = Math.Max(20, Math.Min(boardHost.ClientSize.Width / currentCols, boardHost.ClientSize.Height / currentRows));
            currentCell = cell;
            board.Width = cell * currentCols;
            board.Height = cell * currentRows;
            board.Left = Math.Max(0, (boardHost.ClientSize.Width - board.Width) / 2);
            board.Top = Math.Max(0, (boardHost.ClientSize.Height - board.Height) / 2);
            AdjustBoardFonts();
        }        void RenderFile(string p,string title){ try{ var s=Read(p); summary.Text=title+"\r\nState="+s.State+" Run="+s.Run+" Wave="+s.Wave+" Coins="+s.Coins+" Difficulty="+s.Difficulty+" 棋盘="+s.Rows+"行×"+s.Cols+"列 白="+s.White+" 黑="+s.Black; BuildBoard(s.Rows,s.Cols); gambitsBox.Text=string.Join(Environment.NewLine,s.Gambits); stockBox.Text=string.Join(Environment.NewLine,s.Stock); for(int x=0;x<s.Rows;x++) for(int y=0;y<s.Cols;y++){ int idx=x*s.Cols+y; string val=idx<s.Pieces.Length?s.Pieces[idx]:""; var lab=(Label)board.GetControlFromPosition(y,x); var tk=Token(val); lab.Text=tk.Item1; lab.BackColor=string.IsNullOrWhiteSpace(val)?SquareColor(x,y):BlendSquare(SquareColor(x,y), tk.Item2); lab.ForeColor=tk.Item3; tip.SetToolTip(lab,"row/x="+x+" col/y="+y+" idx="+idx+"\r\n"+val); } }catch(Exception ex){ Clear("读取失败: "+ex.Message); } }
        void Clear(string msg){ summary.Text=msg; foreach(Control c in board.Controls){ c.Text="."; c.BackColor=Color.White; c.ForeColor=Color.Gray; } }
        Color SquareColor(int row,int col){ return ((row+col)%2==0)?Color.White:Color.FromArgb(220,220,220); }
        Color BlendSquare(Color square, Color pieceBack){ return Color.FromArgb((square.R+pieceBack.R)/2,(square.G+pieceBack.G)/2,(square.B+pieceBack.B)/2); }
        Tuple<string,Color,Color> Token(string v){
            if(string.IsNullOrWhiteSpace(v)) return Tuple.Create("",Color.White,Color.Gray);
            var p=v.Split('_');
            if(p.Length<2) return Tuple.Create("?",Color.LightGray,Color.Black);
            string piece=p[0], side=p[1];
            string glyph="?";
            if(side.StartsWith("W")){
                if(piece=="K") glyph="♔"; else if(piece=="Q") glyph="♕"; else if(piece=="R") glyph="♖"; else if(piece=="B") glyph="♗"; else if(piece=="N") glyph="♘"; else if(piece=="P") glyph="♙";
                return Tuple.Create(glyph,Color.FromArgb(245,245,245),Color.Black);
            }
            if(side.StartsWith("B")){
                if(piece=="K") glyph="♚"; else if(piece=="Q") glyph="♛"; else if(piece=="R") glyph="♜"; else if(piece=="B") glyph="♝"; else if(piece=="N") glyph="♞"; else if(piece=="P") glyph="♟";
                return Tuple.Create(glyph,Color.FromArgb(210,210,210),Color.Black);
            }
            return Tuple.Create("?",Color.LightGray,Color.Black);
        }        void RestoreSelected(){ var b=backups.SelectedItem as BackupItem; if(b==null){Msg("请选择备份");return;} if(MessageBox.Show(this,"请确认游戏已退出。\r\n还原备份："+Path.GetFileName(b.Path)+"\r\n覆盖："+Save+"\r\n继续？","确认还原",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return; try{ if(File.Exists(Save)) File.Copy(Save,Save+".before_restore_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".bak",true); Directory.CreateDirectory(Path.GetDirectoryName(Save)); File.Copy(b.Path,Save,true); Stat("已还原: "+Path.GetFileName(b.Path)); RenderCurrent(); }catch(Exception ex){Msg("还原失败: "+ex.Message);} }
        void DeleteSelectedBackup(){
            var b=backups.SelectedItem as BackupItem;
            if(b==null){ Msg("请选择要删除的备份"); return; }
            if(MessageBox.Show(this,"确定删除这个备份？\r\n"+Path.GetFileName(b.Path),"确认删除",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
            try{ File.Delete(b.Path); Stat("已删除备份: "+Path.GetFileName(b.Path)); RefreshList(); }
            catch(Exception ex){ Msg("删除失败: "+ex.Message); }
        }
        void DeleteAllBackups(){
            if(!Directory.Exists(BakDir)){ Msg("备份目录不存在"); return; }
            var files=Directory.GetFiles(BakDir,"*.xml");
            if(files.Length==0){ Msg("没有可删除的备份"); return; }
            if(MessageBox.Show(this,"确定删除全部备份？\r\n数量: "+files.Length+"\r\n目录: "+BakDir,"确认删除全部",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
            try{ int c=0; foreach(var f in files){ File.Delete(f); c++; } Stat("已删除全部备份: "+c+" 个"); RefreshList(); Clear("已删除全部备份"); }
            catch(Exception ex){ Msg("删除全部失败: "+ex.Message); }
        }        void SetCoins(){ try{ BackupBefore("coins"); var doc=LoadDoc(Save); var n=doc.SelectSingleNode("/Data/Coins"); if(n==null) throw new Exception("找不到Coins"); var old=n.InnerText; n.InnerText=coins.Value.ToString(); SaveDoc(doc, Save); Stat("金币: "+old+" -> "+coins.Value); RenderCurrent(); }catch(Exception ex){Msg("修改失败: "+ex.Message);} }
        void RemoveBlack(string p,string label){ try{ if(!File.Exists(p)){Msg("文件不存在");return;} if(MessageBox.Show(this,"删除"+label+"里的所有黑方/敌方棋子？会先备份。","确认",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return; File.Copy(p,p+".before_remove_black_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".bak",true); var doc=LoadDoc(p); int c=0; var ns=doc.SelectNodes("/Data/PiecesOnBoard/string"); if(ns!=null) foreach(XmlNode n in ns) if(Regex.IsMatch(n.InnerText??"","^[A-Z]_B[^_]*(_|$)")){ n.InnerText=""; c++; } SaveDoc(doc, p); Stat(label+"已删除黑棋 "+c+" 个"); RenderFile(p,label); }catch(Exception ex){Msg("删除失败: "+ex.Message);} }
        void ApplyStringList(string nodeName, List<string> values){ try{ BackupBefore(nodeName); var doc=LoadDoc(Save); var parent=doc.SelectSingleNode("/Data/"+nodeName); if(parent==null) throw new Exception("找不到 "+nodeName); parent.RemoveAll(); foreach(var v in values){ var n=doc.CreateElement("string"); n.InnerText=v; parent.AppendChild(n); } SaveDoc(doc, Save); Stat("已修改 "+nodeName+"，数量 "+values.Count); RenderCurrent(); }catch(Exception ex){Msg("应用失败: "+ex.Message);} }
        XmlDocument LoadDoc(string path){ var doc=new XmlDocument(); var text=File.ReadAllText(path, Encoding.UTF8); doc.LoadXml(text); return doc; }
        void SaveDoc(XmlDocument doc,string path){ var settings=new XmlWriterSettings{Encoding=new UTF8Encoding(false),Indent=true}; using(var w=XmlWriter.Create(path,settings)){ doc.Save(w); } }        void BackupBefore(string tag){ if(!File.Exists(Save)) throw new Exception("找不到当前存档"); File.Copy(Save,Save+".before_"+tag+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".bak",true); }
        List<string> Lines(string s){ return s.Replace("\r","").Split('\n').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList(); }
        void Stat(string s){ status.Text="  "+s; }
        void Msg(string s){ MessageBox.Show(this,s,"Gambonanza Save Manager",MessageBoxButtons.OK,MessageBoxIcon.Information); }
    }
}













