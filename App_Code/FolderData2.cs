namespace XML2
{

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class FolderData
    {

        private FolderDataLoginResult loginResultField;

        private FolderDataFolder folderField;

        /// <remarks/>
        public FolderDataLoginResult LoginResult
        {
            get
            {
                return this.loginResultField;
            }
            set
            {
                this.loginResultField = value;
            }
        }

        /// <remarks/>
        public FolderDataFolder Folder
        {
            get
            {
                return this.folderField;
            }
            set
            {
                this.folderField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataLoginResult
    {

        private FolderDataLoginResultLoginState loginStateField;

        private FolderDataLoginResultButton[] buttonListField;

        /// <remarks/>
        public FolderDataLoginResultLoginState LoginState
        {
            get
            {
                return this.loginStateField;
            }
            set
            {
                this.loginStateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Button", IsNullable = false)]
        public FolderDataLoginResultButton[] ButtonList
        {
            get
            {
                return this.buttonListField;
            }
            set
            {
                this.buttonListField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataLoginResultLoginState
    {

        private string idField;

        private string nameField;

        private string emailField;

        private int stateField;

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
        public string Email
        {
            get
            {
                return this.emailField;
            }
            set
            {
                this.emailField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int State
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataLoginResultButton
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
    public partial class FolderDataFolder
    {

        private FolderDataFolderFileList fileListField;

        private string idField;

        private string nameField;

        /// <remarks/>
        public FolderDataFolderFileList FileList
        {
            get
            {
                return this.fileListField;
            }
            set
            {
                this.fileListField = value;
            }
        }

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
    public partial class FolderDataFolderFileList
    {

        private FolderDataFolderFileListFile[] fileField;

        private string countField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("File")]
        public FolderDataFolderFileListFile[] File
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Count
        {
            get
            {
                return this.countField;
            }
            set
            {
                this.countField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataFolderFileListFile
    {

        private FolderDataFolderFileListFileMeeting meetingField;

        private FolderDataFolderFileListFileDownloadTime downloadTimeField;

        private FolderDataFolderFileListFileBrowseTime browseTimeField;

        private string idField;

        private string nameField;

        private string addTimeField;

        private int addVersionField;

        private int currentVersionField;

        private string urlField;

        /// <remarks/>
        public FolderDataFolderFileListFileMeeting Meeting
        {
            get
            {
                return this.meetingField;
            }
            set
            {
                this.meetingField = value;
            }
        }

        /// <remarks/>
        public FolderDataFolderFileListFileDownloadTime DownloadTime
        {
            get
            {
                return this.downloadTimeField;
            }
            set
            {
                this.downloadTimeField = value;
            }
        }

        /// <remarks/>
        public FolderDataFolderFileListFileBrowseTime BrowseTime
        {
            get
            {
                return this.browseTimeField;
            }
            set
            {
                this.browseTimeField = value;
            }
        }

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
        public string AddTime
        {
            get
            {
                return this.addTimeField;
            }
            set
            {
                this.addTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int AddVersion
        {
            get
            {
                return this.addVersionField;
            }
            set
            {
                this.addVersionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int CurrentVersion
        {
            get
            {
                return this.currentVersionField;
            }
            set
            {
                this.currentVersionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataFolderFileListFileMeeting
    {

        private string idField;

        private string nameField;

        private int typeField;

        private string beginTimeField;

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
        public int Type
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
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataFolderFileListFileDownloadTime
    {

        private string beginTimeField;

        private string endTimeField;

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
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FolderDataFolderFileListFileBrowseTime
    {

        private string beginTimeField;

        private string endTimeField;

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
    }


}