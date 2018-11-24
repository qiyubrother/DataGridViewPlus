using System;
using System.Windows.Forms;

namespace Qiyubrother
{
    public partial class DataGridViewPlus
    {
        private bool _bAllowUserToFilter;
        private bool _bLockedChanged;
        public bool AllowUserToFilter
        {
            get { return _bAllowUserToFilter; }
            set
            {
                _bAllowUserToFilter = value;
                foreach (DataGridViewColumn col in Columns)
                {
                    col.HeaderCell = new DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    var dgvHc = col.HeaderCell as DataGridViewAutoFilterColumnHeaderCell;
                    dgvHc.FilteringEnabled = value;
                }
            }
        }
        public bool IsActive{get;set;}
        public bool Locked 
        { 
            get { return _bLockedChanged; } 
            set {
 
                _bLockedChanged = value;
            }
        }
        public static string DataTimeFormat { get; set; }
        public bool IsEnableContentMenu{get;set;}
        public bool IsAutoSaveLayout{get;set;}
        public string Description{get;set;}
        public ContextMenuStrip Menu
        {
            get { return cmsMenu; }
            set { cmsMenu = value; }
        }
    }
}
