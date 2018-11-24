using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data;
namespace Qiyubrother
{
    public static class Extend_DataGridView
    {
        public static void CommitEdit(this DataGridView dgv)
        {
            if (dgv != null && dgv.DataSource != null)
            {
                dgv.EndEdit(DataGridViewDataErrorContexts.Commit);
                var f = dgv.FindForm();
                if (f != null)
                {
                    f.BindingContext[dgv.DataSource].EndCurrentEdit();
                }
            }
        }
        public static void AutoSetActiveCell(this DataGridView dgv)
        {
            int colIndex = -1;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Visible)
                {
                    colIndex = col.Index;
                    break;
                }
            }
            if (colIndex >= 0 && dgv.Rows.Count > 0)
            {
                dgv.CurrentCell = dgv.Rows[0].Cells[colIndex];
            }
        }
        public static void AutoToEndRow(this DataGridView dgv)
        {
            if (dgv.Rows.Count > 0)
            {
                var colIndex = -1;
                foreach(DataGridViewColumn dgvc in dgv.Columns)
                {
                    if (dgvc.Visible)
                    {
                        colIndex = dgvc.DisplayIndex;
                        break;
                    }
                }
                if (colIndex >= 0)
                {
                    dgv.CurrentCell = dgv.Rows[dgv.Rows.Count - 1].Cells[colIndex];
                    dgv.CurrentCell.Selected = true;
                }
            }
        }

        public static void AutoExtendLastColumn(this DataGridView dgv, params string[] ignoreColumns)
        {
            DataGridViewColumn lastCol = null;
            foreach (DataGridViewColumn dgvc in dgv.Columns)
            {
                if (dgvc.Visible)
                {
                    var isExistIgnoreColumn = false;
                    if (ignoreColumns.Length > 0)
                    {
                        var dgvcName = dgvc.Name.ToLower();
                        foreach (var col in ignoreColumns)
                        {
                            if (dgvcName == col.ToLower())
                            {
                                isExistIgnoreColumn = true;
                                break;
                            }
                        }
                        if (isExistIgnoreColumn)
                        {
                            continue;
                        }
                    }
                    lastCol = dgvc;
                }
            }
            if (lastCol != null)
            {
                lastCol.MinimumWidth = 80;
                lastCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }
    }
    public static class Extend_DataTable
    {
        public static void CommitEdit(this DataTable dt, Form owner)
        {
            var f = owner;
            if (f != null)
            {
                f.BindingContext[dt].EndCurrentEdit();
            }
        }
        public static DataTable DeepClone(this DataTable dt)
        {
        	var d = dt.Clone();
        	foreach(DataRow dr in dt.Rows)
        	{
        		var r = d.NewRow();
        		foreach(DataColumn dc in dt.Columns)
        		{
        			r[dc.ColumnName] = dr[dc.ColumnName];
        		}
        		d.Rows.Add(r);
        	}
        	
        	return d;
        }
        public static DataTable ReomveColumns(this DataTable dt, params string[] columns)
        {
        	foreach(var col in columns)
        	{
        		if (dt.Columns.Contains(col))
        		{
        			dt.Columns.Remove(col);
        		}
        	}
        	return dt;
        }
        public static IEnumerable<string> GetColumnNameList(this DataGridViewColumnCollection dgv)
        {
            return (from DataGridViewColumn col in dgv select col.Name).ToList();
        }
    }
}
