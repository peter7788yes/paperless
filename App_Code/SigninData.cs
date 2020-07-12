/// <remarks/>
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class SigninData
{

    private string meetingIDField;

    private string userIDField;

    /// <remarks/>
    public string MeetingID
    {
        get
        {
            return this.meetingIDField;
        }
        set
        {
            this.meetingIDField = value;
        }
    }
}

