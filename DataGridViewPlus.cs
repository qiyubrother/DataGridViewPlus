using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Qiyubrother.Properties;
using InnerUtils;
namespace Qiyubrother
{
    public delegate void KeyEventHandler(object sender, KeyEventArgs2 e);
    public partial class DataGridViewPlus : DataGridView
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point p);

        private string _guid = string.Empty;
        private string _functionCode = string.Empty;
        private ContextMenuStrip cmsMenu = new ContextMenuStrip();
        public event EventHandler FltrChngdEventHandler;
        public event KeyEventHandler GridKeyDownEventHandler;
        protected readonly Font defaultFont = new Font("Tahoma", 8f);
        public string preCstmFltrVl = string.Empty;
        public string preCustomFilterColName = string.Empty;
        public FilterType preCustomFilterType = FilterType.Contains;
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern bool BtBlt(IntPtr pHdc, int iX, int iY, int iWidth, int iHeight, IntPtr pHdcSource, int iXSource, int iYSource, System.Int32 dw);
        private bool isAltKeyDown = false;
        private bool isCtrolKeyDown = false;
        private const int SRC = 0xcc0020;
        protected int currLineColorIndex = 0;
        public static string BreakLine = "<br>";

        [DefaultValue(true)]
        public bool IsEnableStyle { get; set; }

        [DefaultValue(typeof(bool), "false")]
        public bool IsCancelBaseKeyEvent { get; set; }

        public DataGridViewPlus()
        {
            InitializeComponent();
            AllowUserToFilter = true;
            IsActive = true;
            IsEnableContentMenu = true;
            IsAutoSaveLayout = true;
            IsEnableStyle = false;
            DataTimeFormat = "MM/dd/yyyy";
            foreach (DataGridViewColumn col in Columns)
            {
                if (col.HeaderCell is DataGridViewAutoFilterColumnHeaderCell)
                {
                    var hc = col.HeaderCell as DataGridViewAutoFilterColumnHeaderCell;
                    hc.ColumnFilterChanged +=ClmnFltrChngd;
                }
            }
            BindingContextChanged += delegate
                {
                    if (DataSource == null)
                    {
                        return;
                    }
                    foreach (DataGridViewColumn col in Columns)
                    {
                        col.HeaderCell = new DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    }
                };
            CellFormatting += (sender, e) =>
                                  {
                                      if (Columns[e.ColumnIndex].ValueType == typeof(DateTime))
                                      {
                                          if (e.Value != DBNull.Value)
                                          {
                                              try
                                              {
                                                  e.Value = Convert.ToDateTime(e.Value).ToString(DataTimeFormat, CultureInfo.GetCultureInfo("en-US"));
                                                  e.FormattingApplied = true;
                                                  e.CellStyle.Format = DataTimeFormat;
                                              }
                                              catch (Exception)
                                              {
                                                  e.FormattingApplied = false;
                                              }
                                          }
                                      }
                                  };

            BorderStyle = BorderStyle.None;
            BackgroundColor = Color.SlateGray;
            if (Menu == null || Menu.IsDisposed)
            {
                Menu = new ContextMenuStrip();
            }
            Menu.TopLevel = false;
            Menu.Parent = this;
            Menu.Name = "DataGridViewContextMenu";
            Menu.Size = new Size(20, 30);
            var tsmiClearFilter = new ToolStripMenuItem("&Clear Filter", null, MenuClick);
            tsmiClearFilter.Name = "ClearFilter";
            Menu.Items.Add(tsmiClearFilter); // ClearFilter
            base.ContextMenuStrip = Menu;
            ColumnHeadersHeight = 50;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            isAltKeyDown = false;
            isCtrolKeyDown = false;
            base.OnMouseUp(e);

        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (GridKeyDownEventHandler != null)
            {
                GridKeyDownEventHandler(this, new KeyEventArgs2(e));
                if (IsCancelBaseKeyEvent) return;
            }

            isAltKeyDown = e.Alt;
            isCtrolKeyDown = e.Control;

            if (e.KeyCode == Keys.F8)
            {
                if (CurrentCell != null && CurrentCell.RowIndex >= 0 && CurrentCell.ColumnIndex >= 0)
                {
                    var customFilter = new CustomFilter(CurrentCell.OwningColumn.ValueType,
                                                        CurrentCell.OwningColumn.HeaderText);
                    if (customFilter.ShowDialog(FindForm()) == DialogResult.OK)
                    {
                        if (DataSource is IBindingListView)
                        {
                            var dataSource = DataSource as IBindingListView;
                            var newColumnFilter = customFilter.FilterFormatString;
                            if (CurrentCell.OwningColumn.HeaderCell is DataGridViewAutoFilterColumnHeaderCell)
                            {
                                var hc = CurrentCell.OwningColumn.HeaderCell as DataGridViewAutoFilterColumnHeaderCell;


                                var newFilter = hc.FilterWithoutCurrentColumn(dataSource.Filter);
                                if (String.IsNullOrEmpty(newFilter))
                                {
                                    newFilter += newColumnFilter;
                                }
                                else
                                {
                                    newFilter += " AND " + newColumnFilter;
                                }

                                newFilter = newFilter.Trim();
                                if (newFilter.Length > 3)
                                {
                                    if (newFilter.Substring(newFilter.Length - 3).ToUpper() == "AND")
                                    {
                                        newFilter = newFilter.Substring(0, newFilter.Length - 3);
                                    }
                                }
                                dataSource.Filter = newFilter;
                                hc.CurrentColumnFilter = newFilter;
                                ClmnFltrChngd(this, new EventArgs());
                                hc.AutoFixLockedColumns();
                                hc.IsFiltered = true;
                            }
                        }
                    }
                }
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                if (!e.Shift && SelectedCells.Count > 1)
                {
                    CopySelToClipBoard();
                }
                else
                {
                    base.OnKeyDown(e);
                }
            }
            else if (e.KeyCode == Keys.V && e.Control && e.Alt)
            {
                PasteAreaFromClipboard();
            }
            else if (e.KeyCode == Keys.F12 && e.Control && !e.Shift)
            {
                var sfd = new SaveFileDialog();
                sfd.DefaultExt = "csv";
                sfd.FileName = string.Empty;
                sfd.CheckFileExists = true;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var str = GetCsvFormatData();
                    if (ThinCodeHelper.IsShareDenyOpenFile(sfd.FileName))
                    {
                        throw new Exception("Open file error.");
                    }
                    var sw = new StreamWriter(sfd.FileName, false, Encoding.Default);
                    sw.Write(str);
                    sw.Close();
                }
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
        public void ClmnFltrChngd(object sender, EventArgs e)
        {
            if (FltrChngdEventHandler != null)
            {
                var fe = new FilterEventAges();
                if (sender != null && sender is DataGridViewColumn)
                {
                    fe.ColumnName = (sender as DataGridViewColumn).Name;
                }
                FltrChngdEventHandler(this, e);
            }
        }
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            if (Menu.Visible)
            {
                Menu.Visible = false;
            }
        }
        private int colMouseClickIndex = 0;
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var hti = HitTest(e.X, e.Y);
            colMouseClickIndex = hti.ColumnIndex;
            if (e.Button == MouseButtons.Right && hti.ColumnIndex >= 0)
            {
                if (hti.RowIndex >= 0 && hti.ColumnIndex >=0)
                {
                    bool isOnSelCellClick = false;
                    decimal sum = 0;
                    foreach (DataGridViewCell selCell in SelectedCells)
                    {
                        decimal val = 0;
                        if (decimal.TryParse(selCell.Value.ToString(), out val))
                        {
                            sum += val;
                        }
                    }
                    foreach(DataGridViewCell selCell in SelectedCells)
                    {
                        if (selCell.RowIndex == hti.RowIndex && selCell.ColumnIndex == hti.ColumnIndex)
                        {
                            isOnSelCellClick = true;
                            break;
                        }
                    }
                    if (!isOnSelCellClick)
                    {
                        CurrentCell = Rows[hti.RowIndex].Cells[hti.ColumnIndex];
                    }
                }
                Menu.Show(e.Location);
            }
            else
            {
                if (Menu.Visible)
                {
                    Menu.Visible = false;
                }
            }
        }
        private void MenuClick(object sender, EventArgs e)
        {
            if (sender != null && sender is ToolStripMenuItem)
            {
                var item = sender as ToolStripMenuItem;
                if (colMouseClickIndex >= 0)
                {
                    switch (item.Name)
                    {
                        case "ClearFilter":
                            {
                                var bs = (DataSource as BindingSource);
                                if (bs != null)
                                    bs.Filter = string.Empty;
                                foreach (DataGridViewColumn dgvc in this.Columns)
                                {
                                    if (dgvc.HeaderCell is DataGridViewAutoFilterColumnHeaderCell)
                                    {
                                        (dgvc.HeaderCell as DataGridViewAutoFilterColumnHeaderCell).ClearFilter();
                                    }
                                }
                            }
                            break;
                    }
                }
                else if (item.DropDownItems.Count <= 0)
                {
                    if (Columns.Contains(item.Name))
                    {
                        Columns[item.Name].Visible = true;
                    }
                }
            }
        }
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();

            this.ColumnHeadersHeight = 35;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 8.25F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            this.RowHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.RowsDefaultCellStyle = dataGridViewCellStyle2;
            this.RowTemplate.Height = 23;
            this.DataError += new DataGridViewDataErrorEventHandler(DataGridViewAutoFilter_DataError);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
        }
        protected override void OnRowEnter(DataGridViewCellEventArgs e)
        {
            base.OnRowEnter(e);
            if (e.RowIndex >= 0)
            {
                InvalidateRow(e.RowIndex);
            }
        }
        protected override void OnRowLeave(DataGridViewCellEventArgs e)
        {
            base.OnRowLeave(e);
            if (e.RowIndex >= 0)
            {
                InvalidateRow(e.RowIndex);
            }
        }
        protected override bool SetCurrentCellAddressCore(int columnIndex, int rowIndex, bool setAnchorCellAddress, bool validateCurrentCell, bool throughMouseClick)
        {
            try
            {
                return base.SetCurrentCellAddressCore(columnIndex, rowIndex, setAnchorCellAddress, validateCurrentCell, throughMouseClick);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
        {
            if (Font != defaultFont)
            {
                Font = new Font("Microsoft Sans Serif", 9);
            }
            base.OnRowPostPaint(e);
            if (RowHeadersVisible)
            {
                var rectangle = new Rectangle(e.RowBounds.Location.X,
                    e.RowBounds.Location.Y,
                    RowHeadersWidth - 4,
                    e.RowBounds.Height);
                TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(CultureInfo.InvariantCulture),
                    RowHeadersDefaultCellStyle.Font,
                    rectangle,
                    RowHeadersDefaultCellStyle.ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                e.Handled = false;
                return;
            }
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (Rows[e.RowIndex].Cells[e.ColumnIndex].ValueType == typeof(bool))
                {
                    e.PaintBackground(e.ClipBounds, false);
                    return;
                }
            }
            if (e.Value != null)
            {
                var cellWord = GetFormatValue(Rows[e.RowIndex].Cells[e.ColumnIndex]);// e.Value.ToString();
                var cellRect = e.CellBounds;//默认单元格
                cellRect.Y = cellRect.Y + 3;
                cellRect.Height = defaultFont.Height + 1; // Font.Height;
                var cellWordSize = e.Graphics.MeasureString(cellWord, defaultFont);

                using (Brush defaultTextColor = new SolidBrush(e.CellStyle.ForeColor))
                {
                    e.PaintBackground(e.ClipBounds, true);
                    if (DataSource != null && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    {
                        var drv = Rows[e.RowIndex].Cells[e.ColumnIndex].OwningRow.DataBoundItem as DataRowView;
                        if (drv != null && drv.Row.RowState == DataRowState.Modified && drv.Row[e.ColumnIndex].ToString() != drv.Row[e.ColumnIndex, DataRowVersion.Original].ToString())
                        {
                            e.Graphics.FillRectangle(new SolidBrush(Color.Red), e.CellBounds.Left, e.CellBounds.Top, 3, 3); // 当前Cell 有变更
                        }
                    }
                    if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    {
                        if (Rows[e.RowIndex].Cells[e.ColumnIndex].Selected)
                        {
                            Brush selectionTextColor = new SolidBrush(Color.White);
                            e.Graphics.DrawString(cellWord, defaultFont, selectionTextColor, cellRect, StringFormat.GenericDefault);
                        }
                        else
                        {
                            e.Graphics.DrawString(cellWord, defaultFont, defaultTextColor, cellRect, StringFormat.GenericDefault);
                        }
                    }
                    else
                    {
                        e.Graphics.DrawString(cellWord, defaultFont, defaultTextColor, cellRect, StringFormat.GenericDefault);
                    }
                    if (cellRect.Width < cellWordSize.Width)
                    {
                        var midHeight = cellRect.Top + cellRect.Height / 2;
                        var clr = Color.Blue;
                        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && Rows[e.RowIndex].Cells[e.ColumnIndex].Selected)
                        {
                            var r = 255 - BackgroundColor.R;
                            var g = 255 - BackgroundColor.G;
                            var b = 255 - BackgroundColor.B;
                            clr = Color.FromArgb(r, g, b);
                        }

                        e.Graphics.FillPolygon(new SolidBrush(clr), new[]{
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight - 3),
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight - 2),
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight - 1),
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight - 0),
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight + 1),
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight + 2),
                                                    new PointF(cellRect.Left + cellRect.Width - 5, midHeight + 3),
                                                    new PointF(cellRect.Left + cellRect.Width - 2, midHeight)
                                                                               });
                    }
                    e.Handled = true;
                }
            }
        }
        private void CopySelToClipBoard()
        {
            var minRowIndex = Rows.Count;
            var minColumnIndex = Columns.Count;
            var maxRowIndex = -1;
            var maxColumnIndex = -1;
            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.RowIndex < minRowIndex) minRowIndex = cell.RowIndex;
                if (cell.RowIndex > maxRowIndex) maxRowIndex = cell.RowIndex;
                if (cell.ColumnIndex < minColumnIndex && cell.OwningColumn.Visible)
                {
                    minColumnIndex = cell.ColumnIndex;
                }
                if (cell.ColumnIndex > maxColumnIndex && cell.OwningColumn.Visible)
                {
                    maxColumnIndex = cell.ColumnIndex;
                }
            }
            var sb = new StringBuilder(1024);
            for (var j = minColumnIndex; j <= maxColumnIndex; j++)
            {
                if (Columns[j].Visible)
                {
                    sb.Append(j == maxColumnIndex ? string.Format("\"{0}\"\r\n", Columns[j].Name) : string.Format("\"{0}\"\t", Columns[j].Name));
                }
            }
            for (var i = minRowIndex; i <= maxRowIndex; i++)
            {
                for (var j = minColumnIndex; j <= maxColumnIndex; j++)
                {
                    if (Columns[j].Visible)
                    {
                        var val = Rows[i].Cells[j].Value.ToString().Replace("\n", " ");
                        sb.Append(j == maxColumnIndex ? string.Format("\"{0}\"\r\n", val) : string.Format("\"{0}\"\t", val));
                    }
                }
            }

            var txtBox = new TextBox();
            txtBox.Text = sb.ToString();
            txtBox.SelectAll();
            txtBox.Copy();
            txtBox.Dispose();
            sb.Clear();
        }

        private void PasteAreaFromClipboard()
        {
            var r = new CsvReader(Encoding.UTF8, Clipboard.GetText().Replace("\t", ","));
            var dt = r.ReadIntoDataTable();
            if (dt != null && dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow row in SelectedRows)
                    {
                        foreach (DataGridViewColumn col in Columns)
                        {
                            var cell = row.Cells[col.Name];
                            if (dt.Rows.Count > 0 && dt.Columns.Contains(col.Name))
                            {
                                cell.Value = dt.Rows[0][col.Name];
                            }
                        }
                        if (dt.Rows.Count > 0)
                        {
                            dt.Rows.RemoveAt(0);
                            dt.AcceptChanges();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (CurrentCell == null) return;
                    for (var i = CurrentCell.RowIndex; i < Rows.Count; i++)
                    {
                        var row = Rows[i];
                        foreach (DataGridViewColumn col in Columns)
                        {
                            var cell = row.Cells[col.Name];
                            if (dt.Rows.Count > 0 && dt.Columns.Contains(col.Name))
                            {
                                cell.Value = dt.Rows[0][col.Name];
                            }
                        }
                        if (dt.Rows.Count > 0)
                        {
                            dt.Rows.RemoveAt(0);
                            dt.AcceptChanges();
                        }
                        else
                        {
                            break;
                        }
                    }
                }

            }
        }
        private string GetCsvFormatData()
        {
            var minRowIndex = Rows.Count;
            var minColumnIndex = Columns.Count;
            var maxRowIndex = -1;
            var maxColumnIndex = -1;
            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.RowIndex < minRowIndex) minRowIndex = cell.RowIndex;
                if (cell.RowIndex > maxRowIndex) maxRowIndex = cell.RowIndex;
                if (cell.ColumnIndex < minColumnIndex && cell.OwningColumn.Visible)
                {
                    minColumnIndex = cell.ColumnIndex;
                }
                if (cell.ColumnIndex > maxColumnIndex && cell.OwningColumn.Visible)
                {
                    maxColumnIndex = cell.ColumnIndex;
                }
            }
            var sb = new StringBuilder(1024 * 50);
            for (var j = minColumnIndex; j <= maxColumnIndex; j++)
            {
                if (Columns[j].Visible)
                {
                    sb.Append(j == maxColumnIndex ? string.Format("\"{0}\"\r\n", Columns[j].Name) : string.Format("\"{0}\",", Columns[j].Name));
                }
            }
            for (var i = minRowIndex; i <= maxRowIndex; i++)
            {
                for (var j = minColumnIndex; j <= maxColumnIndex; j++)
                {
                    if (Columns[j].Visible)
                    {
                        sb.Append(j == maxColumnIndex ? string.Format("\"{0}\"\r\n", Rows[i].Cells[j].Value) : string.Format("\"{0}\",", Rows[i].Cells[j].Value));
                    }
                }
            }

            return sb.ToString();
        }
        public static void ToImage(DataGridView dgv, string sFilePath)
        {
            dgv.Refresh();
            dgv.Select();

            var g = dgv.CreateGraphics();
            var ibitMap = new Bitmap(dgv.ClientSize.Width, dgv.ClientSize.Height, g);
            var iBitMap_gr = Graphics.FromImage(ibitMap);
            var iBitMap_hdc = iBitMap_gr.GetHdc();
            var me_hdc = g.GetHdc();

            BtBlt(iBitMap_hdc, 0, 0, dgv.ClientSize.Width, dgv.ClientSize.Height, me_hdc, 0, 0, SRC);
            g.ReleaseHdc(me_hdc);
            iBitMap_gr.ReleaseHdc(iBitMap_hdc);

            if (string.IsNullOrEmpty(sFilePath))
                return;
            ibitMap.Save(sFilePath, ImageFormat.Bmp);
        }

        public static Bitmap ToImage(DataGridView dgv)
        {
            dgv.Refresh();
            dgv.Select();

            var g = dgv.CreateGraphics();
            var ibitMap = new Bitmap(dgv.ClientSize.Width, dgv.ClientSize.Height, g);
            var iBitMap_gr = Graphics.FromImage(ibitMap);
            var iBitMap_hdc = iBitMap_gr.GetHdc();
            var me_hdc = g.GetHdc();

            BtBlt(iBitMap_hdc, 0, 0, dgv.ClientSize.Width, dgv.ClientSize.Height, me_hdc, 0, 0, SRC);
            g.ReleaseHdc(me_hdc);
            iBitMap_gr.ReleaseHdc(iBitMap_hdc);

            return ibitMap;
        }

        private void DataGridViewAutoFilter_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }

        private string GetFormatValue(string value, ColumnFormatStyle style)
        {
            var d = new decimal();
            if (!Decimal.TryParse(value, out d) &&
                (style == ColumnFormatStyle.N1
                || style == ColumnFormatStyle.N2
                || style == ColumnFormatStyle.N3
                || style == ColumnFormatStyle.N4
                || style == ColumnFormatStyle.N5
                ))
            {
                return value;
            }
            if (style == ColumnFormatStyle.Normal)
            {

                return value;

            }
            else
            {
                if (style == ColumnFormatStyle.N1)
                    return string.Format("{0:f1}", d);
                else if (style == ColumnFormatStyle.N2)
                    return string.Format("{0:f2}", d);
                else if (style == ColumnFormatStyle.N3)
                    return string.Format("{0:f3}", d);
                else if (style == ColumnFormatStyle.N4)
                    return string.Format("{0:f4}", d);
                else if (style == ColumnFormatStyle.N5)
                    return string.Format("{0:f5}", d);
                else if (style == ColumnFormatStyle.YMD1)
                {
                    var date = DateTime.Now;
                    if (!DateTime.TryParse(value, out date))
                    {
                        return value;
                    }
                    else
                    {
                        return string.Format("{0}/{1}/{2}", date.Year, date.Month.ToString().PadLeft(2, '0'), date.Day.ToString().PadLeft(2, '0'));
                    }
                }
                else if (style == ColumnFormatStyle.YMD2)
                {
                    var date = DateTime.Now;
                    if (!DateTime.TryParse(value, out date))
                    {
                        return value;
                    }
                    else
                    {
                        return string.Format("{0}/{1}/{2}", date.Month.ToString().PadLeft(2, '0'), date.Day.ToString().PadLeft(2, '0'), date.Year);
                    }
                }
                else
                {
                    return value;
                }
            }
        }
        private string GetFormatValue(DataGridViewCell cell)
        {
            return cell.Value.ToString();
        }
    }

    public enum ColumnFormatStyle
    {
        Normal,
        N1,
        N2,
        N3,
        N4,
        N5,
        YMD1, // Style:2016/02/25
        YMD2, // Style:02/25/2016
    }

    public class ColumnFormat
    {
        public string ColumnName = string.Empty;
        public ColumnFormatStyle Style = ColumnFormatStyle.Normal;
    }
    public class FilterEventAges : EventArgs
    {
        public string ColumnName { get; set; }

        public FilterEventAges()
        {
            ColumnName = string.Empty;
        }
    }
    class ThinCodeHelper
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr _lopen(string lpPathName, int iReadWrite);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        private const int OfReadwrite = 2;
        private const int OfShareDenyNone = 0x40;
        private static readonly IntPtr HfileError = new IntPtr(-1);

        public static bool IsShareDenyOpenFile(string fileName)
        {
            if (!File.Exists(fileName)) throw new FileNotFoundException();

            var vHandle = _lopen(fileName, OfReadwrite | OfShareDenyNone);
            if (vHandle == HfileError) return true;
            CloseHandle(vHandle);
            return false;
        }
    }

    public class GeneralErrorException : System.IO.IOException
    {
        public GeneralErrorException(string errorMessage) : base(errorMessage) { }

    }
}
    public class KeyEventArgs2 : KeyEventArgs
    {
        public KeyEventArgs2(KeyEventArgs e)
            : base(e.KeyCode)
        {
            ;
        }
    }
