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
        XDocument doc = XDocument.Load(System.IO.Path.GetFullPath( @".\cpu\"+p._cpuPath+".xml"));
        p._cpuSignature = doc.Root.Attribute("name").Value;
        p.pins = doc.Root.Elements("item").Select(z => new Pin(z)).ToArray();
      }
      catch(Exception) {
        return null;
      }
      return p;
    }

    private string _prjPath;
    private string _cpuPath;
    private string _cpuSignature;
    public Pin[] pins { get; private set; }


    public string Path {
      get {
        if(_prjPath == null) {
          string p = _cpuSignature + "nn";
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
  }
}
