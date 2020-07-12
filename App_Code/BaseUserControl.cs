//using PaperLess_Emeeting.App_Code.BaseInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace PaperLess_Emeeting.App_Code.BaseClass
{
    public delegate void ChildUC_InitSelectDB_Function();
    public delegate void ChildUC_InitUI_Function();
    public delegate void ChildUC_InitEvent_Function();

    public class BaseUserControl : UserControl //, IAsyncInitUwerControl
    {
        public event ChildUC_InitSelectDB_Function ChildUserControl_InitSelectDB_Event;
        public event ChildUC_InitUI_Function  ChildUserControl_InitUI_Event;
        public event ChildUC_InitEvent_Function ChildUserControl_InitEvent_Event;

        public System.Windows.Window ParentWindow { get; set; }

        public BaseUserControl()
        {
            MouseTool.ShowLoding();
            this.Loaded += BaseUserControl_Loaded;
        }


        private void BaseUserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ChildUserControl_InitSelectDB_Event();
            Dispatcher.BeginInvoke(ChildUserControl_InitUI_Event);
            ChildUserControl_InitEvent_Event();
            MouseTool.ShowArrow();
        }
       
    }
}
