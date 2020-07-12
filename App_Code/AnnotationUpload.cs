namespace PaperLess_ViewModel
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class AnnotationUpload
    {
        //錯誤訊息
        private object errorMsgField;

       
      
        /// <remarks/>
        public object ErrorMsg
        {
            get
            {
                return this.errorMsgField;
            }
            set
            {
                this.errorMsgField = value;
            }
        }
    }

   

}