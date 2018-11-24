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
using System.Collections;
using System.Reflection;
using System.Collections.Specialized;
namespace Qiyubrother
{
    public partial class DataGridViewAutoFilterColumnHeaderCell : DataGridViewColumnHeaderCell
    {
        private static FilterForm _filterForm = null;
        public event EventHandler ColumnFilterChanged;

        private OrderedDictionary filters = new OrderedDictionary();

        public String CurrentColumnFilter = String.Empty;

        private const FilterType filterType = FilterType.Contains;
        private readonly Font defaultFont = new Font("Tahoma", 8f);
        public string FilterFormInputText = string.Empty;
        public EnumFilterMode FilterMode = EnumFilterMode.And;

        public DataGridViewAutoFilterColumnHeaderCell(DataGridViewColumnHeaderCell oldHeaderCell)
        {
            if (FilterValueList == null)
            {
                FilterValueList = new List<string>();
            }
            else
            {
                FilterValueList.Clear();
            }
            if (_filterForm == null || _filterForm.IsDisposed)
            {
                _filterForm = new FilterForm(this);
            }
            ContextMenuStrip = oldHeaderCell.ContextMenuStrip;
            ErrorText = oldHeaderCell.ErrorText;
            Tag = oldHeaderCell.Tag;
            ToolTipText = oldHeaderCell.ToolTipText;
            Value = oldHeaderCell.Value;
            ValueType = oldHeaderCell.ValueType;

            if (oldHeaderCell.HasStyle)
            {
                Style = oldHeaderCell.Style;
            }

            var filterCell = oldHeaderCell as DataGridViewAutoFilterColumnHeaderCell;
            if (filterCell != null)
            {
                FilteringEnabled = filterCell.FilteringEnabled;
                AutomaticSortingEnabled = filterCell.AutomaticSortingEnabled;
                DropDownListBoxMaxLines = filterCell.DropDownListBoxMaxLines;
                currentDropDownButtonPaddingOffset =
                    filterCell.currentDropDownButtonPaddingOffset;
            }
        }

        public void ClearFilter()
        {
            FilterFormInputText = string.Empty;
            FilterValueList.Clear();
            IsFiltered = false;
        }

        public IList<string> FilterValueList { get; set; }
        public bool IsFiltered { get;  set; }

        public override object Clone()
        {
            return new DataGridViewAutoFilterColumnHeaderCell(this);
        }

        public void DataGridViewChanged()
        {
            OnDataGridViewChanged();
        }

        protected override void OnDataGridViewChanged()
        {
            if (DataGridView == null) return;

            if (OwningColumn != null)
            {
                if (OwningColumn is DataGridViewImageColumn ||
                (OwningColumn is DataGridViewButtonColumn &&
                ((DataGridViewButtonColumn)OwningColumn).UseColumnTextForButtonValue) ||
                (OwningColumn is DataGridViewLinkColumn &&
                ((DataGridViewLinkColumn)OwningColumn).UseColumnTextForLinkValue))
                {
                    AutomaticSortingEnabled = false;
                    FilteringEnabled = false;
                }

                if (OwningColumn.SortMode == DataGridViewColumnSortMode.Automatic)
                {
                    try
                    {
                        OwningColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
                    }catch{}
                }
            }

            VerifyDataSource();

            HandleDataGridViewEvents();

            SetDropDownButtonBounds();

            base.OnDataGridViewChanged();
        }

        private void VerifyDataSource()
        {
            if (DataGridView == null || DataGridView.DataSource == null || !(DataGridView.DataSource is BindingSource))
            {
                return;
            }
            var data = DataGridView.DataSource as BindingSource;
            if (data == null)
            {
                throw new NotSupportedException(
                    "The DataSource property of the containing DataGridView control " +
                    "must be set to a BindingSource.");
            }
        }

        private void HandleDataGridViewEvents()
        {
            DataGridView.Scroll += DataGridView_Scroll;
            DataGridView.ColumnDisplayIndexChanged += DataGridView_ColumnDisplayIndexChanged;
            DataGridView.ColumnWidthChanged += DataGridView_ColumnWidthChanged;
            DataGridView.ColumnHeadersHeightChanged += DataGridView_ColumnHeadersHeightChanged;
            DataGridView.SizeChanged += DataGridView_SizeChanged;
            DataGridView.DataSourceChanged += DataGridView_DataSourceChanged;
            DataGridView.DataBindingComplete += DataGridView_DataBindingComplete;

            DataGridView.ColumnSortModeChanged += DataGridView_ColumnSortModeChanged;
        }

