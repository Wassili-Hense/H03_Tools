///<remarks>This file is part of the <see cref="https://github.com/X13home">X13.Home</see> project.<remarks>
using JSC = NiL.JS.Core;
using JSL = NiL.JS.BaseLibrary;
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
        p.ainRef = int.Parse(doc.Root.Attribute("ainref").Value.Substring(2), System.Globalization.NumberStyles.HexNumber);
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
        string name = it.Attribute("name").Value;
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
    public string cpu { get { return _cpuPath; } }
    public phyBase phy1 { 
      get { 
        return _phy1; 
      } 
      set { 
        _phy1 = value; 
        _prjPath = null;
        foreach(var p in pins) {
          if(p.config == PinCfg.Phy1) {
            p.config = PinCfg.None;
            p.ViewChanged();
          }
        }
      } 
    }
    public phyBase phy2 { 
      get { 
        return _phy2; 
      } 
      set { 
        _phy2 = value; 
        _prjPath = null;
        foreach(var p in pins) {
          if(p.config == PinCfg.Phy2) {
            p.config = PinCfg.None;
            p.ViewChanged();
          }
        }
      } 
    }
    public int ainRef { get; private set; }

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
          p.systemCur = e;
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
      string name = System.IO.Path.GetFileNameWithoutExtension(this.Path);
      string ePath = "export//" + name + "//";
      if(!Directory.Exists(ePath)) {
        Directory.CreateDirectory(ePath);
      }

      var now = DateTime.Now;

      _exResouces = new List<string>();
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

      Pin twi_sda = null;
      Pin twi_scl = null;
      enAin cAin;

      #region export .xst v0.3
      XDocument doc = new XDocument(new XElement("root", new XAttribute("head", "/etc/declarers/dev")));
      var dev = CreateXItem(name, "pack://application:,,/CC;component/Images/" + (_phy2 == null ? "ty_unode.png" : "ty_ugate.png"));
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
      if(ainRef != 0) {
        ain = new XElement("item", new XAttribute("name", "Analog inputs"));
        dev.Add(ain);
      } else {
        ain = null;
      }
      foreach(var p in pins) {
        p.ExportX(Section.IP, IP);
        p.ExportX(Section.OP, OP);
        p.ExportX(Section.IN, IN);
        p.ExportX(Section.ON, ON);
        p.ExportX(Section.PP, PP);
        p.ExportX(Section.PN, PN);
        cAin = p.ainCur as enAin;
        if(cAin != null) {
          if((cAin.ainRef & 1) == 1) {
            p.ExportX(Section.Ae, ain);
          }
          if((cAin.ainRef & 2) == 2) {
            p.ExportX(Section.Av, ain);
          }
          if((cAin.ainRef & 4) == 4) {
            p.ExportX(Section.Ai, ain);
          }
          if((cAin.ainRef & 8) == 8) {
            p.ExportX(Section.AI, ain);
          }
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
      using(StreamWriter writer = File.CreateText(ePath+name+"_v03.xst")) {
        doc.Save(writer);
      }
      #endregion export .xst

      #region export .xst v0.4
      doc = new XDocument(new XElement("xst", new XAttribute("path", "/$YS/TYPES/MQTT-SN")));
      doc.Declaration = new XDeclaration("1.0", "utf-8", "yes");
      JSC.JSObject val = JSC.JSObject.CreateObject();
      val["editor"] = "Enum";
      val["enum"] = "MsStatus";
      JSC.JSObject children = JSC.JSObject.CreateObject(), t1 = JSC.JSObject.CreateObject(), t2 = JSC.JSObject.CreateObject(), mqi = JSC.JSObject.CreateObject();
      val["Children"] = children;
      val["mi"] = t1;
      t1["MQTT-SN"] = t2;
      t2["mi"] = mqi;
      if(ainRef != 0) {
        t1 = JSC.JSObject.CreateObject();
        t1["default"] = 0;
        t1["editor"] = "Integer";
        mqi["ADCintegrate"] = t1;
      }
      if(_phy1 != null) {
        t1 = JSC.JSObject.CreateObject();
        t1["attr"] = 3;
        mqi["phy1_addr"] = t1;
      }
      if(_phy2 != null) {
        t1 = JSC.JSObject.CreateObject();
        t1["attr"] = 3;
        mqi["phy2_addr"] = t1;
      }
      var lc = new Dictionary<string, JSC.JSObject>();
      foreach(var p in pins) {
        p.ExportX04(lc);
        //p.ExportX(Section.Serial, dev);
        if(p.twiCur.signal == Signal.TWI_SDA) {
          twi_sda = p;
        } else if(p.twiCur.signal == Signal.TWI_SCL) {
          twi_scl = p;
        }
      }
      /*
      if(twi_sda != null && twi_scl != null) {
        var twi1 = new XElement("item", new XAttribute("name", "TWI"));
        int r1 = ExIndex(twi_sda.name + "_used");
        int r2 = ExIndex(twi_scl.name + "_used");
        var twi2 = CreateXItem("Ta0", "ZbX" + r1.ToString() + ", X" + r2.ToString());
        twi2.Add(CreateXItem("_description", "TWI devices"));
        twi1.Add(twi2);
        dev.Add(twi1);
      }
      */
      foreach(var c in lc.OrderBy(z => z.Key).ThenBy(z => z.Value["menu"].Value as string ?? string.Empty)) {
        children[c.Key] = c.Value;
      }
      dev = new XElement("i");
      dev.Add(new XAttribute("n", name));
      dev.Add(new XAttribute("s", JSL.JSON.stringify(val, null, null)));
      dev.Add(new XAttribute("m", "{\"attr\":4}"));
      dev.Add(new XAttribute("ver", ( new Version(3, now.Year % 100, now.Month * 100 + now.Day) )));
      doc.Root.Add(dev);

      using(System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(ePath + name + "_v04.xst", Encoding.UTF8)) {
        writer.Formatting = System.Xml.Formatting.Indented;
        writer.QuoteChar = '\'';
        doc.WriteTo(writer);
        writer.Flush();
      }

      #endregion export .xst V0.4

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
      Port port = null;
      foreach(var p in pins.OrderBy(z => z.name)) {
        if(p.port != port) {
          port = p.port;
          if(port != null) {
            h_sb.Append("// == " + port.name + "\r\n");
          }
        }
        tmp = p.ExportPinOut();
        if(tmp != null) {
          h_sb.Append(tmp);
        }
      }
      h_sb.Append("\r\n#ifdef __cplusplus\r\nextern \"C\" {\r\n#endif\r\n");
      {
        foreach(var p in pins.Where(z => z.systemCur.type == EntryType.system).Select(z => z.systemCur as enSystem).Where(z => z.src != null)) {
          h_sb.AppendLine();
          h_sb.AppendLine(p.src);
        }
      }
      {  // DIO
        int cur_m = 0;
        bool dio_used = false;
        int dio_port_min = 255;
        int dio_port_max = -1;
        var dio_sb = new StringBuilder();
        dio_sb.Append("#define HAL_DIO_MAPPING             {");
        foreach(var p in pins.Where(z => z.port != null && z.mapping >= 0 && z.entrys.Any(y => y.type == EntryType.dio)).OrderBy(z => z.mapping)) {
          dio_used = true;
          while(p.mapping > cur_m) {
            dio_sb.Append("0xFF, ");
            cur_m++;
          }
          dio_sb.AppendFormat("{0}, ", p.port.offset + p.idx);
          cur_m++;
          if(p.port.nr < dio_port_min) {
            dio_port_min = p.port.nr;
          }
          if(p.port.nr > dio_port_max) {
            dio_port_max = p.port.nr;
          }
        }
        if(dio_used) {
          h_sb.Append("\r\n// DIO Section\r\n#define EXTDIO_USED                 1\r\n");
          if(dio_port_min > 0) {
            h_sb.AppendFormat("#define EXTDIO_PORT_OFFSET          {0}\r\n", dio_port_min);
          }
          h_sb.AppendFormat("#define EXTDIO_MAXPORT_NR           {0}\r\n", dio_port_max - dio_port_min + 1);
          dio_sb.Remove(dio_sb.Length - 2, 2);
          dio_sb.Append("}");
          h_sb.AppendLine(dio_sb.ToString());
          h_sb.AppendLine("// End DIO Section");
        }
      }
      {  //PWM
        int pwm_min = 255;
        var pwm_sb = new StringBuilder();
        int cur_m = 0;
        pwm_sb.Append("#define HAL_PWM_PORT2CFG            {");

        var lst = pins.Where(z => z.mapping >= 0 && z.pwmCur.type == EntryType.pwm).OrderBy(z => z.mapping).Select(z => new Tuple<int, int>(z.mapping, (z.pwmCur as enPwm).GetConfig())).ToArray();
        if(lst.Length > 0) {
          pwm_min = lst.Min(z => z.Item1);
          cur_m = pwm_min;
          foreach(var p in lst) {
            while(p.Item1 > cur_m) {
              pwm_sb.Append("0xFF, ");
              cur_m++;
            }
            pwm_sb.AppendFormat("0x{0:X2}, ", p.Item2);
            cur_m++;
          }

          h_sb.Append("\r\n// PWM Section\r\n#define EXTPWM_USED                 1\r\n");
          if(pwm_min > 0) {
            h_sb.AppendFormat("#define HAL_PWM_BASE_OFFSET         {0}\r\n", pwm_min);
          }
          pwm_sb.Remove(pwm_sb.Length - 2, 2);
          pwm_sb.Append("}");
          h_sb.AppendLine(pwm_sb.ToString());
          h_sb.AppendLine("// End PWM Section");
        }
      }
      {  // AIN
        int ain_max;
        var ain_sb = new StringBuilder();
        int cur_m = 0;
        ain_sb.Append("#define HAL_AIN_BASE2APIN           {");
        var lst = pins.Where(z => z.mapping >= 0 && z.ainCur.type == EntryType.ain).OrderBy(z => z.mapping).Select(z => new Tuple<int, int>(z.mapping, (z.ainCur as enAin).GetConfig())).ToArray();
        if(lst.Length > 0) {
          ain_max = lst.Max(z => z.Item2) + 1;
          foreach(var p in lst) {
            while(p.Item1 > cur_m) {
              ain_sb.Append("0xFF, ");
              cur_m++;
            }
            ain_sb.AppendFormat("{0}, ", p.Item2);
            cur_m++;
          }

          h_sb.Append("\r\n// Analogue Inputs\r\n#define EXTAIN_USED                 1\r\n");
          h_sb.AppendFormat("#define EXTAIN_MAXPORT_NR           {0}\r\n", ain_max);
          ain_sb.Remove(ain_sb.Length - 2, 2);
          ain_sb.Append("}");
          h_sb.AppendLine(ain_sb.ToString());
          h_sb.AppendLine("// End Analogue Inputs");
        }
      }
      {  //UART
        var lst = pins.Where(z => z.mapping >= 0 && z.serialCur.signal == Signal.UART_TX).Select(z => z.serialCur as enSerial).ToArray();
        int sp_cnt = pins.Where(z => z.phy1Cur.signal == Signal.UART_TX || z.phy2Cur.signal == Signal.UART_TX).Count();
        h_sb.AppendLine("\r\n// UART Section");
        if(lst.Length > 0) {
          foreach(var p in lst) {
            h_sb.AppendFormat("#define HAL_USE_USART{0}              {1}\r\n", p.channel, p.mapping);
            if(p.config != 0) {
              h_sb.AppendFormat("#define HAL_USART{1}_REMAP            {0}\r\n", p.config, p.channel);
            }
          }
          h_sb.AppendFormat("#define EXTSER_USED                 {0}\r\n", lst.Length);
          h_sb.AppendFormat("#define HAL_UART_NUM_PORTS          {0}\r\n", lst.Length + sp_cnt);
        } else if(sp_cnt > 0) {
          h_sb.AppendFormat("#define HAL_UART_NUM_PORTS          {0}\r\n", sp_cnt);
        }
        h_sb.AppendLine("// End UART Section");

      }
      {  // TWI
        var p = pins.FirstOrDefault(z => z.twiCur.signal == Signal.TWI_SDA);
        enTwi twi;
        if(p != null && (twi = p.twiCur as enTwi) != null) {
          h_sb.AppendLine("\r\n// TWI Section");
          h_sb.AppendFormat("#define HAL_TWI_BUS                 {0}\r\n", twi.channel);
          if(twi.config != 0) {
            h_sb.AppendFormat("#define HAL_TWI_REMAP               {0}\r\n", twi.config);
          }
          h_sb.AppendLine("#define EXTTWI_USED                 1");
          h_sb.AppendLine("// End TWI Section");
        }
      }
      {  // LED
        var p = pins.FirstOrDefault(z => z.systemCur.signal == Signal.SYS_LED);
        if(p != null) {
          var l = p.systemCur as enSysLed;
          if(l != null && p.port!=null) {
            h_sb.AppendLine();
            h_sb.AppendFormat("#define LED_Init()                  hal_dio_configure({0}, DIO_MODE_OUT_PP)\r\n", p.port.offset + p.idx);
            if(l.pnp) {
              h_sb.AppendFormat("#define LED_On()                    {0}\r\n", string.Format(p.port.pinset, p.idx));
              h_sb.AppendFormat("#define LED_Off()                   {0}\r\n", string.Format(p.port.pinrst, p.idx));
            } else {
              h_sb.AppendFormat("#define LED_On()                    {0}\r\n", string.Format(p.port.pinrst, p.idx));
              h_sb.AppendFormat("#define LED_Off()                   {0}\r\n", string.Format(p.port.pinset, p.idx));
            }
          }
        }
      }
      if(_phy1 != null) {
        h_sb.Append(_phy1.ExportH());
      }
      if(_phy2 != null) {
        h_sb.Append(_phy2.ExportH());
      }
      { // Object's Dictionary Section
        h_sb.AppendLine("\r\n// Object's Dictionary Section");
        int od_cnt = pins.Where(z => z.mapping >= 0 || z.twiCur.signal == Signal.TWI_SDA).Count();
        {
          var plc_p = pins.FirstOrDefault(z=>z.config==PinCfg.System && z.name.ToUpper()=="PLC");
          enSystem plc_e;
          int plc_val;
          if(plc_p != null && (plc_e=plc_p.systemCur as enSystem)!=null && int.TryParse(plc_e.name, out plc_val)) {
            od_cnt += plc_val;
          }
        }
        h_sb.AppendFormat("#define OD_MAX_INDEX_LIST           {0}\r\n", od_cnt);
        string pn = System.IO.Path.GetFileNameWithoutExtension(_prjPath);
        h_sb.AppendFormat("#define OD_DEV_UC_TYPE              '{0}'\r\n", pn.Length > 0 ? pn[0] : _cpuSignature[0]);
        h_sb.AppendFormat("#define OD_DEV_UC_SUBTYPE           '{0}'\r\n", pn.Length > 1 ? pn[1] : _cpuSignature[1]);
        h_sb.AppendFormat("#define OD_DEV_PHY1                 '{0}'\r\n", pn.Length > 2 ? pn[2] : ' ');
        h_sb.AppendFormat("#define OD_DEV_PHY2                 '{0}'\r\n", pn.Length > 3 ? pn[3] : ' ');
        h_sb.AppendFormat("#define OD_DEV_HW_TYP_H             '{0}'\r\n", pn.Length > 4 ? pn[4] : '-');
        h_sb.AppendFormat("#define OD_DEV_HW_TYP_L             '{0}'\r\n", pn.Length > 5 ? pn[5] : '-');
        h_sb.AppendLine("// End Object's Dictionary Section");
      }
      //================================================= 
      h_sb.Append("\r\n#ifdef __cplusplus\r\n}\r\n#endif\r\n");
      h_sb.AppendFormat("\r\n#endif //_{0}_H\r\n", name);

      File.WriteAllText(ePath + name + ".h", h_sb.ToString());
      h_sb = null;
      #endregion export .h

    }

    public int ExIndex(string name) {
      int r = _exResouces.IndexOf(name);
      if(r < 0) {
        r = _exResouces.Count;
        _exResouces.Add(name);
      }
      return r + 1;
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
