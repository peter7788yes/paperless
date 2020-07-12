
/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class FolderData
{

    private FolderDataLoginResult loginResultField;

    private FolderDataFolderList folderListField;

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
    public FolderDataFolderList FolderList
    {
        get
        {
            return this.folderListField;
        }
        set
        {
            this.folderListField = value;
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
public partial class FolderDataFolderList
{

    private FolderDataFolderListFolder[] folderField;

    private int countField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Folder")]
    public FolderDataFolderListFolder[] Folder
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

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public int Count
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
public partial class FolderDataFolderListFolder
{

    private string idField;

    private string nameField;

    private ulong addTimeField;

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
    public ulong AddTime
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
}

