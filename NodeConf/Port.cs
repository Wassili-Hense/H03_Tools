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

      return info.Elements("pin").Select(z => new Pin(owner, z, p));

    }
    public string name { get; private set; }
    public int offset { get; private set; }
    public int nr { get; private set; }
  }
}
