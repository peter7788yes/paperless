namespace PaperLess_ViewModel
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class SignOut
    {

        private SignOutReception receptionField;

        private object errorMsgField;

        /// <remarks/>
        public SignOutReception Reception
        {
            get
            {
                return this.receptionField;
            }
            set
            {
                this.receptionField = value;
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
    public partial class SignOutReception
    {

        private string statusField;

      

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

    }

}