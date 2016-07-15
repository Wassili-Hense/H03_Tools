using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class Project {
    public static Project LoadFromCpu(string path) {
      var p = new Project();
      p._cpuPath = path;
      try {
        XDocument doc = XDocument.Load(System.IO.Path.GetFullPath(@".\cpu\" + p._cpuPath + ".xml"));
        p._cpuSignature = doc.Root.Attribute("name").Value;
        List<Pin> pins = new List<Pin>();
        pins.AddRange(doc.Root.Elements("port").SelectMany(z => Port.CreatePort(p, z)));
        pins.AddRange(doc.Root.Elements("pin").Select(z => new Pin(p, z, null)));

        p.pins = pins.ToArray();
        p.SetSysEntrys();
      }
      catch(Exception ex) {
        Console.WriteLine(ex.ToString());
        return null;
      }
      return p;
    }

    private string _prjPath;
    private string _cpuPath;
    private string _cpuSignature;
    private phyBase _phy1;
    private phyBase _phy2;

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
  }
}
