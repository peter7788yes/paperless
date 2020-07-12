using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaperLess_Emeeting.App_Code.BaseInterface
{
    interface IAsyncInitUwerControl
    {
        void InitSelectDB();

        void InitEvent();

        void InitUI();
    }
}
