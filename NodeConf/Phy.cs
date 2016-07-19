using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X13 {
  internal abstract class phyBase {
    public static phyBase Create(string name, int nr) {
      switch(name) {
      case "UART":
        return new phySerial(nr);
      case "RS485":
        return new phyRS485(nr);
      case "CC1101":
        return new phyCC1101(nr);
      case "ENC28J60":
        return new phyENC28J60(nr);
      case "RFM69":
        return new phyRFM69(nr);
      }
      return null;
    }

    protected int _nr;

    public string signature { get; protected set; }
    public abstract List<enBase> GetLst(Pin pin);
    public abstract enBase GetCur(Pin pin);
    public abstract bool SetCur(Pin pin, enBase en);

  }

  internal class phySerial : phyBase {
    private enSerial _tx;
    private enSerial _rx;

    public phySerial(int nr) {
      signature = "S";
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
    public override bool SetCur(Pin pin, enBase en) {
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
  }
  internal class phyRS485 : phyBase {
    private enSerial _tx;
    private enSerial _rx;
    private enSerial _de;

    public phyRS485(int nr) {
      signature = "R";
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
    public override bool SetCur(Pin pin, enBase en) {
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
  }
  internal class phyCC1101 : phyBase {
    private enSpi _mosi;
    private enSpi _miso;
    private enSpi _sck;
    private enBase _nss;

    public phyCC1101(int nr) {
      signature = "C";
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
    public override bool SetCur(Pin pin, enBase en) {
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
              _nss.name = pin.name;
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
          _nss.name = pin.name + "_NSS";
        }
        return true;
      }
      return false;
    }
  }

  internal class phyENC28J60 : phyBase {
    private enSpi _mosi;
    private enSpi _miso;
    private enSpi _sck;
    private enBase _nss;

    public phyENC28J60(int nr) {
      signature = "E";
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
    public override bool SetCur(Pin pin, enBase en) {
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
              _nss.name = pin.name;
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
          _nss.name = pin.name + "_NSS";
        }
        return true;
      }
      return false;
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
    public override bool SetCur(Pin pin, enBase en) {
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
              _nss.name = pin.name;
            }
            _nss = null;
          } else if(_irq == old) {
            _irq.name = pin.name;
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

          if(dio_nss && _nss == null) {
            _nss = en as enDIO;
            _nss.name = pin.name + "_NSS";

          } else if(_irq == null) {
            _irq = en as enDIO;
            _irq.name = pin.name + "_IRQ";
          }
        }
        return true;
      }
      return false;
    }
  }
}
