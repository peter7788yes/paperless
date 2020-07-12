namespace PaperLess_ViewModel
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class SeriesData
    {

        private SeriesDataSeriesMeeting[] seriesMeetingField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("SeriesMeeting")]
        public SeriesDataSeriesMeeting[] SeriesMeeting
        {
            get
            {
                return this.seriesMeetingField;
            }
            set
            {
                this.seriesMeetingField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SeriesDataSeriesMeeting
    {

        private SeriesDataSeriesMeetingSeries seriesField;

        private SeriesDataSeriesMeetingMeeting[] meetingListField;

        /// <remarks/>
        public SeriesDataSeriesMeetingSeries Series
        {
            get
            {
                return this.seriesField;
            }
            set
            {
                this.seriesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Meeting", IsNullable = false)]
        public SeriesDataSeriesMeetingMeeting[] MeetingList
        {
            get
            {
                return this.meetingListField;
            }
            set
            {
                this.meetingListField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SeriesDataSeriesMeetingSeries
    {

        private string idField;

        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SeriesDataSeriesMeetingMeeting
    {

        private string idField;

        private string nameField;

        private string beginTimeField;

        private string endTimeField;

        private string locationField;

        private string pincodeField;

        private string typeField;

        private string seriesMeetingIDField;

        private string isBrowserdField;

        private string isDownloadField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string BeginTime
        {
            get
            {
                return this.beginTimeField;
            }
            set
            {
                this.beginTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string EndTime
        {
            get
            {
                return this.endTimeField;
            }
            set
            {
                this.endTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Location
        {
            get
            {
                return this.locationField;
            }
            set
            {
                this.locationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string pincode
        {
            get
            {
                return this.pincodeField;
            }
            set
            {
                this.pincodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SeriesMeetingID
        {
            get
            {
                return this.seriesMeetingIDField;
            }
            set
            {
                this.seriesMeetingIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string isBrowserd
        {
            get
            {
                return this.isBrowserdField;
            }
            set
            {
                this.isBrowserdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string isDownload
        {
            get
            {
                return this.isDownloadField;
            }
            set
            {
                this.isDownloadField = value;
            }
        }
    }

}