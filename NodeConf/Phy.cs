﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class phyBase {
    public static phyBase Create(string name, int nr) {
      if(name == "none") {
        return null;
      }
      var p = new phyBase();
      p._nr = nr;
      try {
        XDocument doc = XDocument.Load(System.IO.Path.GetFullPath(@".\phy\" + name + ".xml"));
        p.signature = doc.Root.Attribute("signature").Value;
        p.name = doc.Root.Attribute("name").Value;
        p._pins = doc.Root.Elements("pin").Select(z => new phyPin(z)).ToArray();
        var ex=new List<Tuple<string, string[]>>();
        foreach(var a in doc.Root.Elements("append")) {
          ex.Add(new Tuple<string, string[]>(a.Attribute("fmt").Value, a.Elements("var").Select(z=>z.Attribute("name").Value).ToArray()));
        }
        p._exportH = ex.ToArray();
      }
      catch(Exception ex) {
        Console.WriteLine(ex.ToString());
        return null;
      }

      return p;
    }

    private int _nr;
    private phyPin[] _pins;
    private Tuple<string, string[]>[] _exportH;

    public string signature { get; private set; }
    public string name { get; private set; }

    public List<enBase> GetLst(Pin pin) {
      // SPI
      bool spi_defined = false;
      bool spi_hw_nss = pin._owner.pins.SelectMany(z => z.entrys).Where(z => z.signal == Signal.SPI_NSS).OfType<enSpi>().Any();
      int spi_channel = 0;
      int spi_config = 0;
      enSpi sp;

      for(int i = 0; i < _pins.Length; i++) {
        if(_pins[i].pin != null && _pins[i].pin.type == EntryType.spi) {
          sp = _pins[i].pin as enSpi;
          spi_defined = true;
          spi_channel = sp.channel;
          spi_config = sp.config;
          break;
        }
      }

      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => pin._owner.EntryIsEnabled(z))) {
        if(_pins.Any(z => z.pin == en || (z.pin == null && (en.signal == z.signal || (z.signal == Signal.SPI_NSS && !spi_hw_nss && en.signal == Signal.DIO))))) {
          if(en.type == EntryType.spi && spi_defined && ((en as enSpi).channel != spi_channel || (en as enSpi).config != spi_config)) {
            continue;
          }
          lst.Add(en);
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => _pins.Any(z1 => z1.pin == z));
      return en ?? enBase.none;
    }
    public bool SetCur(Pin pin, enBase en, string func) {
      var op = _pins.FirstOrDefault(z => pin.entrys.Any(z1 => z1 == z.pin));
      if(en != null && (op == null ?en.signal!=Signal.NONE:op.pin != en)) {
        if(op != null) {
          op.pin.selected = false;
          op.pin.resouces[pin.name + "_used"] = RcUse.Shared;
          op.pin.func = null;
          op.pin = null;
        }
        op = _pins.FirstOrDefault(z => z.pin == null && (z.signal == en.signal
          || (z.signal == Signal.SPI_NSS && !pin._owner.pins.SelectMany(z1 => z1.entrys).Where(z1 => z1.signal == Signal.SPI_NSS).OfType<enSpi>().Any() && en.signal == Signal.DIO)));
        if(op != null) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          op.pin = en;
          en.func = op.name;
        }
        return true;
      }
      return false;
    }
    public string ExportH() {
      if(_pins.Any(z => z.pin == null && !z.optional)) {
        return "\r\n#error "+ name +"_PHY" + _nr.ToString() + "_NOT_CONFIGURED\r\n";
      }
      // SPI
      var vars = new Dictionary<string, string>();
      bool spi_defined = false;
      enSpi sp;

      for(int i = 0; i < _pins.Length; i++) {
        if(_pins[i].pin != null && _pins[i].pin.type == EntryType.spi) {
          sp = _pins[i].pin as enSpi;
          spi_defined = true;
          vars["spi.channel"] = sp.channel.ToString();
          vars["spi.config"] = sp.config.ToString();
          break;
        }
      }


      var sb = new StringBuilder();

      sb.AppendFormat("\r\n//{0} PHY{1} Section\t", name, _nr);
      for(int i = 0; i < _pins.Length; i++) {
        if(_pins[i].pin == null) {
          continue;
        }
        if(i>0){
          sb.Append(",");
        }
        sb.AppendFormat(" {0}: {1}", _pins[i].name ?? _pins[i].signal.ToString(), _pins[i].pin.parent.name);
      }
      sb.AppendLine();
      if(spi_defined) {
        sb.AppendFormat("#define HAL_USE_SPI{0}              {1}\r\n", vars["spi.channel"], vars["spi.config"]);
      }

      sb.AppendFormat("//End {0} PHY{1} Section\r\n", name, _nr);

      return sb.ToString();
    }
    private class phyPin {
      public phyPin(XElement info) {
        var xn = info.Attribute("signal");
        Signal s;
        if(xn != null && Enum.TryParse<Signal>(info.Attribute("signal").Value, out s)) {
          this.signal = s;
          this.type = (EntryType)((int)s >> 2);
        } else {
          throw new ArgumentException("unknown signal in " + info.ToString(SaveOptions.DisableFormatting));
        }
        xn = info.Attribute("name");
        if(xn != null) {
          this.name = xn.Value;
        }
        xn = info.Attribute("optional");
        this.optional = xn != null && xn.Value == "true";
      }
      public readonly EntryType type;
      public readonly Signal signal;
      public readonly string name;
      public readonly bool optional;
      public enBase pin;

    }
  }
  /*
  internal class phySerial : phyBase {
    private enSerial _tx;
    private enSerial _rx;

    public phySerial(int nr) {
      signature = "S";
      name = "UART";
      this._nr = nr;
    }

    public override List<enBase> GetLst(Pin pin) {
      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.serial && pin._owner.EntryIsEnabled(z)).OfType<enSerial>()) {
        if(en == _rx
          || en == _tx
          || ((en.signal == Signal.UART_RX && _rx == null) && (_tx == null || (en.channel == _tx.channel && en.config == _tx.config)))
          || ((en.signal == Signal.UART_TX && _tx == null) && (_rx == null || (en.channel == _rx.channel && en.config == _rx.config)))) {
          lst.Add(en);
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public override enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => z == _tx || z == _rx);
      return en ?? enBase.none;
    }
    public override bool SetCur(Pin pin, enBase en, string func) {
      var old = GetCur(pin);
      if(en != null && old != en) {
        if(old.type == EntryType.serial) {
          old.selected = false;
          old.resouces[pin.name + "_used"] = RcUse.Shared;
          if(_rx == old) {
            _rx = null;
          } else if(_tx == old) {
            _tx = null;
          }
        }
        if(en.type == EntryType.serial) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          if(en.signal == Signal.UART_RX) {
            _rx = en as enSerial;
          } else if(en.signal == Signal.UART_TX) {
            _tx = en as enSerial;
          }
        }
        return true;
      }
      return false;

    }
    public override string ExportH() {
      if(_tx == null || _rx == null) {
        return "\r\n//UART PHY"+_nr.ToString()+" NOT CONFIGURED\r\n";
      }
      var sb = new StringBuilder();

      sb.AppendFormat("\r\n//UART PHY{0} Section\tTX: {2}, RX: {1}\r\n", _nr, _rx.parent.name, _tx.parent.name);
      sb.AppendFormat("#define HAL_USE_USART{0}            {1}\r\n", _tx.channel, _tx.mapping);
      if(_tx.config != 0) {
        sb.AppendFormat("#define HAL_USART{1}_REMAP          {0}\r\n", _tx.config, _tx.channel);
      }
      sb.AppendFormat("#define UART_PHY_PORT               {0}\r\n", _tx.mapping);
      sb.AppendFormat("#define UART_PHY                    {0}\r\n", _nr);
      sb.AppendLine("#include \"PHY/UART/uart_phy.h\"");
      sb.AppendFormat("//End UART PHY{0} Section\r\n", _nr);

      return sb.ToString();
    }
  }
  internal class phyRS485 : phyBase {
    private enSerial _tx;
    private enSerial _rx;
    private enSerial _de;

    public phyRS485(int nr) {
      signature = "M";
      name = "RS485";
      this._nr = nr;
    }

    public override List<enBase> GetLst(Pin pin) {
      bool defined = false;
      int channel = 0;
      int config = 0;
      if(_tx != null) {
        defined = true;
        channel = _tx.channel;
        config = _tx.config;
      } else if(_rx != null) {
        defined = true;
        channel = _rx.channel;
        config = _rx.config;
      } else if(_de != null) {
        defined = true;
        channel = _de.channel;
        config = _de.config;
      }
      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.serial && pin._owner.EntryIsEnabled(z)).OfType<enSerial>().Where(z => !defined || (z.channel == channel && z.config == config))) {
        if(en == _rx || en == _tx || en == _de || (en.signal == Signal.UART_RX && _rx == null) || (en.signal == Signal.UART_TX && _tx == null) || (en.signal == Signal.UART_DE && _de == null)) {
          lst.Add(en);
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public override enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => z == _tx || z == _rx || z == _de);
      return en ?? enBase.none;
    }
    public override bool SetCur(Pin pin, enBase en, string func) {
      var old = GetCur(pin);
      if(en != null && old != en) {
        if(old.type == EntryType.serial) {
          old.selected = false;
          old.resouces[pin.name + "_used"] = RcUse.Shared;
          if(_rx == old) {
            _rx = null;
          } else if(_tx == old) {
            _tx = null;
          } else if(_de == old) {
            _de = null;
          }
        }
        if(en.type == EntryType.serial) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          if(en.signal == Signal.UART_RX) {
            _rx = en as enSerial;
          } else if(en.signal == Signal.UART_TX) {
            _tx = en as enSerial;
          } else if(en.signal == Signal.UART_DE) {
            _de = en as enSerial;
          }
        }
        return true;
      }
      return false;
    }
    public override string ExportH() {
      if(_tx == null || _rx == null || _de==null) {
        return "\r\n//RS485 PHY" + _nr.ToString() + " NOT CONFIGURED\r\n";
      }
      var sb = new StringBuilder();

      sb.AppendFormat("\r\n//RS485 PHY{0} Section\tTX: {2}, RX: {1}, DE: {3}\r\n", _nr, _rx.parent.name, _tx.parent.name, _de.parent.name);
      sb.AppendFormat("#define HAL_USE_USART{0}            {1}\r\n", _tx.channel, _tx.mapping);
      if(_tx.config != 0) {
        sb.AppendFormat("#define HAL_USART{1}_REMAP          {0}\r\n", _tx.config, _tx.channel);
      }
      sb.AppendFormat("#define RS485_PHY_PORT              {0}\r\n", _tx.mapping);
      sb.AppendFormat("#define RS485_PHY                   {0}\r\n", _nr);
      sb.AppendLine("#include \"PHY/RS485/rs485_phy.h\"");
      sb.AppendFormat("//End UART PHY{0} Section\r\n", _nr);

      return sb.ToString();
    }
  }
  internal class phyCC1101 : phyBase {
    private enSpi _mosi;
    private enSpi _miso;
    private enSpi _sck;
    private enBase _nss;

    public phyCC1101(int nr) {
      signature = "C";
      name = "CC1101";
      this._nr = nr;
    }

    public override List<enBase> GetLst(Pin pin) {
      bool defined = false;
      int channel = 0;
      int config = 0;
      if(_mosi != null) {
        defined = true;
        channel = _mosi.channel;
        config = _mosi.config;
      } else if(_miso != null) {
        defined = true;
        channel = _miso.channel;
        config = _miso.config;
      } else if(_sck != null) {
        defined = true;
        channel = _sck.channel;
        config = _sck.config;
      } else if(_nss != null && _nss.signal == Signal.SPI_NSS) {
        defined = true;
        channel = (_nss as enSpi).channel;
        config = (_nss as enSpi).config;
      }


      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.spi && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config))) {
        if(en == _mosi || (en.signal == Signal.SPI_MOSI && _mosi == null)
           || en == _miso || (en.signal == Signal.SPI_MISO && _miso == null)
           || en == _sck || (en.signal == Signal.SPI_SCK && _sck == null)
           || en == _nss || (en.signal == Signal.SPI_NSS && _nss == null)) {
          lst.Add(en);
        }
      }
      if(!pin._owner.pins.SelectMany(z => z.entrys).Where(z => z.signal == Signal.SPI_NSS && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config)).Any()) {
        foreach(var en in pin.entrys.Where(z => z.type == EntryType.dio)) {
          if(en == _nss || _nss == null) {
            lst.Add(en);
          }
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public override enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => z == _miso || z == _mosi || z == _sck || z == _nss);
      return en ?? enBase.none;
    }
    public override bool SetCur(Pin pin, enBase en, string func) {
      var old = GetCur(pin);
      if(en != null && old != en) {
        if(old.type != EntryType.none) {
          old.selected = false;
          old.resouces[pin.name + "_used"] = RcUse.Shared;
          if(_mosi == old) {
            _mosi = null;
          } else if(_miso == old) {
            _miso = null;
          } else if(_sck == old) {
            _sck = null;
          } else if(_nss == old) {
            if(_nss.type == EntryType.dio) {
              _nss.func = null;
            }
            _nss = null;
          }
        }
        if(en.type == EntryType.spi) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          if(en.signal == Signal.SPI_MOSI) {
            _mosi = en as enSpi;
          } else if(en.signal == Signal.SPI_MISO) {
            _miso = en as enSpi;
          } else if(en.signal == Signal.SPI_SCK) {
            _sck = en as enSpi;
          } else if(en.signal == Signal.SPI_NSS) {
            _nss = en;
          }
        } else if(en.type == EntryType.dio) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          _nss = en;
          _nss.func = "NSS";
        }
        return true;
      }
      return false;
    }
    public override string ExportH() {
      if(_mosi == null || _miso == null || _sck == null || _nss==null || _nss.parent.port==null) {
        return "\r\n//CC11 PHY" + _nr.ToString() + " NOT CONFIGURED\r\n";
      }
      var sb = new StringBuilder();

      sb.AppendFormat("\r\n//CC11 PHY{0} Section\tMOSI: {1}, MISO: {2}, SCK: {3}, NSS: {4}\r\n", _nr, _mosi.parent.name, _miso.parent.name, _sck.parent.name, _nss.parent.name);
      sb.AppendFormat("#define HAL_USE_SPI{0}              {1}\r\n", _mosi.channel, _mosi.config);
      sb.AppendFormat("#define CC11_USE_SPI                {0}\r\n", _mosi.channel);
      sb.AppendFormat("#define CC11_NSS_PIN                {0}\r\n", _nss.parent.idx + _nss.parent.port.offset);
      sb.AppendFormat("#define CC11_WAIT_LOW_MISO()        while({0})\r\n", string.Format(_miso.parent.port.pinget, _miso.parent.idx));
      sb.AppendFormat("#define CC11_SELECT()               {0}\r\n", string.Format(_nss.parent.port.pinrst, _nss.parent.idx));
      sb.AppendFormat("#define CC11_RELEASE()              {0}\r\n", string.Format(_nss.parent.port.pinset, _nss.parent.idx));
      sb.AppendFormat("#define CC11_PHY                    {0}\r\n", _nr);
      sb.AppendLine("#define CC11_DEFAULT_FREQ           868300000UL");
      sb.AppendLine("#include \"PHY/CC1101/cc11_phy.h\"");
      sb.AppendFormat("//End CC11 PHY{0} Section\r\n", _nr);

      return sb.ToString();
    }
  }
  internal class phyENC28J60 : phyBase {
    private enSpi _mosi;
    private enSpi _miso;
    private enSpi _sck;
    private enBase _nss;

    public phyENC28J60(int nr) {
      signature = "E";
      name = "ENC28J60";
      this._nr = nr;
    }

    public override List<enBase> GetLst(Pin pin) {
      bool defined = false;
      int channel = 0;
      int config = 0;
      if(_mosi != null) {
        defined = true;
        channel = _mosi.channel;
        config = _mosi.config;
      } else if(_miso != null) {
        defined = true;
        channel = _miso.channel;
        config = _miso.config;
      } else if(_sck != null) {
        defined = true;
        channel = _sck.channel;
        config = _sck.config;
      } else if(_nss != null && _nss.signal == Signal.SPI_NSS) {
        defined = true;
        channel = (_nss as enSpi).channel;
        config = (_nss as enSpi).config;
      }


      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.spi && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config))) {
        if(en == _mosi || (en.signal == Signal.SPI_MOSI && _mosi == null)
           || en == _miso || (en.signal == Signal.SPI_MISO && _miso == null)
           || en == _sck || (en.signal == Signal.SPI_SCK && _sck == null)
           || en == _nss || (en.signal == Signal.SPI_NSS && _nss == null)) {
          lst.Add(en);
        }
      }
      if(!pin._owner.pins.SelectMany(z => z.entrys).Where(z => z.signal == Signal.SPI_NSS && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config)).Any()) {
        foreach(var en in pin.entrys.Where(z => z.type == EntryType.dio)) {
          if(en == _nss || _nss == null) {
            lst.Add(en);
          }
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public override enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => z == _miso || z == _mosi || z == _sck || z == _nss);
      return en ?? enBase.none;
    }
    public override bool SetCur(Pin pin, enBase en, string func) {
      var old = GetCur(pin);
      if(en != null && old != en) {
        if(old.type != EntryType.none) {
          old.selected = false;
          old.resouces[pin.name + "_used"] = RcUse.Shared;
          if(_mosi == old) {
            _mosi = null;
          } else if(_miso == old) {
            _miso = null;
          } else if(_sck == old) {
            _sck = null;
          } else if(_nss == old) {
            if(_nss.type == EntryType.dio) {
              _nss.func = null;
            }
            _nss = null;
          }
        }
        if(en.type == EntryType.spi) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          if(en.signal == Signal.SPI_MOSI) {
            _mosi = en as enSpi;
          } else if(en.signal == Signal.SPI_MISO) {
            _miso = en as enSpi;
          } else if(en.signal == Signal.SPI_SCK) {
            _sck = en as enSpi;
          } else if(en.signal == Signal.SPI_NSS) {
            _nss = en;
          }
        } else if(en.type == EntryType.dio) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          _nss = en;
          _nss.func ="NSS";
        }
        return true;
      }
      return false;
    }
    public override string ExportH() {
      if(_mosi == null || _miso == null || _sck == null || _nss == null || _nss.parent.port == null) {
        return "\r\n//ENC26J60 PHY" + _nr.ToString() + " NOT CONFIGURED\r\n";
      }
      var sb = new StringBuilder();

      sb.AppendFormat("\r\n//ENC PHY{0} Section\tMOSI: {1}, MISO: {2}, SCK: {3}, NSS: {4}\r\n", _nr, _mosi.parent.name, _miso.parent.name, _sck.parent.name, _nss.parent.name);
      sb.AppendFormat("#define HAL_USE_SPI{0}              {1}\r\n", _mosi.channel, _mosi.config);
      sb.AppendFormat("#define ENC_USE_SPI                 {0}\r\n", _mosi.channel);
      sb.AppendFormat("#define ENC_NSS_PIN                 {0}\r\n", _nss.parent.idx + _nss.parent.port.offset);
      sb.AppendFormat("#define ENC_SELECT()                {0}\r\n", string.Format(_nss.parent.port.pinrst, _nss.parent.idx));
      sb.AppendFormat("#define ENC_RELEASE()               {0}\r\n", string.Format(_nss.parent.port.pinset, _nss.parent.idx));
      sb.AppendFormat("#define ENC_PHY                     {0}\r\n", _nr);
      sb.AppendLine("#define OD_DEF_DEV_MAC              {0x00,0x04,0xA3,0x00,0x00,0x05}   // MAC MSB->LSB");
      sb.AppendLine("#include \"PHY/ENC28J60/enc28j60_phy.h\"");
      sb.AppendFormat("//End ENC PHY{0} Section\r\n", _nr);

      return sb.ToString();
    }
  }
  internal class phyRFM69 : phyBase {
    private enSpi _mosi;
    private enSpi _miso;
    private enSpi _sck;
    private enBase _nss;
    private enDIO _irq;

    public phyRFM69(int nr) {
      signature = "Q";
      name = "RFM69";
      this._nr = nr;
    }

    public override List<enBase> GetLst(Pin pin) {
      bool defined = false;
      int channel = 0;
      int config = 0;
      if(_mosi != null) {
        defined = true;
        channel = _mosi.channel;
        config = _mosi.config;
      } else if(_miso != null) {
        defined = true;
        channel = _miso.channel;
        config = _miso.config;
      } else if(_sck != null) {
        defined = true;
        channel = _sck.channel;
        config = _sck.config;
      } else if(_nss != null && _nss.signal == Signal.SPI_NSS) {
        defined = true;
        channel = (_nss as enSpi).channel;
        config = (_nss as enSpi).config;
      }


      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.spi && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config))) {
        if(en == _mosi || (en.signal == Signal.SPI_MOSI && _mosi == null)
           || en == _miso || (en.signal == Signal.SPI_MISO && _miso == null)
           || en == _sck || (en.signal == Signal.SPI_SCK && _sck == null)
           || en == _nss || (en.signal == Signal.SPI_NSS && _nss == null)) {
          lst.Add(en);
        }
      }
      bool dio_nss = !pin._owner.pins.SelectMany(z => z.entrys).Where(z => z.signal == Signal.SPI_NSS && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config)).Any();
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.dio)) {
        if((dio_nss && (en == _nss || _nss == null))
          || en == _irq || _irq == null) {
          lst.Add(en);
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public override enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => z == _miso || z == _mosi || z == _sck || z == _nss || z == _irq);
      return en ?? enBase.none;
    }
    public override bool SetCur(Pin pin, enBase en, string func) {
      var old = GetCur(pin);
      if(en != null && old != en) {
        if(old.type != EntryType.none) {
          old.selected = false;
          old.resouces[pin.name + "_used"] = RcUse.Shared;
          if(_mosi == old) {
            _mosi = null;
          } else if(_miso == old) {
            _miso = null;
          } else if(_sck == old) {
            _sck = null;
          } else if(_nss == old) {
            if(_nss.type == EntryType.dio) {
              _nss.func = null;
            }
            _nss = null;
          } else if(_irq == old) {
            _irq.func = null;
            _irq = null;
          }
        }
        if(en.type == EntryType.spi) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          if(en.signal == Signal.SPI_MOSI) {
            _mosi = en as enSpi;
          } else if(en.signal == Signal.SPI_MISO) {
            _miso = en as enSpi;
          } else if(en.signal == Signal.SPI_SCK) {
            _sck = en as enSpi;
          } else if(en.signal == Signal.SPI_NSS) {
            _nss = en;
          }
        } else if(en.type == EntryType.dio) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;

          bool defined = false;
          int channel = 0;
          int config = 0;
          if(_mosi != null) {
            defined = true;
            channel = _mosi.channel;
            config = _mosi.config;
          } else if(_miso != null) {
            defined = true;
            channel = _miso.channel;
            config = _miso.config;
          } else if(_sck != null) {
            defined = true;
            channel = _sck.channel;
            config = _sck.config;
          } else if(_nss != null && _nss.signal == Signal.SPI_NSS) {
            defined = true;
            channel = (_nss as enSpi).channel;
            config = (_nss as enSpi).config;
          }
          bool dio_nss = !pin._owner.pins.SelectMany(z => z.entrys).Where(z => z.signal == Signal.SPI_NSS && pin._owner.EntryIsEnabled(z)).OfType<enSpi>().Where(z => !defined || (z.channel == channel && z.config == config)).Any();

          if(dio_nss && _nss == null && func!="IRQ") {
            _nss = en as enDIO;
            _nss.func = "NSS";

          } else if(_irq == null) {
            _irq = en as enDIO;
            _irq.func = "IRQ";
          }
        }
        return true;
      }
      return false;
    }
    public override string ExportH() {
      if(_mosi == null || _miso == null || _sck == null || _nss == null || _nss.parent.port == null || _irq==null || _irq.parent.port==null) {
        return "\r\n//RFM69 PHY" + _nr.ToString() + " NOT CONFIGURED\r\n";
      }
      var sb = new StringBuilder();

      sb.AppendFormat("\r\n//RFM69 PHY{0} Section\tMOSI: {1}, MISO: {2}, SCK: {3}, NSS: {4}, IRQ: {5}\r\n", _nr, _mosi.parent.name, _miso.parent.name, _sck.parent.name, _nss.parent.name, _irq.parent.name);
      sb.AppendFormat("#define HAL_USE_SPI{0}              {1}\r\n", _mosi.channel, _mosi.config);
      sb.AppendFormat("#define RFM69_USE_SPI               {0}\r\n", _mosi.channel);
      sb.AppendFormat("#define RFM69_NSS_PIN               {0}\r\n", _nss.parent.idx + _nss.parent.port.offset);
      sb.AppendFormat("#define RFM69_SELECT()              {0}\r\n", string.Format(_nss.parent.port.pinrst, _nss.parent.idx));
      sb.AppendFormat("#define RFM69_RELEASE()             {0}\r\n", string.Format(_nss.parent.port.pinset, _nss.parent.idx));
      sb.AppendFormat("#define RFM69_IRQ_PIN               {0}\r\n", _irq.parent.idx + _irq.parent.port.offset);
      sb.AppendFormat("#define RFM69_IRQ_STATE()           ({0} != 0)\r\n", string.Format(_irq.parent.port.pinget, _irq.parent.idx));
      sb.AppendFormat("#define RFM69_PHY                   {0}\r\n", _nr);
      sb.AppendLine("#define RFM69_DEFAULT_FREQ          868300000UL");
      sb.AppendLine("#include \"PHY/RFM69/rfm69_phy.h\"");
      sb.AppendFormat("//End RFM69 PHY{0} Section\r\n", _nr);

      return sb.ToString();
    }
  }*/
}
