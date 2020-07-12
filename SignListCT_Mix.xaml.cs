using PaperLess_Emeeting.App_Code.MessageBox;
using PaperLess_ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PaperLess_Emeeting
{
    
   

    /// <summary>
    /// SignListCT.xaml 的互動邏輯
    /// </summary>
    public partial class SignListCT_Mix : UserControl
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserPWD { get; set; }
        public string MeetingID { get; set; }
        public DateTime BeginTime = new DateTime(2010, 1, 1);
        public DateTime EndTime = new DateTime(2050, 1, 1);
        public MeetingUserType meetingUserType =MeetingUserType.與會人員;
        
        public SignListCT_Mix()
        {
            MouseTool.ShowLoading();
            InitializeComponent();
            this.Loaded += SignListCT_Mix_Loaded;
            //MouseTool.ShowArrow();
        }

        private void SignListCT_Mix_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                InitSelectDB();
                // 只要是 CT 主要畫面，優先權設定為Send，因為設定Normal，按鈕的出現會感覺卡卡的。
                this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    try
                    {
                        InitUI();
                        InitEvent();
                    }
                    catch (Exception ex)
                    {
                        LogTool.Debug(ex);
                    }
                    MouseTool.ShowArrow();
                }));
               
            });
          
          
        }

        private void InitEvent()
        {
            //txtIsSigned.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            //txtIsSigned.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            //txtIsSigned.MouseLeftButtonDown += txtIsSigned_MouseLeftButtonDown;

            btnIndex.MouseEnter += (sender, e) => { MouseTool.ShowHand();}; //ClearBorderColor(); btnIndex.BorderBrush = Brushes.Gray; };
            btnIndex.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnIndex.MouseLeftButtonDown +=(sender, e) =>
            {
                ClearButtonColor();
                this.btnIndex.Background = ColorTool.HexColorToBrush("#019fde");
                this.txtIndex.Foreground = Brushes.White;
                ChangeSignRow_ByOrder(SignListCT_Order.序號);
            };

            btnDept.MouseEnter += (sender, e) => { MouseTool.ShowHand();}; //ClearBorderColor(); btnDept.BorderBrush = Brushes.Gray; };
            btnDept.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnDept.MouseLeftButtonDown +=(sender, e) =>
            {
                ClearButtonColor();
                this.btnDept.Background = ColorTool.HexColorToBrush("#019fde");
                this.txtDept.Foreground = Brushes.White;
                ChangeSignRow_ByOrder(SignListCT_Order.機關單位);
            };
            btnIsSigned.MouseEnter += (sender, e) => { MouseTool.ShowHand();}; //ClearBorderColor(); btnIsSigned.BorderBrush = Brushes.Gray; };
            btnIsSigned.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnIsSigned.MouseLeftButtonDown += (sender, e) =>
            {
                ClearButtonColor();
                this.btnIsSigned.Background = ColorTool.HexColorToBrush("#019fde");
                this.txtIsSigned.Foreground = Brushes.White;
                ChangeSignRow_ByOrder(SignListCT_Order.是否簽到);
            };


            txtKeyword.MouseEnter += (sender, e) => { MouseTool.ShowIBeam(); txtKeyword.Focus(); };
            txtKeyword.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); }; //Keyboard.ClearFocus();
            txtKeyword.KeyUp += txtKeyword_KeyUp;
            txtKeyword.Focus();

            btnAddUser.MouseEnter += (sender, e) => { MouseTool.ShowHand(); };
            btnAddUser.MouseLeave += (sender, e) => { MouseTool.ShowArrow(); };
            btnAddUser.MouseLeftButtonDown += (sender, e) =>
            {
                Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
                if (Home_Window != null)
                {
                    Home_Window.CC.Content = new SignPadCT("guest", "來賓","","", (x,y) => { Home_Window.CC.Content = new SignListCT_Mix(); });
                }
            };
        }

        //private void txtIsSigned_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    string AllowIpRange = "";
        //    DataTable dt = MSCE.GetDataTable("select AllowIpRange from NowLogin");
        //    if (dt.Rows.Count > 0)
        //    {
        //        AllowIpRange = dt.Rows[0]["AllowIpRange"].ToString();
        //    }
        //    if (AllowIpRange.Equals("") == false && IpTool.CheckInNowWifi(AllowIpRange) == false)
        //    {
        //        AutoClosingMessageBox.Show("您不在會議室範圍內，無法使用此功能");
        //        return;
        //    }


        //    Home Home_Window = Application.Current.Windows.OfType<Home>().FirstOrDefault();
        //    if (Home_Window != null)
        //    {
        //        Home_Window.Content = new SignPadCT(UserID, UserName, "","", (x, y) => { Home_Window.Content = new SignListCT_Mix(); });
        //    }
        //}

        private void txtKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            CallSearch();
        }

        private void CallSearch()
        {
            string keyword = txtKeyword.Text.ToLower().Trim();

            if (keyword.Equals("") == false)
            {
                foreach (SignRow_Mix x in SignRowSP.Children.OfType<SignRow_Mix>())
                {
                    if (x.txtIndex.Text.ToLower().Contains(keyword) == true
                         || x.txtAgent.Text.ToLower().Contains(keyword) == true
                         || x.txtDept.Text.ToLower().Contains(keyword) == true
                         || x.txtTitle.Text.ToLower().Contains(keyword) == true
                         || x.txtUserName.Text.ToLower().Contains(keyword) == true)
                    {
                        x.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        x.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                //LawRowSP.Children.OfType<LawRow>().ToList().ForEach(x =>x.Visibility=Visibility.Visible );

                foreach (SignRow_Mix x in SignRowSP.Children.OfType<SignRow_Mix>())
                {
                    x.Visibility = Visibility.Visible;
                };
            }
        }

        private void ChangeSignRow_ByOrder(SignListCT_Order signListCT_Order)
        {
           
           List<SignRow_Mix> SignRowS = SignRowSP.Children.OfType<SignRow_Mix>().ToList();
           SignRowSP.Children.Clear();
           switch (signListCT_Order)
           {
                case SignListCT_Order.序號:
                   //MessageBox.Show(SignRowS.Count().ToString());
                   SignRowS = SignRowS.OrderBy(x=>int.Parse(x.signinDataUser.Rank)).ToList();
                   //MessageBox.Show(SignRowS.Count().ToString());
                   break;
                case SignListCT_Order.機關單位:
                   SignRowS = SignRowS.OrderBy(x => x.signinDataUser.Dept).ToList();
                   break;
                case SignListCT_Order.是否簽到:
                   SignRowS = SignRowS.OrderBy(x => x.signinDataUser.IsSigned).ToList();
                   break;
           }


           if (SignRowS != null)
           {
               Task.Factory.StartNew(() =>
                   {
                       this.Dispatcher.BeginInvoke(new Action(()=>
                           {
                                   foreach (SignRow_Mix item in SignRowS)
                                   {
                                       SignRowSP.Children.Add(item);
                                   }
                           }));
                   });
           }

           // 二選一
           //CallSearch();
           txtKeyword.Text = "";
           txtKeyword.Focus();
        }

        private void ClearButtonColor()
        {
            this.btnIndex.Background = ColorTool.HexColorToBrush("#D3Dce0");
            this.btnDept.Background = ColorTool.HexColorToBrush("#D3Dce0");
            this.btnIsSigned.Background = ColorTool.HexColorToBrush("#D3Dce0");

            this.txtIndex.Foreground = Brushes.Black;
            this.txtDept.Foreground = Brushes.Black;
            this.txtIsSigned.Foreground = Brushes.Black;
        }

        private void ClearBorderColor()
        {
            this.btnIndex.BorderBrush = ColorTool.HexColorToBrush("#5F879B");
            this.btnDept.BorderBrush = ColorTool.HexColorToBrush("#5F879B");
            this.btnIsSigned.BorderBrush = ColorTool.HexColorToBrush("#5F879B");
        }

        private void InitSelectDB()
        {
           
          
            DataTable dt = MSCE.GetDataTable("select UserID,UserName,UserPWD,MeetingID,MeetingBeginTime,MeetingEndTime,MeetingUserType from NowLogin");
            if (dt.Rows.Count > 0)
            {
                UserID = dt.Rows[0]["UserID"].ToString();
                UserName = dt.Rows[0]["UserName"].ToString();
                UserPWD = dt.Rows[0]["UserPWD"].ToString();
                MeetingID = dt.Rows[0]["MeetingID"].ToString();

                DateTime.TryParse(dt.Rows[0]["MeetingBeginTime"].ToString(), out BeginTime);
                DateTime.TryParse(dt.Rows[0]["MeetingEndTime"].ToString(), out EndTime);

                Enum.TryParse<MeetingUserType>(dt.Rows[0]["MeetingUserType"].ToString(), out meetingUserType);
            }
        }

        private void InitUI()
        {

            MouseTool.ShowLoading();

            switch (meetingUserType)
            {
                case MeetingUserType.議事管理人員:
                    CateBtnS.Visibility = Visibility.Visible;
                    SearchInput.Visibility = Visibility.Visible;
                    btnAddUser.Visibility = Visibility.Visible;
                    break;
                case MeetingUserType.與會人員:
                    break;
                case MeetingUserType.代理人:
                    break;
                case MeetingUserType.其它:
                    break;
            }
            //this.btnIndex.Background = ColorTool.HexColorToBrush("#019fde");
            //this.txtIndex.Foreground = Brushes.White;

            // 非同步POST方法
            GetSigninData.AsyncPOST(MeetingID, (sid) => { GetSigninData_DoAction(sid); });
                   //, (sid) => { this.Dispatcher.BeginInvoke(new Action<SigninData>(GetSigninData_DoAction), sid); });

            #region 同步POST
            //SigninData sid=GetSigninData.POST(MeetingID);
            //if (sid != null)
            //{
            //    foreach (SigninDataUser item in sid.UserList)
            //    {
            //        SignRowSP.Children.Add(new SignRow(item));
            //    }
            //}
            //else
            //{
            //    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
            //}
            #endregion
        }

        private void GetSigninData_DoAction(SigninData sid)
        {
              //先判斷是否要invoke
            if (this.Dispatcher.CheckAccess() == false)
            {
                // 這裡是下載事件處理，優先權設定為ContextIdle => 列舉值為 3。 幕後作業完成後，會處理作業。
                this.Dispatcher.BeginInvoke(new Action<SigninData>(GetSigninData_DoAction), sid);
            }
            else
            {
                if (sid != null)
                {
                     Task.Factory.StartNew(() =>
                       {
                           this.Dispatcher.BeginInvoke(new Action(() =>
                              {
                                  //int i = 0;
                                  foreach (SigninDataUser item in sid.UserList)
                                  {
                                      //i++;
                                      bool EnableTxtPLSSigned = false;
                                      if (DateTime.Now >= BeginTime.AddHours(-1) && DateTime.Now < EndTime.AddHours(1))
                                      {
                                          EnableTxtPLSSigned = true;
                                      }
                                      SignRowSP.Children.Add(new SignRow_Mix(item, meetingUserType, EnableTxtPLSSigned));
                                  }
                              }));
                       });
                }
                else
                {
                    AutoClosingMessageBox.Show("無法取得資料，請稍後再試");
                }

                MouseTool.ShowArrow();
            }
        }
    }
}
