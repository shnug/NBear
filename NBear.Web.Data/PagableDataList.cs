using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NBear.Web.Data
{
    public class PagableDataList : DataList
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            UpdateSelectArgumenets();      
        }

        private void UpdateSelectArgumenets()
        {
            //reset SelectArguments according to PageSize and PageIndex
            SelectArguments.StartRowIndex = 0;

            if (PageSize > 0)
            {
                SelectArguments.MaximumRows = PageSize;
                if (PageIndex > 1)
                {
                    SelectArguments.StartRowIndex = (PageIndex - 1) * PageSize;
                }
            }
            else
            {
                SelectArguments.MaximumRows = 0;
            }  
        }   

        [Category("Paging"), DefaultValue(0), Description("Page size.")]
        public int PageSize
        {
            get
            {
                return (ViewState["pageSize"] == null ? 0 : (int)ViewState["pageSize"]);
            }
            set
            {
                if (value > 0)
                {
                    ViewState["pageSize"] = value;
                }
                else
                {
                    ViewState["pageSize"] = 0;
                }
                UpdateSelectArgumenets();
            }
        }

        [Category("Paging"), DefaultValue(1), Description("Current page No.")]
        public int PageIndex
        {
            get
            {
                return (ViewState["pageIndex"] == null ? 1 : (int)ViewState["pageIndex"]);
            }
            set
            {
                if (value > 0)
                {
                    ViewState["pageIndex"] = value;
                }
                else
                {
                    ViewState["pageIndex"] = 1;
                }
                UpdateSelectArgumenets();
            }
        }
    }
}
