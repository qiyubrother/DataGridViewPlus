using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Qiyubrother.Properties;
using ContentAlignment = System.Drawing.ContentAlignment;

namespace Qiyubrother
{
    public partial class FilterForm : Form
    {
        private DataGridViewAutoFilterColumnHeaderCell headerCell = null;
        public FilterForm(DataGridViewAutoFilterColumnHeaderCell ownerColunmHeaderCell)
        {
            InitializeComponent();
            KeyPreview = true;
            VisibleChanged += delegate
                                  {
                                      if (Visible)
                                      {
                                          txtFilter.Focus();
                                      }
                                      if (!isFirstShow && Visible)
                                      {
                                          ProcessSelectedItems();
                                      }
                                  };
            rbAnd.CheckedChanged += FilterTextChanged;
            rbOr.CheckedChanged += FilterTextChanged;
            headerCell = ownerColunmHeaderCell;
            inputText = ownerColunmHeaderCell.FilterFormInputText;
        }
        DataTable dt = new DataTable("DataSource");
        private DataTable dtBackground = null;
        private bool isFirstShow = true;
        public string Whole = string.Empty;
        private string inputText = string.Empty;
        protected override void OnShown(EventArgs e)
        {
            txtFilter.Focus();

            btnOK.Click += delegate
                               {
                                   ProcessSelectedItems();
                               };
            btnCancel.Click += delegate { headerCell.HideDropDownList(); };
            if (DataSource == null)
            {
                DataSource = new List<string>();
            }
            txtFilter.KeyDown += (sender, args) =>
            {
                if (args.KeyCode == Keys.Down)
                {
                    dgv.Focus();
                    dgv.Select();
                }
            };
            dgv.AllowUserToResizeRows = false;
            dgv.CellDoubleClick += delegate { btnOK.PerformClick(); };
            dgv.KeyDown += delegate(object sender, KeyEventArgs key)
                               {
                                   Trace.WriteLine("==[FilterForm.dgv.KeyDown]==" + DateTime.Now.ToLongTimeString());
                                   switch (key.KeyCode)
                                   {
                                       case Keys.Enter:
                                           headerCell.FilterFormInputText = txtFilter.Text;
                                           headerCell.UpdateFilter();
                                           headerCell.HideDropDownList();
                                           break;
                                       case Keys.Escape:
                                           headerCell.HideDropDownList();
                                           break;
                                   }
                               };
            dgv.CellMouseClick += (sender, args) =>
                                     {
                                         if (dgv.CurrentCell != null && dgv.CurrentCell.OwningColumn.Index == 0)
                                         {
                                             if (dgv.CurrentCell.RowIndex == 0 && dgv.CurrentCell.ColumnIndex == 0)
                                             {
                                                 var val = Convert.ToBoolean(dgv.Rows[0].Cells["Sel"].Value);
                                                 var isFirstRow = true;
                                                 foreach (DataGridViewRow dgvr in dgv.Rows)
                                                 {
                                                     if (isFirstRow)
                                                     {
                                                         isFirstRow = false;
                                                     }
                                                     dgvr.Cells["Sel"].Value = !val;
                                                 }
                                                 dgv.CommitEdit();
                                             }
                                             else
                                             {
                                                 var sel = !Convert.ToBoolean(dgv.CurrentCell.OwningRow.Cells["Sel"].Value);
                                                 dgv.CurrentCell.OwningRow.Cells["Sel"].Value = sel;
                                                 var val = dgv.CurrentCell.OwningRow.Cells["Values"].Value.ToString();
                                                 if (val != "(Blanks)" && val != "(NonBlanks)" && val != "(Custom...)")
                                                 {
                                                     if (!sel)
                                                     {
                                                         dgv.Rows[0].Cells["Sel"].Value = false;
                                                     }
                                                     else
                                                     {
                                                         bool isAllSel = true;

                                                         for (int i = 1; i < dgv.Rows.Count; i++ )
                                                         {
                                                             var dgvr = dgv.Rows[i];
                                                             if (!Convert.ToBoolean(dgvr.Cells["Sel"].Value))
                                                             {
                                                                 isAllSel = false;
                                                                 break;
                                                             }
                                                         }
                                                         if (isAllSel)
                                                         {
                                                             dgv.Rows[0].Cells["Sel"].Value = true;
                                                         }
                                                     }
                                                     dgv.CommitEdit();
                                                 }
                                             }
                                         }
                                     };
            if (DataSource == null)
            {
                DataSource = new List<string>();
            }
            else
            {
                dt.Columns.Add("Sel", typeof (bool));
                dt.Columns.Add("Values");
                foreach (var val in DataSource)
                {
                    dt.Rows.Add(true, val);
                }
                dt.AcceptChanges();

                dtBackground = dt.Clone();

                foreach (DataRow dr in dt.Rows.AsParallel())
                {
                    dtBackground.LoadDataRow(dr.ItemArray, true);
                }
                
                dgv.DataSource = dtBackground.DefaultView;
            }
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.RowHeadersVisible = false;
            dgv.ColumnHeadersVisible = false;
            dgv.BackColor = Color.Gray;
            dgv.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            dgv.Columns["Sel"].Width = 30;
            dgv.Columns["Sel"].Resizable = DataGridViewTriState.False;
            dgv.Columns["Values"].ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.Columns["Values"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            base.OnShown(e);

            var l = dtBackground.DefaultView.Cast<DataRowView>().Count(x => x["Values"].ToString() != "(Select All)" && x["Values"].ToString() != "(All)" && x["Values"].ToString() != "(Custom...)");
            dgv.DataSource = dtBackground.DefaultView;
            lblStat.Text = string.Format("{0}", l);

            Focus();
            dgv.CommitEdit();
            dgv.Focus();
            txtFilter.Focus();
            txtFilter.Text = inputText;
            rbAnd.Checked = headerCell.FilterMode == EnumFilterMode.And;
            rbOr.Checked = headerCell.FilterMode == EnumFilterMode.Or;
            FilterTextChanged(null, null);
            isFirstShow = false;
        }

        public void Clear()
        {
            DataSource = new List<string>();
        }

        private void ProcessSelectedItems()
        {
            if (SelectItems == null)
            {
                SelectItems = new List<string>();
            }
            if (dgv.CurrentCell != null)
            {
                SelectItems.Clear();
                if (dgv.CurrentCell.OwningRow.Cells["Values"].Value.ToString() == "(Custom...)"
                    || dgv.CurrentCell.OwningRow.Cells["Values"].Value.ToString() == "(Blanks)"
                    || dgv.CurrentCell.OwningRow.Cells["Values"].Value.ToString() == "(NonBlanks)")
                {
                    SelectItems.Add(dgv.CurrentCell.OwningRow.Cells["Values"].Value.ToString());
                }
                else
                {
                    for (var i = 0; i < dgv.Rows.Count; i++)
                    {
                        var dgvr = dgv.Rows[i];
                        if (i == 0)
                        {
                            var val = dgvr.Cells["Values"].Value.ToString();
                            if (Convert.ToBoolean(dgvr.Cells["Sel"].Value)
                                && val == "(All)")
                            {
                                SelectItems.Clear();
                                SelectItems.Add(dgvr.Cells["Values"].Value.ToString());
                                break;
                            }
                        }
                        if (Convert.ToBoolean(dgvr.Cells["Sel"].Value))
                        {
                            if (dgvr.Cells["Values"].Value.ToString() == "(Select All)"
                                || dgvr.Cells["Values"].Value.ToString() == "(All)"
                                || dgvr.Cells["Values"].Value.ToString() == "(Blanks)"
                                || dgvr.Cells["Values"].Value.ToString() == "(NonBlanks)"
                                || dgvr.Cells["Values"].Value.ToString() == "(Custom...)"
                                )
                            {
                                continue;
                            }
                            SelectItems.Add(dgvr.Cells["Values"].Value.ToString());
                        }
                    }
                }
            }
            headerCell.FilterFormInputText = txtFilter.Text;
            headerCell.FilterMode = rbAnd.Checked ? EnumFilterMode.And : EnumFilterMode.Or;
            headerCell.UpdateFilter();
            headerCell.HideDropDownList();
        }


        [DefaultValue("")]
        public IList<string> SelectItems { get; set; }
       
        public IList<string> DataSource { get; set; }

        private void FilterTextChanged(object sender, EventArgs e)
        {
            Func<string, string> funcParsePlus = s =>
            {
                const char quotationMarks = '"';
                const char space = ' ';
                var keyList = new List<string>();
                char status = 'n'; // n:normal q:with quotation marks

                if (string.IsNullOrEmpty(s)) return string.Empty;
                if (s == "\"") return string.Empty;

                var sb = new StringBuilder();
                for (var i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (c == quotationMarks) // 是双引号
                    {
                        if (sb.Length > 0
                            && sb[sb.Length - 1] == quotationMarks) // 连续出现双引号
                        {
                            status = 'n';
                            sb.Append(c); // 添加双引号
                        }
                        else
                        {
                            if (status == 'n')// 双引号开始
                            {
                                status = 'q';
                            }
                            else if (status == 'q')// 双引号结束
                            {
                                status = 'n';
                                keyList.Add(sb.ToString());
                                sb.Length = 0;
                            }
                        }

                    }
                    else if (c == space)
                    {
                        if (status == 'q')
                        {
                            sb.Append(c); // 空格视作普通字符
                        }
                        else if (status == 'n')
                        {
                            if (sb.Length > 0)
                            {
                                keyList.Add(sb.ToString());// 空格视作分隔符
                                sb.Length = 0;
                            }
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                keyList.Add(sb.ToString());

                sb.Length = 0;
                if (rbAnd.Checked)
                {
                    foreach (var item in keyList)
                    {
                        var val = item.Replace("%", "[%]").Replace("*", "[*]");
                        sb.Append(string.Format("Values like '%{0}%' and ", val));
                    }
                }
                else
                {
                    // 当前值可以拆分成多个[或]的关系
                    sb.Append("(");
                    foreach (var item in keyList)
                    {
                        var val = item.Replace("%", "[%]").Replace("*", "[*]");


                        foreach (var subVal in val.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            sb.Append(string.Format("Values like '%{0}%' or ", subVal));
                        }
                    }
                    if (sb.Length > 3) sb.Length -= 3;
                    sb.Append(") and ");
                }

                sb.Append(" Values <> '(Custom...)' and Values <> '(Blanks)' and Values <> '(NonBlanks)'");

                return sb.ToString();
            };
            dt.DefaultView.RowFilter = funcParsePlus(txtFilter.Text);
            dtBackground.Clear();
            dtBackground = dt.DefaultView.ToTable();
            var l = dtBackground.DefaultView.Cast<DataRowView>().Count(x => x["Values"].ToString() != "(Select All)" && x["Values"].ToString() != "(All)" && x["Values"].ToString() != "(Custom...)");
            dgv.DataSource = dtBackground.DefaultView;
            lblStat.Text = string.Format("{0}", l);
        }

    }
    public class TextBox2 : TextBox
    {
        Button btnClear = new Button();
        public TextBox2()
        {
            this.Controls.Add(btnClear);
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Width = btnClear.Height;
            btnClear.Dock = DockStyle.Right;
            btnClear.Image = new Bitmap(Resources.ClearFilter2.ToBitmap(), 14, 14);

            btnClear.ImageAlign = ContentAlignment.MiddleLeft;
            btnClear.MouseHover += delegate
                                       {
                                           Cursor = Cursors.Arrow;
                                       };
            btnClear.Click += delegate { Text = string.Empty; Focus(); };
        }

    }
}
