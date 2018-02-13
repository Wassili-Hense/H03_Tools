///<remarks>This file is part of the <see cref="https://github.com/X13home">X13.Home</see> project.<remarks>
using JSC = NiL.JS.Core;
using JSL = NiL.JS.BaseLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace X13 {
  internal class Pin : INotifyPropertyChanged {
    public Pin(Project owner, XElement info, Port port) {
      this._owner = owner;
      this.port = port;
      if(port == null) {
        this.name = info.Attribute("name").Value;
        var xn = info.Attribute("fpin");
        this.nr = xn != null ? xn.Value : null;
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
          entrys.Add(new enSysLed((enDIO)c, true));
          entrys.Add(new enSysLed((enDIO)c, false));
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

    public Project _owner;
    public enBase[] entrys;
    public Port port { get; private set; }
    public string name { get; private set; }
    public string nr { get; private set; }
    public int idx { get; private set; }
    public int mapping { get { return _addr; } }

    public PinCfg config;
    public int _addr;

    public void ViewChanged() {
      PropertyChangedReise("systemVis");
      PropertyChangedReise("systemCur");
      PropertyChangedReise("systemLst");
      PropertyChangedReise("phy1Vis");
      PropertyChangedReise("phy1Cur");
      PropertyChangedReise("phy2Vis");
      PropertyChangedReise("phy2Cur");
      PropertyChangedReise("phy3Vis");
      PropertyChangedReise("phy3Cur");
      PropertyChangedReise("phy4Vis");
      PropertyChangedReise("phy4Cur");
      PropertyChangedReise("dioVis");
      PropertyChangedReise("dioCur");
      PropertyChangedReise("dioLst");
      PropertyChangedReise("ainVis");
      PropertyChangedReise("ainCur");
      PropertyChangedReise("ainLst");
      PropertyChangedReise("pwmVis");
      PropertyChangedReise("pwmCur");
      PropertyChangedReise("pwmLst");
      PropertyChangedReise("pwmIsAvaliable");
      PropertyChangedReise("serialVis");
      PropertyChangedReise("serialCur");
      PropertyChangedReise("serialLst");
      PropertyChangedReise("twiVis");
      PropertyChangedReise("twiCur");
      PropertyChangedReise("twiLst");
      PropertyChangedReise("titelVis");
    }
    public void Load(XElement it) {
      this.config = (PinCfg)Enum.Parse(typeof(PinCfg), it.Attribute("cfg").Value);
      var xn = it.Attribute("addr");
      if(xn != null && config == PinCfg.IO) {
        _addr = int.Parse(xn.Value);
        xn = it.Attribute("titel");
        if(xn != null && !string.IsNullOrEmpty(xn.Value)) {
          titelCur = xn.Value;
        }
      } else {
        _addr = -1;
      }
      foreach(var i2 in it.Elements("item")) {
        string name = i2.Attribute("name").Value;
        xn = i2.Attribute("func");
        var en = entrys.FirstOrDefault(z => z.name == name);
        if(en != null) {
          if(config == PinCfg.Phy1 && _owner.phy1 != null) {
            _owner.phy1.SetCur(this, en, xn != null ? xn.Value : null);
          } else if(config == PinCfg.Phy2 && _owner.phy2 != null) {
            _owner.phy2.SetCur(this, en, xn != null ? xn.Value : null);
          } else if(config == PinCfg.Phy3 && _owner.phy3 != null) {
            _owner.phy3.SetCur(this, en, xn != null ? xn.Value : null);
          } else if(config == PinCfg.Phy4 && _owner.phy4 != null) {
            _owner.phy4.SetCur(this, en, xn != null ? xn.Value : null);
          } else {
            en.selected = true;
          }
        }
      }
    }
    public XElement Save() {
      var el = new XElement("pin");
      el.Add(new XAttribute("name", name));
      el.Add(new XAttribute("cfg", config.ToString()));
      if(config == PinCfg.IO && _addr >= 0) {
        el.Add(new XAttribute("addr", _addr.ToString()));
      }
      if(titelCur != null) {
        el.Add(new XAttribute("titel", titelCur));
      }

      foreach(var en in entrys.Where(z => z.selected)) {
        var e2 = new XElement("item", new XAttribute("name", en.name));
        if(en.func != null) {
          e2.Add(new XAttribute("func", en.func));
        }
        el.Add(e2);
      }
      return el;
    }
    public void ExportX(Section section, XElement parent) {
      XElement rez = null;
      switch(section) {
      case Section.IP:
        if(config == PinCfg.IO && entrys.Any(z => z.type == EntryType.dio && z.selected)) {
          int ri = _owner.ExIndex(name + "_used");
          rez = Project.CreateXItem("Ip" + _addr.ToString(), (_addr < 10 ? "A" : "B") + "zX" + ri.ToString());
          rez.Add(Project.CreateXItem("_description", "Digital input, " + (titelCur ?? name)));
        }
        break;
      case Section.IN:
        if(config == PinCfg.IO && entrys.Any(z => z.type == EntryType.dio && z.selected)) {
          int ri = _owner.ExIndex(name + "_used");
          rez = Project.CreateXItem("In" + _addr.ToString(), (_addr < 10 ? "E" : "D") + "zX" + ri.ToString());
          rez.Add(Project.CreateXItem("_description", "Digital inverted input, " + (titelCur ?? name)));
        }
        break;
      case Section.OP:
        if(config == PinCfg.IO && entrys.Any(z => z.type == EntryType.dio && z.selected)) {
          int ri = _owner.ExIndex(name + "_used");
          rez = Project.CreateXItem("Op" + _addr.ToString(), (_addr < 10 ? "C" : "D") + "zX" + ri.ToString());
          rez.Add(Project.CreateXItem("_description", "Digital output, " + (titelCur ?? name)));
        }
        break;
      case Section.ON:
        if(config == PinCfg.IO && entrys.Any(z => z.type == EntryType.dio && z.selected)) {
          int ri = _owner.ExIndex(name + "_used");
          rez = Project.CreateXItem("On" + _addr.ToString(), (_addr < 10 ? "F" : "G") + "zX" + ri.ToString());
          rez.Add(Project.CreateXItem("_description", "Digital inverted output, " + (titelCur ?? name)));
        }
        break;
      case Section.PP:
        if(config == PinCfg.IO && pwmCur.type == EntryType.pwm) {
          int ri = _owner.ExIndex(name + "_used");
          int ti = _owner.ExIndex(pwmCur.name);
          rez = Project.CreateXItem("Pp" + _addr.ToString(), (_addr < 10 ? "I" : "J") + "iX" + ri.ToString() + ", X" + ti.ToString());
          rez.Add(Project.CreateXItem("_description", "PWM output, " + (titelCur ?? name)));
        }
        break;
      case Section.PN:
        if(config == PinCfg.IO && pwmCur.type == EntryType.pwm) {
          int ri = _owner.ExIndex(name + "_used");
          int ti = _owner.ExIndex(pwmCur.name);
          rez = Project.CreateXItem("Pn" + _addr.ToString(), (_addr < 10 ? "K" : "L") + "iX" + ri.ToString() + ", X" + ti.ToString());
          rez.Add(Project.CreateXItem("_description", "PWM inverted output, " + (titelCur ?? name)));
        }
        break;
      case Section.Ae:
        if(config == PinCfg.IO && ainCur.type == EntryType.ain) {
          int ri = _owner.ExIndex(name + "_used");
          int ti = _owner.ExIndex(ainCur.name);
          rez = Project.CreateXItem("Ae" + _addr.ToString(), (_addr < 10 ? "M" : "N") + "iX" + ri.ToString() + ", X" + ti.ToString());
          rez.Add(Project.CreateXItem("_description", "Analog input external reference, " + (titelCur ?? name)));
        }
        break;
      case Section.Av:
        if(config == PinCfg.IO && ainCur.type == EntryType.ain) {
          int ri = _owner.ExIndex(name + "_used");
          int ti = _owner.ExIndex(ainCur.name);
          rez = Project.CreateXItem("Av" + _addr.ToString(), (_addr < 10 ? "M" : "N") + "iX" + ri.ToString() + ", X" + ti.ToString());
          rez.Add(Project.CreateXItem("_description", "Analog input AVcc reference, " + (titelCur ?? name)));
        }
        break;
      case Section.Ai:
        if(config == PinCfg.IO && ainCur.type == EntryType.ain) {
          int ri = _owner.ExIndex(name + "_used");
          int ti = _owner.ExIndex(ainCur.name);
          rez = Project.CreateXItem("Ai" + _addr.ToString(), (_addr < 10 ? "M" : "N") + "iX" + ri.ToString() + ", X" + ti.ToString());
          rez.Add(Project.CreateXItem("_description", "Analog input internal reference, " + (titelCur ?? name)));
        }
        break;
      case Section.AI:
        if(config == PinCfg.IO && ainCur.type == EntryType.ain) {
          int ri = _owner.ExIndex(name + "_used");
          int ti = _owner.ExIndex(ainCur.name);
          rez = Project.CreateXItem("AI" + _addr.ToString(), (_addr < 10 ? "M" : "N") + "iX" + ri.ToString() + ", X" + ti.ToString());
          rez.Add(Project.CreateXItem("_description", "Analog input internal2 reference, " + (titelCur ?? name)));
        }
        break;
      case Section.Serial: {
          enSerial uart;
          if(config == PinCfg.IO && (uart = serialCur as enSerial) != null) {
            string uName = "Serial " + uart.mapping.ToString();
            XElement uParent = parent.Elements("item").FirstOrDefault(z => z.Attribute("name").Value == uName);
            if(uParent == null) {
              uParent = new XElement("item", new XAttribute("name", uName));
              parent.Add(uParent);
            }

            int r_pin = _owner.ExIndex(name + "_used");
            int r_s0 = _owner.ExIndex("UART" + uart.channel.ToString() + "_s0");
            int r_s1 = _owner.ExIndex("UART" + uart.channel.ToString() + "_s1");
            int r_s2 = _owner.ExIndex("UART" + uart.channel.ToString() + "_s2");
            int r_s3 = _owner.ExIndex("UART" + uart.channel.ToString() + "_s3");
            int r_s4 = _owner.ExIndex("UART" + uart.channel.ToString() + "_s4");
            if(uart.signal == Signal.UART_RX) {
              rez = Project.CreateXItem("Sr" + uart.mapping.ToString(), string.Format("ObX{0},S{1},B{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "RxD 2400, " + ((titelCur ?? name))));
              uParent.Add(rez);
              rez = Project.CreateXItem("Sr" + (16 + uart.mapping).ToString(), string.Format("ObX{0},B{1},S{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "RxD 4800, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("Sr" + (32 + uart.mapping).ToString(), string.Format("ObX{0},B{1},B{2},S{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "RxD 9600, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("Sr" + (48 + uart.mapping).ToString(), string.Format("ObX{0},B{1},B{2},B{3},S{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "RxD 19200, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("Sr" + (64 + uart.mapping).ToString(), string.Format("ObX{0},B{1},B{2},B{3},B{4},S{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "RxD 38400, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = null;
            } else if(uart.signal == Signal.UART_TX) {
              rez = Project.CreateXItem("St" + uart.mapping.ToString(), string.Format("ObX{0},S{1},B{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "TxD 2400, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("St" + (16 + uart.mapping).ToString(), string.Format("ObX{0},B{1},S{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "TxD 4800, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("St" + (32 + uart.mapping).ToString(), string.Format("ObX{0},B{1},B{2},S{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "TxD 9600, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("St" + (48 + uart.mapping).ToString(), string.Format("ObX{0},B{1},B{2},B{3},S{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "TxD 19200, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = Project.CreateXItem("St" + (64 + uart.mapping).ToString(), string.Format("ObX{0},B{1},B{2},B{3},B{4},S{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4));
              rez.Add(Project.CreateXItem("_description", "TxD 38400, " + (titelCur ?? name)));
              uParent.Add(rez);
              rez = null;
            }
          }
        }
        break;
      }
      if(rez != null) {
        parent.Add(rez);
      }
    }
    internal void ExportX04(Dictionary<string, JSC.JSObject> children) {
      for(int i = 0; i < entrys.Length; i++) {
        entrys[i].ExportX04(children);
      }
    }
    public string ExportPinOut() {
      if(nr == null || (config == PinCfg.None && twiCur.type != EntryType.twi)) {
        return null;
      }
      var sb = new StringBuilder();
      sb.Append("// " + nr + "\t" + name + "\t");
      if(!string.IsNullOrEmpty(titelCur)) {
        sb.Append(titelCur);
      }
      sb.Append("\t");
      if(config == PinCfg.System) {
        sb.Append("| sys_" + (systemCur.func ?? systemCur.name));
      } else if(config == PinCfg.Phy1) {
        sb.Append("| phy1_"+(phy1Cur.func ?? phy1Cur.name));
      } else if(config == PinCfg.Phy2) {
        sb.Append("| phy2_" + (phy2Cur.func ?? phy2Cur.name));
      } else if(config == PinCfg.Phy3) {
        sb.Append("| phy3_" + (phy3Cur.func ?? phy3Cur.name));
      } else if(config == PinCfg.Phy4) {
        sb.Append("| phy4_" + (phy4Cur.func ?? phy4Cur.name));
      } else {
        if(ainCur.type != EntryType.none) {
          sb.Append("| "+ainCur.name + "\t");
        }
        if(pwmCur.type != EntryType.none) {
          sb.Append("| " + pwmCur.name + "\t");
        }
        if(twiCur.type != EntryType.none) {
          sb.Append("| " + twiCur.signal.ToString() + "\t");
        }
        if(serialCur.type != EntryType.none) {
          sb.Append("| " + serialCur.signal.ToString()+" "+ (serialCur as enSerial).mapping.ToString() + "\t");
        }
      }
      sb.Append("\r\n");

      return sb.ToString();
    }

    #region view
    private bool GetVis(EntryType t) {
      return entrys.Any(z => z.type == t && _owner.EntryIsEnabled(z));
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

    public System.Windows.Visibility systemVis {
      get {
        return ((config == PinCfg.None || config == PinCfg.System) && (GetVis(EntryType.system))) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase systemCur {
      get {
        var s = GetCur(EntryType.system);
        return s;
      }
      set {
        if(value == null) {
          return;
        }
        SetCur(EntryType.system, value);
        if(value != null && value.type != EntryType.none) {
          config = PinCfg.System;
        } else if(config == PinCfg.System) {
          config = PinCfg.None;
        }
      }
    }
    public List<enBase> systemLst {
      get {
        return GetLst(EntryType.system);
      }
    }

    public System.Windows.Visibility phy1Vis {
      get {
        List<enBase> lst;
        if(_owner.phy1 != null && (config == PinCfg.None || config == PinCfg.Phy1)) {
          lst = _owner.phy1.GetLst(this);
        } else {
          lst = null;
        }
        if((lst!=null || phy1Lst!=null) && (lst==null || phy1Lst==null || !lst.SequenceEqual(phy1Lst))){
          phy1Lst = lst;
          PropertyChangedReise("phy1Lst");
          int fl = 0;
          if(lst != null) {
            fl |= lst.Any(z => z.type == EntryType.spi) ? 1 : 0;
            fl |= lst.Any(z => z.type == EntryType.serial) ? 2 : 0;
            fl |= lst.Any(z => z.type == EntryType.dio) ? 4 : 0;
          }
          switch(fl) {
          case 1:
            phy1Border = Brushes.Red;
            break;
          case 2:
            phy1Border = Brushes.Blue;
            break;
          case 3:
            phy1Border = Brushes.Violet;
            break;
          case 4:
            phy1Border = Brushes.LightSlateGray;
            break;
          case 5:
            phy1Border = Brushes.Orange;
            break;
          case 6:
            phy1Border = Brushes.Cyan;
            break;
          case 7:
            phy1Border = Brushes.Black;
            break;
          default:
            phy1Border = null;
            break;
          }
          PropertyChangedReise("phy1Border");
        }
        return phy1Lst != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase phy1Cur {
      get {
        return _owner.phy1 != null ? _owner.phy1.GetCur(this) : enBase.none;
      }
      set {
        if(_owner.phy1 != null) {
          bool refresh = _owner.phy1.SetCur(this, value, null);
          if(value != null && value.type != EntryType.none) {
            config = PinCfg.Phy1;
          } else if(config == PinCfg.Phy1) {
            config = PinCfg.None;
          }
          if(refresh) {
            _owner.RefreshView();
          }
        }
      }
    }
    public List<enBase> phy1Lst { get; private set; }
    public System.Windows.Media.Brush phy1Border { get; private set; }

    public System.Windows.Visibility phy2Vis {
      get {
        List<enBase> lst;
        if(_owner.phy2 != null && (config == PinCfg.None || config == PinCfg.Phy2)) {
          lst = _owner.phy2.GetLst(this);
        } else {
          lst = null;
        }
        if((lst != null || phy2Lst != null) && (lst == null || phy2Lst == null || !lst.SequenceEqual(phy2Lst))) {
          phy2Lst = lst;
          PropertyChangedReise("phy2Lst");
          int fl = 0;
          if(lst != null) {
            fl |= lst.Any(z => z.type == EntryType.spi) ? 1 : 0;
            fl |= lst.Any(z => z.type == EntryType.serial) ? 2 : 0;
            fl |= lst.Any(z => z.type == EntryType.dio) ? 4 : 0;
          }
          switch(fl) {
          case 1:
            phy2Border = Brushes.Red;
            break;
          case 2:
            phy2Border = Brushes.Blue;
            break;
          case 3:
            phy2Border = Brushes.Violet;
            break;
          case 4:
            phy2Border = Brushes.LightSlateGray;
            break;
          case 5:
            phy2Border = Brushes.Orange;
            break;
          case 6:
            phy2Border = Brushes.Cyan;
            break;
          case 7:
            phy2Border = Brushes.Black;
            break;
          default:
            phy2Border = null;
            break;
          }
          PropertyChangedReise("phy2Border");
        }
        return phy2Lst != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase phy2Cur {
      get {
        return _owner.phy2 != null ? _owner.phy2.GetCur(this) : enBase.none;
      }
      set {

        if(_owner.phy2 != null) {
          bool refresh = _owner.phy2.SetCur(this, value, null);
          if(value != null && value.type != EntryType.none) {
            config = PinCfg.Phy2;
          } else if(config == PinCfg.Phy2) {
            config = PinCfg.None;
          }
          if(refresh) {
            _owner.RefreshView();
          }
        }
      }
    }
    public List<enBase> phy2Lst { get; private set; }
    public System.Windows.Media.Brush phy2Border { get; private set; }

    public System.Windows.Visibility phy3Vis {
      get {
        List<enBase> lst;
        if(_owner.phy3 != null && (config == PinCfg.None || config == PinCfg.Phy3)) {
          lst = _owner.phy3.GetLst(this);
        } else {
          lst = null;
        }
        if((lst != null || phy3Lst != null) && (lst == null || phy3Lst == null || !lst.SequenceEqual(phy3Lst))) {
          phy3Lst = lst;
          PropertyChangedReise("phy3Lst");
          int fl = 0;
          if(lst != null) {
            fl |= lst.Any(z => z.type == EntryType.spi) ? 1 : 0;
            fl |= lst.Any(z => z.type == EntryType.serial) ? 2 : 0;
            fl |= lst.Any(z => z.type == EntryType.dio) ? 4 : 0;
          }
          switch(fl) {
          case 1:
            phy3Border = Brushes.Red;
            break;
          case 2:
            phy3Border = Brushes.Blue;
            break;
          case 3:
            phy3Border = Brushes.Violet;
            break;
          case 4:
            phy3Border = Brushes.LightSlateGray;
            break;
          case 5:
            phy3Border = Brushes.Orange;
            break;
          case 6:
            phy3Border = Brushes.Cyan;
            break;
          case 7:
            phy3Border = Brushes.Black;
            break;
          default:
            phy3Border = null;
            break;
          }
          PropertyChangedReise("phy3Border");
        }
        return phy3Lst != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase phy3Cur {
      get {
        return _owner.phy3 != null ? _owner.phy3.GetCur(this) : enBase.none;
      }
      set {

        if(_owner.phy3 != null) {
          bool refresh = _owner.phy3.SetCur(this, value, null);
          if(value != null && value.type != EntryType.none) {
            config = PinCfg.Phy3;
          } else if(config == PinCfg.Phy3) {
            config = PinCfg.None;
          }
          if(refresh) {
            _owner.RefreshView();
          }
        }
      }
    }
    public List<enBase> phy3Lst { get; private set; }
    public System.Windows.Media.Brush phy3Border { get; private set; }

    public System.Windows.Visibility phy4Vis {
      get {
        List<enBase> lst;
        if(_owner.phy4 != null && (config == PinCfg.None || config == PinCfg.Phy4)) {
          lst = _owner.phy4.GetLst(this);
        } else {
          lst = null;
        }
        if((lst != null || phy4Lst != null) && (lst == null || phy4Lst == null || !lst.SequenceEqual(phy4Lst))) {
          phy4Lst = lst;
          PropertyChangedReise("phy4Lst");
          int fl = 0;
          if(lst != null) {
            fl |= lst.Any(z => z.type == EntryType.spi) ? 1 : 0;
            fl |= lst.Any(z => z.type == EntryType.serial) ? 2 : 0;
            fl |= lst.Any(z => z.type == EntryType.dio) ? 4 : 0;
          }
          switch(fl) {
          case 1:
            phy4Border = Brushes.Red;
            break;
          case 2:
            phy4Border = Brushes.Blue;
            break;
          case 3:
            phy4Border = Brushes.Violet;
            break;
          case 4:
            phy4Border = Brushes.LightSlateGray;
            break;
          case 5:
            phy4Border = Brushes.Orange;
            break;
          case 6:
            phy4Border = Brushes.Cyan;
            break;
          case 7:
            phy4Border = Brushes.Black;
            break;
          default:
            phy4Border = null;
            break;
          }
          PropertyChangedReise("phy4Border");
        }
        return phy4Lst != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      }
    }
    public enBase phy4Cur {
      get {
        return _owner.phy4 != null ? _owner.phy4.GetCur(this) : enBase.none;
      }
      set {

        if(_owner.phy4 != null) {
          bool refresh = _owner.phy4.SetCur(this, value, null);
          if(value != null && value.type != EntryType.none) {
            config = PinCfg.Phy4;
          } else if(config == PinCfg.Phy4) {
            config = PinCfg.None;
          }
          if(refresh) {
            _owner.RefreshView();
          }
        }
      }
    }
    public List<enBase> phy4Lst { get; private set; }
    public System.Windows.Media.Brush phy4Border { get; private set; }

    public System.Windows.Visibility dioVis { get { return ((config == PinCfg.None || config == PinCfg.IO) && (GetVis(EntryType.dio) || GetVis(EntryType.ain))) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public string dioCur {
      get {
        return (!entrys.Any(z => (z.type == EntryType.dio || z.type == EntryType.ain) && _owner.EntryIsEnabled(z)) || _addr < 0) ? "--" : _addr.ToString("00");
      }
      set {
        int tmp;
        var en = entrys.FirstOrDefault(z => z.type == EntryType.dio || z.type == EntryType.ain);
        if(en == null || value == "--" || !int.TryParse(value, out tmp)) {
          tmp = -1;
        }
        if(_addr != tmp) {
          _addr = tmp;
          if(_addr >= 0) {
            config = PinCfg.IO;
            foreach(var i2 in entrys.Where(z => z.type != EntryType.system && z.type != EntryType.spi && z.type != EntryType.twi && z.signal != Signal.UART_DE && _owner.EntryIsEnabled(z))) {
              if(i2.type != EntryType.pwm || !entrys.Any(z => z.type == i2.type && z.selected && z != i2)) {
                i2.selected = true;
              }
            }
          } else if(config == PinCfg.IO) {
            foreach(var i2 in entrys.Where(z => z.selected)) {
              i2.selected = false;
            }
            config = PinCfg.None;
          }
          _owner.RefreshView();
        }
      }
    }
    public List<string> dioLst {
      get {
        var lst = new List<string>(100);
        string tmp;
        lst.Add("--");
        if(entrys.Any(z => (z.type == EntryType.dio || z.type == EntryType.ain) && _owner.EntryIsEnabled(z))) {
          for(int i = 0; i < 100; i++) {
            lst.Add(i.ToString("00"));
          }
          foreach(var p in _owner.pins) {
            tmp = p.dioCur;
            if(p != this && tmp != "--") {
              lst.Remove(tmp);
            }
          }
        }
        return lst;
      }
    }

    public System.Windows.Visibility ainVis { get { return ((config == PinCfg.IO) && GetVis(EntryType.ain)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase ainCur {
      get { return GetCur(EntryType.ain); }
      set { SetCur(EntryType.ain, value); }
    }
    public List<enBase> ainLst { get { return GetLst(EntryType.ain); } }

    public System.Windows.Visibility pwmVis { get { return ((config == PinCfg.IO) && entrys.Any(z => z.type == EntryType.pwm)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase pwmCur {
      get {
        enBase cur = entrys.FirstOrDefault(z => z.type == EntryType.pwm && z.selected);
        return cur ?? enBase.none;
      }
      set {
        if(value != pwmCur) {
          foreach(var e in entrys.Where(z => z.type == EntryType.pwm && z.selected && z != value)) {
            e.selected = false;
          }
          if(value != null && value.type == EntryType.pwm) {
            value.selected = true;
          }
          _owner.RefreshView();
        }
      }
    }
    public bool pwmIsAvaliable {
      get {
        return pwmCur.isAvailable;
      }
    }
    public List<enBase> pwmLst {
      get {
        List<enBase> lst = new List<enBase>();
        lst.Add(enBase.none);
        lst.AddRange(entrys.Where(z => z.type == EntryType.pwm));
        return lst;
      }
    }

    public System.Windows.Visibility serialVis { get { return ((config == PinCfg.IO) && entrys.Any(z => z.type == EntryType.serial && z.signal != Signal.UART_DE && _owner.EntryIsEnabled(z))) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase serialCur {
      get { return config == PinCfg.IO ? GetCur(EntryType.serial) : enBase.none; }
      set { SetCur(EntryType.serial, value); }
    }
    public List<enBase> serialLst {
      get {
        List<enBase> lst = new List<enBase>();
        lst.Add(enBase.none);
        lst.AddRange(entrys.Where(z => z.type == EntryType.serial && z.signal != Signal.UART_DE && _owner.EntryIsEnabled(z)));
        return lst;
      }
    }

    public System.Windows.Visibility twiVis { get { return ((config == PinCfg.IO || config == PinCfg.None) && GetVis(EntryType.twi)) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; } }
    public enBase twiCur {
      get { return GetCur(EntryType.twi); }
      set { SetCur(EntryType.twi, value); }
    }
    public List<enBase> twiLst { get { return GetLst(EntryType.twi); } }

    public System.Windows.Visibility titelVis { get { return /*config == PinCfg.IO ?*/ System.Windows.Visibility.Visible/* : System.Windows.Visibility.Collapsed*/; } }
    public string titelCur { get; set; }
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
