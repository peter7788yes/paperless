﻿
/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class MeetingData
{

    private MeetingDataLoginResult loginResultField;

    private string subjectField;

    private string meetingsTitleField;

    private MeetingDataAgenda[] agendaListField;

    private MeetingDataMeetingsFile meetingsFileField;

    private MeetingDataDownloadFile downloadFileField;

    private string stateField;

    private string idField;

    private string nameField;

    private string beginTimeField;

    private string endTimeField;

    private string pinCodeField;

    private string typeField;

    private string locationField;

    private string capacityField;

    private string ipField;

    private string syncIPField;

    private string syncPortField;

    private string seriesMeetingIDField;

    private string statusField;

    private string watermarkField;

    /// <remarks/>
    public MeetingDataLoginResult LoginResult
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
    public string Subject
    {
        get
        {
            return this.subjectField;
        }
        set
        {
            this.subjectField = value;
        }
    }

    /// <remarks/>
    public string MeetingsTitle
    {
        get
        {
            return this.meetingsTitleField;
        }
        set
        {
            this.meetingsTitleField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("Agenda", IsNullable = false)]
    public MeetingDataAgenda[] AgendaList
    {
        get
        {
            return this.agendaListField;
        }
        set
        {
            this.agendaListField = value;
        }
    }

    /// <remarks/>
    public MeetingDataMeetingsFile MeetingsFile
    {
        get
        {
            return this.meetingsFileField;
        }
        set
        {
            this.meetingsFileField = value;
        }
    }

    /// <remarks/>
    public MeetingDataDownloadFile DownloadFile
    {
        get
        {
            return this.downloadFileField;
        }
        set
        {
            this.downloadFileField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string State
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
    public string PinCode
    {
        get
        {
            return this.pinCodeField;
        }
        set
        {
            this.pinCodeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Type
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
    public string Capacity
    {
        get
        {
            return this.capacityField;
        }
        set
        {
            this.capacityField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string IP
    {
        get
        {
            return this.ipField;
        }
        set
        {
            this.ipField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string syncIP
    {
        get
        {
            return this.syncIPField;
        }
        set
        {
            this.syncIPField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string syncPort
    {
        get
        {
            return this.syncPortField;
        }
        set
        {
            this.syncPortField = value;
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
    public string status
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
    public string watermark
    {
        get
        {
            return this.watermarkField;
        }
        set
        {
            this.watermarkField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataLoginResult
{

    private MeetingDataLoginResultLoginState loginStateField;

    private MeetingDataLoginResultButton[] enableButtonListField;

    /// <remarks/>
    public MeetingDataLoginResultLoginState LoginState
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
    public MeetingDataLoginResultButton[] EnableButtonList
    {
        get
        {
            return this.enableButtonListField;
        }
        set
        {
            this.enableButtonListField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataLoginResultLoginState
{

    private string idField;

    private string nameField;

    private string emailField;

    private string typeField;

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
    public string Type
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
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataLoginResultButton
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
public partial class MeetingDataAgenda
{

    private string idField;

    private string parentIDField;

    private string agendaField;

    private string captionField;

    private string proposalUnitField;

    private string progressField;

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
    public string ParentID
    {
        get
        {
            return this.parentIDField;
        }
        set
        {
            this.parentIDField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Agenda
    {
        get
        {
            return this.agendaField;
        }
        set
        {
            this.agendaField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Caption
    {
        get
        {
            return this.captionField;
        }
        set
        {
            this.captionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string ProposalUnit
    {
        get
        {
            return this.proposalUnitField;
        }
        set
        {
            this.proposalUnitField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Progress
    {
        get
        {
            return this.progressField;
        }
        set
        {
            this.progressField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataMeetingsFile
{

    private MeetingDataMeetingsFileFile[] fileListField;

    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("File", IsNullable = false)]
    public MeetingDataMeetingsFileFile[] FileList
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
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataMeetingsFileFile
{

    private string idField;

    private string urlField;

    private string fileNameField;

    private string versionField;

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

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string FileName
    {
        get
        {
            return this.fileNameField;
        }
        set
        {
            this.fileNameField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string version
    {
        get
        {
            return this.versionField;
        }
        set
        {
            this.versionField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataDownloadFile
{

    private MeetingDataDownloadFileDownloadTime downloadTimeField;

    private MeetingDataDownloadFileBrowseTime browseTimeField;

    private MeetingDataDownloadFileFile[] downloadFileListField;

    /// <remarks/>
    public MeetingDataDownloadFileDownloadTime DownloadTime
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
    public MeetingDataDownloadFileBrowseTime BrowseTime
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
    [System.Xml.Serialization.XmlArrayItemAttribute("File", IsNullable = false)]
    public MeetingDataDownloadFileFile[] DownloadFileList
    {
        get
        {
            return this.downloadFileListField;
        }
        set
        {
            this.downloadFileListField = value;
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MeetingDataDownloadFileDownloadTime
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
public partial class MeetingDataDownloadFileBrowseTime
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
public partial class MeetingDataDownloadFileFile
{

    private string fileNameField;

    private string urlField;

    private string idField;

    private string agendaIDField;

    private string versionField;

    private string folderIDField;

    private string encryptionKeyField;

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string FileName
    {
        get
        {
            return this.fileNameField;
        }
        set
        {
            this.fileNameField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string FolderID
    {
        get
        {
            return this.folderIDField;
        }
        set
        {
            this.folderIDField = value;
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
    public string AgendaID
    {
        get
        {
            return this.agendaIDField;
        }
        set
        {
            this.agendaIDField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string version
    {
        get
        {
            return this.versionField;
        }
        set
        {
            this.versionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string EncryptionKey
    {
        get
        {
            return this.encryptionKeyField;
        }
        set
        {
            this.encryptionKeyField = value;
        }
    }
}

