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
        case "system":
          c = new enSystem(el, this);
          break;
        case "dio":
          c = new enDIO(el, this);
          break;
        case "ain":
          c = new enAin(el, this);
          break;
        case "pwm":
          c = new enPwm(el, this);
          break;
        case "serial":
          c = new enSerial(el, this);
          break;
        case "spi":
          c = new enSpi(el, this);
          break;
        case "twi":
          c = new enTwi(el, this);
          break;
        }
        if(c != null) {
          entrys.Add(c);
        }
      }
      this.entrys = entrys.ToArray();
      _addr = -2;
    }

    internal Project _owner;
    public enBase[] entrys;
    public Port port { get; private set; }
    public string name { get; private set; }
    public string nr { get; private set; }
    public int idx { get; private set; }

    private PinCfg _config;
    private int _addr;

    public void ViewChanged() {
      PropertyChangedReise("systemVis");
      PropertyChangedReise("systemCur");
      PropertyChangedReise("systemLst");
      PropertyChangedReise("phy1Vis");
      PropertyChangedReise("phy1Cur");
      PropertyChangedReise("phy1Lst");
      PropertyChangedReise("phy2Vis");
      PropertyChangedReise("phy2Cur");
      PropertyChangedReise("phy2Lst");
      PropertyChangedReise("dioVis");
      PropertyChangedReise("dioCur");
      PropertyChangedReise("dioLst");
      PropertyChangedReise("ainVis");
      PropertyChangedReise("ainCur");
      PropertyChangedReise("ainLst");
      PropertyChangedReise("pwmVis");
      PropertyChangedReise("pwmCur");
      PropertyChangedReise("pwmLst");
      PropertyChangedReise("serialVis");
      PropertyChangedReise("serialCur");
      PropertyChangedReise("serialLst");
      PropertyChangedReise("spiVis");
      PropertyChangedReise("spiCur");
      PropertyChangedReise("spiLst");
      PropertyChangedReise("twiVis");
      PropertyChangedReise("twiCur");
      PropertyChangedReise("twiLst");
    }

    #region view
    private bool GetVis(EntryType t) {
      return entrys.Any(z => z.type == t && _owner.EntryIsEnabled(z));;
    }
    private enBase GetCur(EntryType t) {
      enBase cur;
      foreach(var c in entrys.Where(z => z.type == t && z.selected && !_owner.EntryIsEnabled(z))) {
        c.selected = false;
      }
      cur = entrys.FirstOrDefault(z => z.type == t && z.selected);
      return cur ?? enBase.none;
    }
    private void SetCur(EntryType t, enBase v) {
      if(v != GetCur(t)) {
        foreach(var e in entrys.Where(z => z.type == t && z.selected && z != v)) {
          e.selected = false;
        }
        if(v != null && v.type == t) {
          v.selected = true;
        }
        _owner.RefreshView();
      }
    }
    private List<enBase> GetLst(EntryType t) {
      List<enBase> lst = new List<enBase>();
      lst.Add(enBase.none);
      lst.AddRange(entrys.Where(z => z.type == t && _owner.EntryIsEnabled(z)));
      return lst;
    }

    public System.Windows.Visibility systemVis { get { return ((_config == PinCfg.None || _config == PinCfg.System) && GetVis(EntryType.system)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase systemCur {
      get { return GetCur(EntryType.system); }
      set { 
        SetCur(EntryType.system, value);
        if(value != null && value.type == EntryType.system) {
          _config = PinCfg.System;
        } else if(_config == PinCfg.System) {
          _config=PinCfg.None;
        }
      }
    }
    public List<enBase> systemLst { get { return GetLst(EntryType.system); } }

    public System.Windows.Visibility phy1Vis { 
      get { 
        bool v=false;
        if(_owner.phy1!=null && _config == PinCfg.None || _config == PinCfg.Phy1){
          var lst=_owner.phy1.GetLst(this);
          v = lst != null;
        }
        return v ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; 
      } 
    }
    public enBase phy1Cur {
      get {
        return _owner.phy1 != null ? _owner.phy1.GetCur(this) : enBase.none;
      }
      set {
        
        if(_owner.phy1 != null) {
          bool refresh=_owner.phy1.SetCur(this, value);
          if(value != null && value.type != EntryType.none) {
            _config = PinCfg.Phy1;
          } else if(_config == PinCfg.Phy1) {
            _config = PinCfg.None;
          }
          if(refresh) {
            _owner.RefreshView();
          }
        }
      }
    }
    public List<enBase> phy1Lst {
      get {
        if(_owner.phy1 != null) {
          return _owner.phy1.GetLst(this);
        }
        return null;
      }
    }

    public System.Windows.Visibility phy2Vis {
      get {
        bool v = false;
        if(_owner.phy2 != null && _config == PinCfg.None || _config == PinCfg.Phy2) {
          var lst = _owner.phy2.GetLst(this);
          v = lst != null;
        }
        return v ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase phy2Cur {
      get {
        return _owner.phy2 != null ? _owner.phy2.GetCur(this) : enBase.none;
      }
      set {

        if(_owner.phy2 != null) {
          bool refresh = _owner.phy2.SetCur(this, value);
          if(value != null && value.type != EntryType.none) {
            _config = PinCfg.Phy2;
          } else if(_config == PinCfg.Phy2) {
            _config = PinCfg.None;
          }
          if(refresh) {
            _owner.RefreshView();
          }
        }
      }
    }
    public List<enBase> phy2Lst {
      get {
        if(_owner.phy2 != null) {
          return _owner.phy2.GetLst(this);
        }
        return null;
      }
    }

    public System.Windows.Visibility dioVis { get { return ((_config == PinCfg.None || _config == PinCfg.IO) && GetVis(EntryType.dio)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public string dioCur {
      get {
        return (!entrys.Any(z => z.type == EntryType.dio && _owner.EntryIsEnabled(z)) || _addr < 0) ? "none" : _addr.ToString();
      }
      set {
        int tmp;
        var en = entrys.FirstOrDefault(z => z.type == EntryType.dio) as enDIO;
        if(en == null || value == "none" || !int.TryParse(value, out tmp)) {
          tmp = -1;
        }
        if(_addr != tmp) {
          _addr = tmp;
          if(_addr >= 0) {
            _config = PinCfg.IO;
            foreach(var i2 in entrys.Where(z=>z.type!=EntryType.system && z.type!=EntryType.dio && z.type!=EntryType.spi && _owner.EntryIsEnabled(z))){
              i2.selected=true;
            }
          } else if(_config == PinCfg.IO) {
            foreach(var i2 in entrys.Where(z => z.type != EntryType.system && z.selected)) {
              i2.selected = false;
            }
            _config = PinCfg.None;
          }
          _owner.RefreshView();
        }
      }
    }
    public List<string> dioLst {
      get {
        var lst = new List<string>(100);
        string tmp;
        lst.Add("none");
        if(entrys.Any(z => z.type == EntryType.dio && _owner.EntryIsEnabled(z))) {
          for(int i = 0; i < 100; i++) {
            lst.Add(i.ToString());
          }
          foreach(var p in _owner.pins) {
            tmp = p.dioCur;
            if(p!=this && tmp != "none") {
              lst.Remove(tmp);
            }
          }
        }
        return lst;
      }
    }

    public System.Windows.Visibility ainVis { get { return ((_config == PinCfg.IO) && GetVis(EntryType.ain)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase ainCur {
      get { return GetCur(EntryType.ain); }
      set { SetCur(EntryType.ain, value); }
    }
    public List<enBase> ainLst { get { return GetLst(EntryType.ain); } }

    public System.Windows.Visibility pwmVis { get { return ((_config == PinCfg.IO) && GetVis(EntryType.pwm)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase pwmCur {
      get { return GetCur(EntryType.pwm); }
      set { SetCur(EntryType.pwm, value); }
    }
    public List<enBase> pwmLst { get { return GetLst(EntryType.pwm); } }

    public System.Windows.Visibility serialVis { get { return ((_config == PinCfg.IO) && GetVis(EntryType.serial)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase serialCur {
      get { return GetCur(EntryType.serial); }
      set { SetCur(EntryType.serial, value); }
    }
    public List<enBase> serialLst { get { return GetLst(EntryType.serial); } }

    public System.Windows.Visibility spiVis { get { return ((_config == PinCfg.IO) && GetVis(EntryType.spi)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase spiCur {
      get { return GetCur(EntryType.spi); }
      set { SetCur(EntryType.spi, value); }
    }
    public List<enBase> spiLst { get { return GetLst(EntryType.spi); } }

    public System.Windows.Visibility twiVis { get { return ((_config == PinCfg.IO) && GetVis(EntryType.twi)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase twiCur {
      get { return GetCur(EntryType.twi); }
      set { SetCur(EntryType.twi, value); }
    }
    public List<enBase> twiLst { get { return GetLst(EntryType.twi); } }

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
  internal enum PinCfg {
    None,
    System,
    Phy1,
    Phy2,
    Phy3,
    Phy4,
    IO,
  }
}
