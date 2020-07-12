namespace PaperLess_ViewModel
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class SigninDataUpload
    {

        private SigninDataUploadFile fileField;

        private object errorMsgField;

        /// <remarks/>
        public SigninDataUploadFile File
        {
            get
            {
                return this.fileField;
            }
            set
            {
                this.fileField = value;
            }
        }

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

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SigninDataUploadFile
    {

        private string statusField;

        private string signedPicField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SignedPic
        {
            get
            {
                return this.signedPicField;
            }
            set
            {
                this.signedPicField = value;
            }
        }
    }

}