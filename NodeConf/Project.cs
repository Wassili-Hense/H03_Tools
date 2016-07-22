using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class Project {
    public static XElement CreateXItem(string name, string value) {
      var dev = new XElement("item");
      dev.Add(new XAttribute("name", name));
      dev.Add(new XAttribute("value", value));
      dev.Add(new XAttribute("saved", "True"));
      dev.Add(new XAttribute("type", "string"));
      return dev;
    }

    public static Project LoadFromCpu(string path, bool init = true) {
      var p = new Project();
      p._cpuPath = path;
      try {
        XDocument doc = XDocument.Load(System.IO.Path.GetFullPath(@".\cpu\" + p._cpuPath + ".xml"));
        p._cpuSignature = doc.Root.Attribute("name").Value;
        p._ainRef = int.Parse(doc.Root.Attribute("ainref").Value.Substring(2), System.Globalization.NumberStyles.HexNumber);
        List<Pin> pins = new List<Pin>();
        pins.AddRange(doc.Root.Elements("port").SelectMany(z => Port.CreatePort(p, z)));
        pins.AddRange(doc.Root.Elements("pin").Select(z => new Pin(p, z, null)));

        p.pins = pins.ToArray();
        if(init) {
          p.SetSysEntrys();
        }
      }
      catch(Exception ex) {
        Console.WriteLine(ex.ToString());
        return null;
      }
      return p;
    }
    public static Project Load(string path) {
      XDocument doc = XDocument.Load(path);
      var prj = LoadFromCpu(doc.Root.Attribute("cpu").Value, false);
      var xn = doc.Root.Attribute("phy1");
      if(xn != null) {
        prj.phy1 = phyBase.Create(xn.Value, 1);
      }
      xn = doc.Root.Attribute("phy2");
      if(xn != null) {
        prj.phy2 = phyBase.Create(xn.Value, 2);
      }
      prj._prjPath = path;
      foreach(var it in doc.Root.Elements("pin")) {
        string name=it.Attribute("name").Value;
        var p = prj.pins.FirstOrDefault(z => z.name == name);
        if(p != null) {
          p.Load(it);
        }
      }
      return prj;
    }

    private string _prjPath;
    private string _cpuPath;
    private string _cpuSignature;
    private phyBase _phy1;
    private phyBase _phy2;
    private List<string> _exResouces;
    private int _ainRef;

    public enDIO led;
    public Pin[] pins { get; private set; }
    public phyBase phy1 { get { return _phy1; } set { _phy1 = value; _prjPath = null; } }
    public phyBase phy2 { get { return _phy2; } set { _phy2 = value; _prjPath = null; } }

    public string Path {
      get {
        if(_prjPath == null) {
          string p = _cpuSignature + (_phy1 == null ? "n" : _phy1.signature) + (_phy2 == null ? "n" : _phy2.signature);
          int i;
          for(i = 10; i < 99; i++) {
            _prjPath = @"projects\" + p + i.ToString("00") + ".xml";
            if(!File.Exists(_prjPath)) {
              break;
            }
          }
        }
        return _prjPath;
      }
      set {
        _prjPath = value;
      }
    }

    private void SetSysEntrys() {
      foreach(var p in pins) {
        foreach(var e in p.entrys.Where(z => z.type == EntryType.system && this.EntryIsEnabled(z))) {
          p.systemCur=e;
          break;
        }
      }
    }

    public bool EntryIsEnabled(enBase en) {
      Dictionary<string, RcUse> resources = new Dictionary<string, RcUse>();
      bool enabled = true;
      RcUse cr;
      foreach(var p in pins) {
        foreach(var e in p.entrys.Where(z => z.selected && z != en)) {
          foreach(var r in e.resouces) {
            if(!resources.TryGetValue(r.Key, out cr)
              || ((r.Value == RcUse.Exclusive && cr != RcUse.Exclusive) || (r.Value == RcUse.Baned && cr == RcUse.Shared))) {
              resources[r.Key] = r.Value;
            }
          }
        }
      }
      foreach(var r in en.resouces) {
        if(resources.TryGetValue(r.Key, out cr)
          && ((r.Value == RcUse.Exclusive && cr == RcUse.Exclusive) || (r.Value == RcUse.Shared && cr != RcUse.Shared) || ((ushort)r.Value >= 0x100 && r.Value != cr))) {
          enabled = false;
          break;
        }
      }
      return enabled;
    }
    public void RefreshView() {
      foreach(var p in pins) {
        p.ViewChanged();
      }
    }
    public void Save() {
      if(!Directory.Exists(System.IO.Path.GetDirectoryName(_prjPath))) {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_prjPath));
      }
      XDocument doc = new XDocument(new XElement("prj"));
      doc.Root.Add(new XAttribute("cpu", _cpuPath));
      if(_phy1 != null) {
        doc.Root.Add(new XAttribute("phy1", _phy1.name));
      }
      if(_phy2 != null) {
        doc.Root.Add(new XAttribute("phy2", _phy2.name));
      }
      foreach(var p in pins) {
        doc.Root.Add(p.Save());
      }
      using(StreamWriter writer = File.CreateText(_prjPath)) {
        doc.Save(writer);
      }
    }
    public void Export() {
      if(!Directory.Exists(System.IO.Path.GetDirectoryName(_prjPath))) {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_prjPath));
      }

      var now = DateTime.Now;

      _exResouces = new List<string>();
      string name = System.IO.Path.GetFileNameWithoutExtension(this.Path);
      if(name.Length > 6) {
        name = name.Substring(0, 6);
      }

      #region UART mapping
      var uMap = new List<int>();
      int idx;
      enSerial up;
      foreach(var p in pins) {
        up = p.serialCur as enSerial;
        if(up != null) {
          idx = uMap.IndexOf(up.channel);
          if(idx < 0) {
            idx = uMap.Count;
            uMap.Add(up.channel);
          }
          up.mapping = idx;
        }
      }
      foreach(var p in pins) {
        up = p.phy1Cur as enSerial;
        if(up != null) {
          idx = uMap.IndexOf(up.channel);
          if(idx < 0) {
            idx = uMap.Count;
            uMap.Add(up.channel);
          }
          up.mapping = idx;
        }
        up = p.phy2Cur as enSerial;
        if(up != null) {
          idx = uMap.IndexOf(up.channel);
          if(idx < 0) {
            idx = uMap.Count;
            uMap.Add(up.channel);
          }
          up.mapping = idx;
        }
      }
      #endregion UART mapping

      #region export .xst
      XDocument doc = new XDocument(new XElement("root", new XAttribute("head", "/etc/declarers/dev") ) );
      var dev=CreateXItem(name, "pack://application:,,/CC;component/Images/" + (_phy2 == null ? "ty_unode.png" : "ty_ugate.png"));
      dev.Add(new XAttribute("ver", (new Version(3, now.Year % 100, now.Month * 100 + now.Day))));

      doc.Root.Add(dev);
      var IP = new XElement("item", new XAttribute("name", "Digital inputs"));
      dev.Add(IP);
      var OP = new XElement("item", new XAttribute("name", "Digital outputs"));
      dev.Add(OP);
      var IN = new XElement("item", new XAttribute("name", "Digital inverted inputs"));
      dev.Add(IN);
      var ON = new XElement("item", new XAttribute("name", "Digital inverted outputs"));
      dev.Add(ON);
      var PP = new XElement("item", new XAttribute("name", "PWM"));
      dev.Add(PP);
      var PN = new XElement("item", new XAttribute("name", "PWM inverted"));
      dev.Add(PN);
      XElement ain;
      if(_ainRef != 0) {
        ain = new XElement("item", new XAttribute("name", "Analog inputs"));
        dev.Add(ain);
      } else {
        ain = null;
      }
      Pin twi_sda=null;
      Pin twi_scl=null;
      foreach(var p in pins) {
        p.ExportX(Section.IP, IP);
        p.ExportX(Section.OP, OP);
        p.ExportX(Section.IN, IN);
        p.ExportX(Section.ON, ON);
        p.ExportX(Section.PP, PP);
        p.ExportX(Section.PN, PN);
        if((_ainRef & 1) == 1) {
          p.ExportX(Section.Ae, ain);
        }
        if((_ainRef & 2) == 2) {
          p.ExportX(Section.Av, ain);
        }
        if((_ainRef & 4) == 4) {
          p.ExportX(Section.Ai, ain);
        }
        if((_ainRef & 8) == 8) {
          p.ExportX(Section.AI, ain);
        }
        p.ExportX(Section.Serial, dev);
        if(p.twiCur.signal == Signal.TWI_SDA) {
          twi_sda = p;
        } else if(p.twiCur.signal == Signal.TWI_SCL) {
          twi_scl = p;
        }
      }
      if(twi_sda != null && twi_scl != null) {
        var twi1 = new XElement("item", new XAttribute("name", "TWI"));
        int r1 = ExIndex(twi_sda.name + "_used");
        int r2 = ExIndex(twi_scl.name + "_used");
        var twi2 = CreateXItem("Ta0", "ZbX" + r1.ToString() + ", X" + r2.ToString());
        twi2.Add(CreateXItem("_description", "TWI devices"));
        twi1.Add(twi2);
        dev.Add(twi1);
      }
      var add = new XElement("item", new XAttribute("name", "Add"));
      add.Add(CreateXItem("bool", "yZ"));
      add.Add(CreateXItem("long", "yI"));
      add.Add(CreateXItem("string", "yS"));
      add.Add(CreateXItem("Byte array", "yB"));
      dev.Add(add);
      dev.Add(CreateXItem("remove", "zD"));
      using(StreamWriter writer = File.CreateText(System.IO.Path.ChangeExtension(_prjPath, "xst"))) {
        doc.Save(writer);
      }
      #endregion export .xst

      #region export .h
      var h_sb = new StringBuilder();

      h_sb.AppendLine("// This file is part of the https://github.com/X13home project.");
      h_sb.AppendFormat("\r\n#ifndef _{0}_H\r\n#define _{0}_H\r\n\r\n", name);
      h_sb.AppendFormat("// Board: {0}\r\n", name);
      h_sb.AppendFormat("// uC: {0}\r\n", System.IO.Path.GetFileNameWithoutExtension(_cpuPath));
      if(_phy1 != null) {
        h_sb.AppendFormat("// PHY1: {0}\r\n", _phy1.name);
      }
      if(_phy2 != null) {
        h_sb.AppendFormat("// PHY2: {0}\r\n", _phy2.name);
      }
      h_sb.AppendLine();
      string tmp;
      Port port=null;
      foreach(var p in pins.OrderBy(z => z.name)) {
        if(p.port != port) {
          port = p.port;
          if(port != null) {
            h_sb.Append("//\t" + port.name + "\r\n");
          }
        }
        tmp = p.ExportPinOut();
        if(tmp != null) {
          h_sb.Append(tmp);
        }
      }

      //================================================= 
      h_sb.AppendFormat("\r\n#endif //_{0}_H\r\n", name);
      
      File.WriteAllText(System.IO.Path.ChangeExtension(_prjPath, "h"), h_sb.ToString());
      h_sb = null;
      #endregion export .h

    }


    public int ExIndex(string name) {
      int r = _exResouces.IndexOf(name);
      if(r < 0) {
        r = _exResouces.Count;
        _exResouces.Add(name);
      }
      return r+1;
    }

  }
  internal enum Section {
    IP,
    IN,
    OP,
    ON,
    PP,
    PN,
    Ae,
    Av,
    Ai,
    AI,
    Serial,
  }
}
