using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class Port {
    public static IEnumerable<Pin> CreatePort(Project owner, XElement info) {
      var p = new Port();
      p.name = info.Attribute("name").Value;
      p.offset = int.Parse(info.Attribute("offset").Value);
      p.nr = int.Parse(info.Attribute("nr").Value);
      p.pinset = info.Attribute("pinset").Value;
      p.pinrst = info.Attribute("pinrst").Value;
      p.pinget = info.Attribute("pinget").Value;
      var xn = info.Attribute("titel");
      p.titel = (xn != null && !string.IsNullOrEmpty(xn.Value)) ? xn.Value : p.name;
      return info.Elements("pin").Select(z => new Pin(owner, z, p));

    }
    public string name { get; private set; }
    public int offset { get; private set; }
    public int nr { get; private set; }
    public string titel { get; private set; }
    public string pinset { get; private set; }
    public string pinrst { get; private set; }
    public string pinget { get; private set; }

  }
}
