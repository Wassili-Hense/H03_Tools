using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class Pin : INotifyPropertyChanged {
    public Pin(XElement info) {
      this.name = info.Attribute("name").Value;
      var xn = info.Attribute("pin");
      nr = xn == null ? string.Empty : xn.Value;
    }

    public string name { get; private set; }
    public string nr { get; private set; }

    public override string ToString() {
      return string.IsNullOrEmpty(nr) ? name : (name + "[" + nr + "]");
    }
    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    protected void PropertyChangedReise([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "") {
      if(PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
    #endregion INotifyPropertyChanged Members

  }
}
