using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DocumentVM : INotifyPropertyChanged
{
    private string _FileIcon;
    public string FileIcon
    {
        get { return _FileIcon; }
        set
        {
            if (value.Equals(_FileIcon)==false)
            {
                _FileIcon = value;
                OnPropertyChanged("FileIcon");
            }
        }
    }

    private string _FileName;
    public string FileName
    {
        get { return _FileName; }
        set
        {
            if (value.Equals(_FileName) == false)
            {
                _FileName = value;
                OnPropertyChanged("FileName");
            }
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}
