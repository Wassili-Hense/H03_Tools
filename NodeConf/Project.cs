﻿using System;
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
      dev.Add(new XAttribute("type", "System.String"));
      return dev;
    }

    public static Project LoadFromCpu(string path, bool init = true) {
      var p = new Project();
      p._cpuPath = path;
      try {
        XDocument doc = XDocument.Load(System.IO.Path.GetFullPath(@".\cpu\" + p._cpuPath + ".xml"));
        p._cpuSignature = doc.Root.Attribute("name").Value;
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
      _exResouces = new List<string>();
      XDocument doc = new XDocument(new XElement("root", new XAttribute("head", "/etc/declarers/dev") ) );
      string name=System.IO.Path.GetFileNameWithoutExtension(this.Path);
      if(name.Length > 6) {
        name = name.Substring(0, 6);
      }
      var dev=CreateXItem(name, "pack://application:,,/CC;component/Images/" + (_phy2 == null ? "ty_unode.png" : "ty_ugate.png"));
      var now = DateTime.Now;
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
      foreach(var p in pins) {
        p.ExportX(Section.IP, IP);
        p.ExportX(Section.OP, OP);
        p.ExportX(Section.IN, IN);
        p.ExportX(Section.ON, ON);
        p.ExportX(Section.PP, PP);
        p.ExportX(Section.PN, PN);
      }
      dev.Add(CreateXItem("remove", "zD"));
      using(StreamWriter writer = File.CreateText(System.IO.Path.ChangeExtension(_prjPath, "xst"))) {
        doc.Save(writer);
      }

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
  }
}
