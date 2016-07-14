using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class Pin : INotifyPropertyChanged {
    public Pin(Project owner, XElement info, Port port) {
      this._owner = owner;
      this.port = port;
      if(port == null) {
        this.name = info.Attribute("name").Value;
      } else {
        this.nr = info.Attribute("fpin").Value;
        this.idx = int.Parse(info.Attribute("idx").Value);
        this.name = port.name + "." + idx.ToString("00");
      }
      List<enBase> entrys = new List<enBase>();
      enBase c;
      foreach(var el in info.Elements()) {
        c = null;
        switch(el.Name.LocalName) {
        case "dio":
          c = new enDIO(el, this);
          break;
        case "system":
          c = new enSystem(el, this);
          break;
        case "serial":
          c = new enSerial(el, this);
          break;
        }
        if(c != null) {
          entrys.Add(c);
        }
      }
      this.entrys = entrys.ToArray();
    }

    private Project _owner;
    public enBase[] entrys;
    public Port port { get; private set; }
    public string name { get; private set; }
    public string nr { get; private set; }
    public int idx { get; private set; }

    public void ViewChanged() {
      PropertyChangedReise("systemVis");
      PropertyChangedReise("systemCur");
      PropertyChangedReise("systemLst");
      PropertyChangedReise("serialVis");
      PropertyChangedReise("serialCur");
      PropertyChangedReise("serialLst");
    }
    #region view
    public System.Windows.Visibility systemVis {
      get {
        bool v = entrys.Any(z => z.type == EntryType.system && _owner.EntryIsEnabled(z));
        return v ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase systemCur {
      get {
        var cur = entrys.FirstOrDefault(z => z.type == EntryType.system && z.selected);
        return cur ?? enBase.none;
      }
      set {
        if(value != this.systemCur) {
          foreach(var e in entrys.Where(z => z.type == EntryType.system && z.selected && z != value)) {
            e.selected = false;
          }
          if(value != null && value.type == EntryType.system) {
            value.selected = true;
          }
          _owner.RefreshView();
        }

      }
    }
    public List<enBase> systemLst {
      get {
        List<enBase> lst = new List<enBase>();
        lst.Add(enBase.none);
        lst.AddRange(entrys.Where(z => z.type == EntryType.system));
        return lst;
      }
    }
    public System.Windows.Visibility serialVis {
      get {
        bool v = entrys.Any(z => z.type == EntryType.serial && _owner.EntryIsEnabled(z));
        return v ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase serialCur {
      get {
        var cur = entrys.FirstOrDefault(z => z.type == EntryType.serial && z.selected);
        return cur ?? enBase.none;
      }
      set {
        if(value != this.serialCur) {
          foreach(var e in entrys.Where(z => z.type == EntryType.serial && z.selected && z != value)) {
            e.selected = false;
          }
          if(value != null && value.type == EntryType.serial) {
            value.selected = true;
          }
          _owner.RefreshView();
        }
      }
    }
    public List<enBase> serialLst {
      get {
        List<enBase> lst = new List<enBase>();
        lst.Add(enBase.none);
        lst.AddRange(entrys.Where(z => z.type == EntryType.serial && _owner.EntryIsEnabled(z)));
        return lst;
      }
    }
    #endregion view

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    protected void PropertyChangedReise([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "") {
      if(PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
    #endregion INotifyPropertyChanged Members

    public override string ToString() {
      return string.IsNullOrEmpty(nr) ? name : (name + "[" + nr + "]");
    }
  }
  internal enum RcUse : ushort {
    None = '0',
    Baned = 'B',
    Shared = 'S',
    Exclusive = 'X',
  }
}
