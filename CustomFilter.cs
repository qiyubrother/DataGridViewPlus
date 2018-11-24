using System;
using System.Windows.Forms;

namespace Qiyubrother
{
    public partial class CustomFilter : Form
    {
        private string _filter = string.Empty;
        private string _filterFormatString = string.Empty;
        private FilterType _CustomFilterType = FilterType.Contains;
        private Type _colType = null;
        private string _colName = string.Empty;
        public CustomFilter(Type columnType, String columnName)
        {
            InitializeComponent();
            _colType = columnType;
            _colName = columnName;

            if (columnType == typeof(String))
            {
                comboType.Items.AddRange(new object[]
                                             {
                                                "contains",
                                                "does not contain",
                                                "start with",
                                                "end with"
                });
                comboType.SelectedIndex = 0;
            }
            else if (columnType == typeof(System.Decimal))
            {
                comboType.Items.AddRange(new object[]
                                             {
                                                "equals",
                                                "does not equal",
                                                "is greater than",
                                                "is greater than or equal to",
                                                "is less than",
                                                "is less than or equal to"
                });
                comboType.SelectedIndex = 0;
            }
            else if (columnType == typeof(DateTime))
            {
                comboType.Text = "equals";
                comboType.Enabled = false;
                lblPrompt.Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ToString();
            }
            txtColumnDataType.Text = columnType.Name;
            base.Text += " - [" + columnName + "]";

        }
        public Type ValueType
        {
            get { return _colType; }
        }
        private void CustomFilter_Load(object sender, EventArgs e)
        {
            txtFilter.Text = _filter;
            if (comboType.Items.Count > 0)
            {
                switch (CustomFilterType)
                {
                    case FilterType.Contains:
                        comboType.SelectedIndex = 0;
                        break;
                    case FilterType.DoesNotContain:
                        comboType.SelectedIndex = 1;
                        break;
                    case FilterType.StartWith:
                        comboType.SelectedIndex = 2;
                        break;
                    case FilterType.EndWith:
                        comboType.SelectedIndex = 3;
                        break;
                    case FilterType.Equal:
                        comboType.SelectedIndex = 0;
                        break;
                    case FilterType.DoesNotEqual:
                        comboType.SelectedIndex = 1;
                        break;
                    case FilterType.IsGreaterThan:
                        comboType.SelectedIndex = 2;
                        break;
                    case FilterType.IsGreaterThanOrEqualTo:
                        comboType.SelectedIndex = 3;
                        break;
                    case FilterType.IsLessThan:
                        comboType.SelectedIndex = 4;
                        break;
                    case FilterType.IsLessThanOrEqualTo:
                        comboType.SelectedIndex = 5;
                        break;
                }
            }
        }
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }
        public string FilterFormatString
        {
            get { return _filterFormatString; }
            set { _filterFormatString = value; }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            switch(comboType.Text)
            {
                case "contains":
                    CustomFilterType = FilterType.Contains;
                    FilterFormatString = string.Format(" {0} like '%{1}%'", _colName, txtFilter.Text.Trim());
                    break;
                case "does not contain":
                    CustomFilterType = FilterType.DoesNotEqual;
                    FilterFormatString = string.Format(" {0} not like '%{1}%'", _colName, txtFilter.Text.Trim());
                    break;
                case "start with":
                    CustomFilterType = FilterType.StartWith;
                    FilterFormatString = string.Format(" {0} like '{1}%'", _colName, txtFilter.Text.Trim());
                    break;
                case "end with":
                    CustomFilterType = FilterType.EndWith;
                    FilterFormatString = string.Format(" {0} like '%{1}'", _colName, txtFilter.Text.Trim());
                    break;
                case "equals":
                    FilterFormatString = string.Format(" {0} = {1}", _colName, txtFilter.Text.Trim());
                    break;
                case "does not equal":
                    CustomFilterType = FilterType.DoesNotEqual;
                    FilterFormatString = string.Format(" {0} <> {1}", _colName, txtFilter.Text.Trim());
                    break;
                case "is greater than":
                    CustomFilterType = FilterType.IsGreaterThan;
                    FilterFormatString = string.Format(" {0} > {1}", _colName, txtFilter.Text.Trim());
                    break;
                case "is greater than or equal to":
                    CustomFilterType = FilterType.IsGreaterThanOrEqualTo;
                    FilterFormatString = string.Format(" {0} >= {1}", _colName, txtFilter.Text.Trim());
                    break;
                case "is less than":
                    CustomFilterType = FilterType.IsLessThan;
                    FilterFormatString = string.Format(" {0} < {1}", _colName, txtFilter.Text.Trim());
                    break;
                case "is less than or equal to":
                    CustomFilterType = FilterType.IsLessThanOrEqualTo;
                    FilterFormatString = string.Format(" {0} <= {1}", _colName, txtFilter.Text.Trim());
                    break;
            }
            double Result = 0;
            if (txtColumnDataType.Text == "Decimal" && !System.Double.TryParse(txtFilter.Text, out Result))
            {
                txtFilter.Focus();
                return;
            }
            _filter = txtFilter.Text;

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public FilterType CustomFilterType
        {
            get{return _CustomFilterType;}
            set{_CustomFilterType = value;}
        }

        private void comboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtFilter.Focus();
        }
    }
    public enum FilterType
    {
        IsLessThanOrEqualTo =7,
        IsLessThan = 6,
        IsGreaterThanOrEqualTo = 5,
        IsGreaterThan = 4,
        DoesNotContain = 3,
        DoesNotEqual = 2,
        Equal = 1,
        Contains = 0,
        StartWith = 8,
        EndWith = 9,
    }
}
