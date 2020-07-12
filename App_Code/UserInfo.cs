
/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class UserInfo
{

    private string userIDField;

    private string userPWField;

    private string userDeviceField;

    private string userDateBeginField;

    private string userDateEndField;

    /// <remarks/>
    public string UserID
    {
        get
        {
            return this.userIDField;
        }
        set
        {
            this.userIDField = value;
        }
    }

    /// <remarks/>
    public string UserPW
    {
        get
        {
            return this.userPWField;
        }
        set
        {
            this.userPWField = value;
        }
    }

    /// <remarks/>
    public string UserDevice
    {
        get
        {
            return this.userDeviceField;
        }
        set
        {
            this.userDeviceField = value;
        }
    }

    /// <remarks/>
    public string UserDateBegin
    {
        get
        {
            return this.userDateBeginField;
        }
        set
        {
            this.userDateBeginField = value;
        }
    }

    /// <remarks/>
    public string UserDateEnd
    {
        get
        {
            return this.userDateEndField;
        }
        set
        {
            this.userDateEndField = value;
        }
    }
}