        private void DataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                ResetDropDown();
            }
        }

        private void DataGridView_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
        {
            ResetDropDown();
        }

        private void DataGridView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            ResetDropDown();
        }

        private void DataGridView_ColumnHeadersHeightChanged(object sender, EventArgs e)
        {
            ResetDropDown();
        }

        private void DataGridView_SizeChanged(object sender, EventArgs e)
        {
            ResetDropDown();
        }

        private void DataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.Reset)
            {
                ResetDropDown();
                ResetFilter();
            }
        }

        private void DataGridView_DataSourceChanged(object sender, EventArgs e)
        {
            VerifyDataSource();
            ResetDropDown();
            ResetFilter();
        }

        private void ResetDropDown()
        {
            InvalidateDropDownButtonBounds();
            if (dropDownListBoxShowing)
            {
                HideDropDownList();
            }
        }

        public void ResetFilter()
        {
            if (DataGridView == null) return;
            var source = DataGridView.DataSource as BindingSource;
            if (source == null || String.IsNullOrEmpty(source.Filter))
            {
                IsFiltered = false;
                FilterValueList.Clear();
                FilterValueList.Add("(All)");
                CurrentColumnFilter = String.Empty;
            }
        }
        private void DataGridView_ColumnSortModeChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column == OwningColumn &&
                e.Column.SortMode == DataGridViewColumnSortMode.Automatic)
            {
                throw new InvalidOperationException(
                    "A SortMode value of Automatic is incompatible with " +
                    "the DataGridViewAutoFilterColumnHeaderCell type. " +
                    "Use the AutomaticSortingEnabled property instead.");
            }
        }

        protected override void Paint(
            Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates cellState,
            object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            cellStyle.Font = defaultFont;
            base.Paint(graphics, clipBounds, cellBounds, rowIndex,
                cellState, value, formattedValue,
                errorText, cellStyle, advancedBorderStyle, paintParts);

            if (!FilteringEnabled ||
                (paintParts & DataGridViewPaintParts.ContentBackground) == 0)
            {
                return;
            }

            var buttonBounds = DropDownButtonBounds;

            if (buttonBounds.Width < 1 || buttonBounds.Height < 1) return;

            if (Application.RenderWithVisualStyles)
            {
                var state = ComboBoxState.Normal;

                if (dropDownListBoxShowing)
                {
                    state = ComboBoxState.Pressed;
                }
                else if (IsFiltered)
                {
                    state = ComboBoxState.Hot;
                }
                ComboBoxRenderer.DrawDropDownButton(
                    graphics, buttonBounds, state);
            }
            else
            {
                var pressedOffset = 0;
                //var state = PushButtonState.Normal;
                if (dropDownListBoxShowing)
                {
                    //state = PushButtonState.Pressed;
                    pressedOffset = 1;
                }
                if (IsFiltered)
                {
                    graphics.FillPolygon(new SolidBrush(Color.Red), new Point[] {
                        new Point(
                            buttonBounds.Width / 2 + 
                                buttonBounds.Left - 1 + pressedOffset, 
                            buttonBounds.Height * 3 / 4 + 
                                buttonBounds.Top - 1 + pressedOffset),
                        new Point(
                            buttonBounds.Width / 4 + 
                                buttonBounds.Left + pressedOffset,
                            buttonBounds.Height / 2 + 
                                buttonBounds.Top - 1 + pressedOffset),
                        new Point(
                            buttonBounds.Width * 3 / 4 + 
                                buttonBounds.Left - 1 + pressedOffset,
                            buttonBounds.Height / 2 + 
                                buttonBounds.Top - 1 + pressedOffset)
                    });
                }
                else
                {
                    graphics.FillPolygon(SystemBrushes.ControlText, new Point[] {
                        new Point(
                            buttonBounds.Width / 2 + 
                                buttonBounds.Left - 1 + pressedOffset, 
                            buttonBounds.Height * 3 / 4 + 
                                buttonBounds.Top - 1 + pressedOffset),
                        new Point(
                            buttonBounds.Width / 4 + 
                                buttonBounds.Left + pressedOffset,
                            buttonBounds.Height / 2 + 
                                buttonBounds.Top - 1 + pressedOffset),
                        new Point(
                            buttonBounds.Width * 3 / 4 + 
                                buttonBounds.Left - 1 + pressedOffset,
                            buttonBounds.Height / 2 + 
                                buttonBounds.Top - 1 + pressedOffset)
                    });
                }
            }

        }
		public bool IsMouseDown = false;
		protected override void OnMouseUp(DataGridViewCellMouseEventArgs e)
		{
			IsMouseDown = false;
			if (_filterForm != null && _filterForm.Visible)
			{
				_filterForm.Focus();
				return;
			}
			base.OnMouseUp(e);
		}
        protected override void OnMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseDown(e);
            IsMouseDown = true;
            _filterForm.Parent = null;
            HideDropDownList();
            
            if (e.Button == MouseButtons.Right)
            {
                base.OnMouseDown(e);
                return;
            }

            if (lostFocusOnDropDownButtonClick)
            {
                lostFocusOnDropDownButtonClick = false;
                return;
            }

            var cellBounds = DataGridView.GetCellDisplayRectangle(e.ColumnIndex, -1, false);
            if (OwningColumn.Resizable == DataGridViewTriState.True &&
                ((DataGridView.RightToLeft == RightToLeft.No &&
                cellBounds.Width - e.X < 6) || e.X < 6))
            {
                return;
            }

            var scrollingOffset = 0;
            if (DataGridView.RightToLeft == RightToLeft.No &&
                DataGridView.FirstDisplayedScrollingColumnIndex ==
                ColumnIndex)
            {
                scrollingOffset = DataGridView.FirstDisplayedScrollingColumnHiddenWidth;
            }

            if (FilteringEnabled &&
                DropDownButtonBounds.Contains(
                e.X + cellBounds.Left - scrollingOffset, e.Y + cellBounds.Top))
            {
                if (DataGridView.IsCurrentCellInEditMode)
                {
                    DataGridView.EndEdit();

                    var source = DataGridView.DataSource as BindingSource;
                    if (source != null)
                    {
                        source.EndEdit();
                    }
                }
                ShowDropDownList();
            }
            else if (AutomaticSortingEnabled &&
                DataGridView.SelectionMode !=
                DataGridViewSelectionMode.ColumnHeaderSelect)
            {
                SortByColumn();
            }
        }
        private void SortByColumn()
        {
            Debug.Assert(DataGridView != null && OwningColumn != null, "DataGridView or OwningColumn is null");

            var sortList = DataGridView.DataSource as IBindingList;
            if (sortList == null ||
                !sortList.SupportsSorting ||
                !AutomaticSortingEnabled)
            {
                return;
            }

            var direction = ListSortDirection.Ascending;
            if (DataGridView.SortedColumn == OwningColumn &&
                DataGridView.SortOrder == SortOrder.Ascending)
            {
                direction = ListSortDirection.Descending;
            }
            DataGridView.Sort(OwningColumn, direction);
        }

        private bool dropDownListBoxShowing;

        public void ShowDropDownList()
        {
            HideDropDownList();
            
            if (DataGridView.CurrentRow != null &&
                DataGridView.CurrentRow.IsNewRow)
            {
                DataGridView.CurrentCell = null;
            }
            var pfr = PopulateFilters();
            var filterArray = new String[filters.Count];
            filters.Keys.CopyTo(filterArray, 0);
            _filterForm.Clear();
            _filterForm.DataSource = filterArray;
            _filterForm.SelectItems = FilterValueList;
            DataGridView.EndEdit();
            HandleDropDownListBoxEvents();
            if (_filterForm.IsDisposed)
            {
                _filterForm = new FilterForm(this);
                _filterForm.DataSource = filterArray;
                _filterForm.SelectItems = FilterValueList;
                if (pfr != null)
                {
                    #region 计算数据长度
                    var l = filterArray.Count(x => x != "(Select All)" && x != "(All)" && x != "(Custom...)");
                    #endregion
                    //_filterForm.lblStat.Text = string.Format("{0}/{1}", l, pfr.Whole);
                    //_filterForm.Whole = pfr.Whole;
                }
            }
            dropDownListBoxShowing = true;
            SetDropDownListBoxBounds();
            _filterForm.Visible = true;
            DataGridView.InvalidateCell(this);
        }
        public void HideDropDownList()
        {

            Debug.Assert(DataGridView != null, "DataGridView is null");
            if (DataGridView == null)
            {
                dropDownListBoxShowing = false;
                _filterForm.Visible = false;
                _filterForm.Dispose();
                return;
            }
            dropDownListBoxShowing = false;
            _filterForm.Visible = false;
            _filterForm.Dispose();
            if (DataGridView == null)
            {
                return;
            }
            DataGridView.InvalidateCell(this);
        }
        private void SetDropDownListBoxBounds()
        {
            var dropDownListBoxLeft = 0;

            var clientLeft = 1;
            
            var clientRight = DataGridView.Right;
            dropDownListBoxLeft = DropDownButtonBounds.Left;
            if (dropDownListBoxLeft < clientLeft)
            {
                dropDownListBoxLeft = clientLeft;
            }
            if (dropDownListBoxLeft + _filterForm.Width > clientRight)
            {
                dropDownListBoxLeft = clientRight - _filterForm.Width - 22;
            }
            var dgvPoint = DataGridView.PointToScreen(new Point(DataGridView.Left,DataGridView.Top));
            _filterForm.Left = dropDownListBoxLeft + dgvPoint.X;
            _filterForm.Top = DropDownButtonBounds.Bottom + dgvPoint.Y;
        }
        private void HandleDropDownListBoxEvents()
        {
            DataGridView.MouseClick -= GridMouseClick;
            DataGridView.MouseClick += GridMouseClick;
        }
        void GridMouseClick(object sender, MouseEventArgs e)
        {
            if (DataGridView == null)
            {
                return;
            }
            DataGridView.HitTestInfo hi = DataGridView.HitTest(e.X, e.Y);
            if (hi.Type != DataGridViewHitTestType.ColumnHeader)
            {
                HideDropDownList();
            }
        }
        private Boolean lostFocusOnDropDownButtonClick;

        private PopulateFiltersResult PopulateFilters()
        {
            if (DataGridView == null) return null;

            var data = DataGridView.DataSource as BindingSource;

            if (!(data != null && data.SupportsFiltering && OwningColumn != null))
            {
                return null;
            }

            var  pfr = new PopulateFiltersResult();
            pfr.Desc = "Whole";
            //pfr.Whole = data.
            data.RaiseListChangedEvents = false;

            var oldFilter = data.Filter;
            var tmpFilter = FilterWithoutCurrentColumn(oldFilter);
            var isNeedRestoreFilter = false; // {true: slow, false:fast}
            data.Filter = tmpFilter;
            //pfr.Shown = data.Count.ToString();
            if (tmpFilter != string.Empty || oldFilter == null)
            {
                isNeedRestoreFilter = true;
            }

            filters.Clear();
            var containsBlanks = false;
            var containsNonBlanks = false;
            var list = new List<object>();
            IList ds = data;
            if (isNeedRestoreFilter && !string.IsNullOrEmpty(data.Filter))
            {
                ds = data.CurrencyManager.List;
            }

            foreach (var item in ds)
            {
                Object value = null;

                var ictd = item as ICustomTypeDescriptor;
                if (ictd != null)
                {
                    PropertyDescriptorCollection properties = ictd.GetProperties();
                    foreach (PropertyDescriptor property in properties)
                    {
                        if (String.Compare(OwningColumn.DataPropertyName,
                            property.Name, true /*case insensitive*/,
                            System.Globalization.CultureInfo.InvariantCulture) == 0)
                        {
                            value = property.GetValue(item);
                            break;
                        }
                    }
                }
                else
                {
                    var properties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (PropertyInfo property in properties)
                    {
                        if (String.Compare(OwningColumn.DataPropertyName,
                            property.Name, true /*case insensitive*/,
                            System.Globalization.CultureInfo.InvariantCulture) == 0)
                        {
                            value = property.GetValue(item, null /*property index*/);
                            break;
                        }
                    }
                }
                if (value == null || value == DBNull.Value)
                {
                    containsBlanks = true;
                    continue;
                }

                list.Add(value);
            }

            
            if (list.Count < 10000)
            {
                list.Sort();
            }
            foreach (var value in list)
            {
                String formattedValue = null;
                var style = OwningColumn.InheritedStyle;
                formattedValue = (String)GetFormattedValue(value, -1, ref style,
                    null, null, DataGridViewDataErrorContexts.Formatting);

                if (String.IsNullOrEmpty(formattedValue))
                {
                    containsBlanks = true;
                }
                else if (!filters.Contains(formattedValue))
                {
                    containsNonBlanks = true;

                    filters.Add(formattedValue, value.ToString());
                }
            }

            if (!string.IsNullOrEmpty(oldFilter)) data.Filter = oldFilter;

            data.RaiseListChangedEvents = true;

            filters.Insert(0, "(All)", null);
            filters.Insert(1, "(Custom...)", null);
            if (containsBlanks && containsNonBlanks)
            {
                filters.Add("(Blanks)", null);
                filters.Add("(NonBlanks)", null);
            }
            list.Clear();

            return pfr;
        }
        public String FilterWithoutCurrentColumn(String filter)
        {
            if (String.IsNullOrEmpty(filter))
            {
                return String.Empty;
            }
            if (!IsFiltered)
            {
                return filter;
            }
            if (string.IsNullOrEmpty(CurrentColumnFilter))
            {
                return string.Empty;
            }
            if (filter.IndexOf(CurrentColumnFilter, StringComparison.Ordinal) > 0)
            {
                return filter.Replace(" AND " + CurrentColumnFilter, String.Empty);
            }
            else
            {
                if (filter.Length > CurrentColumnFilter.Length)
                {
                    return filter.Replace(
                        CurrentColumnFilter + " AND ", String.Empty);
                }
                else
                {
                    return String.Empty;
                }
            }
        }
        public void UpdateFilter()
        {
            FilterValueList = _filterForm.SelectItems;
            if (DataGridView == null) return;

            if (DataGridView.DataSource == null)
            {
                IsFiltered = false;
                CurrentColumnFilter = String.Empty;
                return;
            }
            var data = DataGridView.DataSource as IBindingListView;

            Debug.Assert(data != null && data.SupportsFiltering,
                "DataSource is not an IBindingListView or does not support filtering");

            if (FilterValueList.Count == 1 && FilterValueList[0].Equals("(All)"))
            {
                // 显示所有，清除当前列的过滤
                data.Filter = FilterWithoutCurrentColumn(data.Filter);
                IsFiltered = false;
                CurrentColumnFilter = String.Empty;
                var bsInner = DataGridView.DataSource as BindingSource;
                if (bsInner != null)
                {
                    bsInner.RaiseListChangedEvents = true;
                    bsInner.MoveFirst();
                }
                try
                {
                    var dgv = DataGridView as DataGridViewPlus;
                    if (dgv != null)
                    {
                        dgv.ClmnFltrChngd(null, null);
                    }

                }
                catch (InvalidExpressionException ex)
                {
                    throw new NotSupportedException(ex.Message);
                }
                AutoFixLockedColumns();
                return;
            }

            String newColumnFilter = null;

            var columnProperty =OwningColumn.DataPropertyName.Replace("]", @"\]");
            if (FilterValueList.Count == 1)
            {
                switch (FilterValueList[0])
                {
                    case "(Blanks)":
                        newColumnFilter = String.Format(
                            "LEN(ISNULL(CONVERT([{0}],'System.String'),''))=0",
                            columnProperty);
                        break;
                    case "(NonBlanks)":
                        newColumnFilter = String.Format(
                            "LEN(ISNULL(CONVERT([{0}],'System.String'),''))>0",
                            columnProperty);

                        break;
                    case "(Custom...)":
                        var customFilter = new CustomFilter(OwningColumn.ValueType,
                                                            OwningColumn.HeaderText);
                        customFilter.CustomFilterType = filterType;
                        var dv = DataGridView as DataGridViewPlus;
                        if (dv != null)
                        {
                            if (dv.preCustomFilterColName != OwningColumn.Name)
                            {
                                dv.preCustomFilterColName = OwningColumn.Name;
                            }
                            else
                            {
                                customFilter.Filter = dv.preCstmFltrVl;
                                customFilter.CustomFilterType = dv.preCustomFilterType;
                            }
                            if (customFilter.ShowDialog(DataGridView.FindForm()) == DialogResult.OK)
                            {
                                newColumnFilter = customFilter.FilterFormatString;
                                dv.preCustomFilterType = customFilter.CustomFilterType;
                                dv.preCstmFltrVl = customFilter.Filter;
                            }
                            else
                            {
                                AutoFixLockedColumns();
                                return;
                            }
                        }
                        break;
                    default:
                        try
                        {
                            if (filters.Contains(FilterValueList[0]))
                            {
                                newColumnFilter = String.Format("[{0}]='{1}'",
                                    columnProperty,
                                    (filters[FilterValueList[0]].ToString())
                                    .Replace("'", "''"));
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        break;
                }
            }
            else
            {
                try
                {
                    // 生成过滤字符串
                    var sb = new StringBuilder();
                    foreach(var val in FilterValueList)
                    {
                        sb.Append(string.Format("'{0}',", val.Replace("'", "''")));
                    }
                    sb.Length--;
                    newColumnFilter = String.Format("[{0}] IN ({1})", columnProperty, sb);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            var newFilter = FilterWithoutCurrentColumn(data.Filter);
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

            try
            {
                data.Filter = newFilter;
                var dgv = DataGridView as DataGridViewPlus;
                if (dgv != null)
                {
                    dgv.ClmnFltrChngd(null, null);
                }

            }
            catch (InvalidExpressionException ex)
            {
                throw new NotSupportedException(
                    "Invalid expression: " + newFilter, ex);
            }

            IsFiltered = true;
            CurrentColumnFilter = newColumnFilter;
        }
        public void AutoFixLockedColumns()
        {
            
        }
        public static void RemoveFilter(DataGridView dataGridView)
        {
            if (dataGridView == null)
            {
                throw new ArgumentNullException("dataGridView");
            }

            var data = dataGridView.DataSource as BindingSource;

            if (data == null ||
                data.DataSource == null ||
                !data.SupportsFiltering)
            {
                throw new ArgumentException("The DataSource property of the " +
                    "specified DataGridView is not set to a BindingSource " +
                    "with a SupportsFiltering property value of true.");
            }

            if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.IsNewRow)
            {
                dataGridView.CurrentCell = null;
            }

            data.Filter = null;
        }
        public static String GetFilterStatus(DataGridView dataGridView)
        {
            if (dataGridView == null)
            {
                throw new ArgumentNullException("dataGridView");
            }

            var data = dataGridView.DataSource as BindingSource;

            if (data == null || String.IsNullOrEmpty(data.Filter) ||
                data.DataSource == null ||
                !data.SupportsFiltering)
            {
                return String.Empty;
            }

            var currentRowCount = data.Count;

            data.RaiseListChangedEvents = false;
            var oldFilter = data.Filter;
            data.Filter = null;
            var unfilteredRowCount = data.Count;
            data.Filter = oldFilter;
            data.RaiseListChangedEvents = true;

            Debug.Assert(currentRowCount <= unfilteredRowCount,
                "current count is greater than unfiltered count");

            if (currentRowCount == unfilteredRowCount)
            {
                return String.Empty;
            }
            return String.Format("{0} of {1} records found",
                currentRowCount, unfilteredRowCount);
        }

        private Rectangle dropDownButtonBoundsValue = Rectangle.Empty;

        private Rectangle DropDownButtonBounds
        {
            get
            {
                if (!FilteringEnabled)
                {
                    return Rectangle.Empty;
                }
                if (dropDownButtonBoundsValue == Rectangle.Empty)
                {
                    SetDropDownButtonBounds();
                }
                return dropDownButtonBoundsValue;
            }
        }

        private void InvalidateDropDownButtonBounds()
        {
            if (!dropDownButtonBoundsValue.IsEmpty)
            {
                dropDownButtonBoundsValue = Rectangle.Empty;
            }
        }
        private void SetDropDownButtonBounds()
        {
            var cellBounds =
                DataGridView.GetCellDisplayRectangle(
                ColumnIndex, -1, false);

            var buttonEdgeLength = InheritedStyle.Font.Height + 5;
            var borderRect = BorderWidths(
                DataGridView.AdjustColumnHeaderBorderStyle(
                DataGridView.AdvancedColumnHeadersBorderStyle,
                new DataGridViewAdvancedBorderStyle(), false, false));
            var borderAndPaddingHeight = 2 +
                borderRect.Top + borderRect.Height +
                InheritedStyle.Padding.Vertical;
            var visualStylesEnabled =
                Application.RenderWithVisualStyles &&
                DataGridView.EnableHeadersVisualStyles;
            if (visualStylesEnabled)
            {
                borderAndPaddingHeight += 3;
            }

            if (buttonEdgeLength >
                DataGridView.ColumnHeadersHeight -
                borderAndPaddingHeight)
            {
                buttonEdgeLength =
                    DataGridView.ColumnHeadersHeight -
                    borderAndPaddingHeight;
            }

            if (buttonEdgeLength > cellBounds.Width - 3)
            {
                buttonEdgeLength = cellBounds.Width - 3;
            }

            var topOffset = visualStylesEnabled ? 4 : 1;
            var top = cellBounds.Bottom - buttonEdgeLength - topOffset;
            var leftOffset = visualStylesEnabled ? 3 : 6;
            var left = 0;
            if (DataGridView.RightToLeft == RightToLeft.No)
            {
                left = cellBounds.Right - buttonEdgeLength - leftOffset;
            }
            else
            {
                left = cellBounds.Left + leftOffset;
            }

            dropDownButtonBoundsValue = new Rectangle(left, top,
                buttonEdgeLength, buttonEdgeLength);
            AdjustPadding(buttonEdgeLength + leftOffset);
        }

        private void AdjustPadding(Int32 newDropDownButtonPaddingOffset)
        {
            var widthChange = newDropDownButtonPaddingOffset -
                currentDropDownButtonPaddingOffset;

            if (widthChange != 0)
            {
                currentDropDownButtonPaddingOffset =
                    newDropDownButtonPaddingOffset;
                var dropDownPadding = new Padding(0, 0, widthChange, 0);
                Style.Padding = Padding.Add(
                    InheritedStyle.Padding, dropDownPadding);
            }
        }

        private Int32 currentDropDownButtonPaddingOffset;

        private Boolean _filteringEnabledValue = true;

        [DefaultValue(true)]
        public Boolean FilteringEnabled
        {
            get
            {
                if (DataGridView == null ||
                    DataGridView.DataSource == null)
                {
                    return _filteringEnabledValue;
                }

                var data = DataGridView.DataSource as BindingSource;
                if (data != null)
                {
                    return _filteringEnabledValue && data.SupportsFiltering;
                }
                else
                {
                    return _filteringEnabledValue;
                }
            }
            set
            {
                if (!value)
                {
                    AdjustPadding(0);
                    InvalidateDropDownButtonBounds();
                }

                _filteringEnabledValue = value;
            }
        }

        private Boolean automaticSortingEnabledValue = true;

        [DefaultValue(true)]
        public Boolean AutomaticSortingEnabled
        {
            get
            {
                return automaticSortingEnabledValue;
            }
            set
            {
                automaticSortingEnabledValue = value;
                if (OwningColumn != null)
                {
                    if (value)
                    {
                        OwningColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
                    }
                    else
                    {
                        OwningColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                }
            }
        }

        private Int32 _dropDownListBoxMaxLinesValue = 20;

        [DefaultValue(20)]
        public Int32 DropDownListBoxMaxLines
        {
            get { return _dropDownListBoxMaxLinesValue; }
            set { _dropDownListBoxMaxLinesValue = value; }
        }


        private ExternedVisible columnVisible = ExternedVisible.Shown;

        public ExternedVisible ColumnVisible
        {
            get { return columnVisible; }
            set { 
                columnVisible = value;
                if (value == ExternedVisible.Hide)
                {
                    OwningColumn.Visible = false;
                }
                else if (value == ExternedVisible.Shown)
                {
                    OwningColumn.Visible = true;
                }
                else if (value == ExternedVisible.NeverShown)
                {
                    OwningColumn.Visible = false;
                }
            }
        }
    }
    public enum ExternedVisible
    {
        NeverShown,
        Shown,
        Hide,
    }
    public enum EnumFilterMode
    {
        And,
        Or
    }
    public class PopulateFiltersResult
    {
        public string Desc = string.Empty;
        public string Whole = string.Empty;
        public string Shown = string.Empty;
    }
}
