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
        if( en==_rx 
          || en==_tx
          || ((en.signal == 1 && _rx == null) && (_tx == null || (en.channel == _tx.channel && en.config == _tx.config))) 
          || ((en.signal == 2 && _tx == null) && (_rx == null || (en.channel == _rx.channel && en.config == _rx.config)))) {
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
      if(en!=null && old != en) {
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
          if((en as enSerial).signal == 1) {
            _rx = en as enSerial;
          } else if((en as enSerial).signal == 2) {
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
      int channel=0;
      int config=0;
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
      foreach(var en in pin.entrys.Where(z => z.type == EntryType.serial && pin._owner.EntryIsEnabled(z)).OfType<enSerial>().Where(z=>!defined || (z.channel==channel && z.config==config) ) ) {
        if(en == _rx || en == _tx || en == _de || (en.signal == 1 && _rx == null) || (en.signal == 2 && _tx == null) || (en.signal == 3 && _de == null)) {
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
      var en = pin.entrys.FirstOrDefault(z => z == _tx || z == _rx || z==_de);
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
          if((en as enSerial).signal == 1) {
            _rx = en as enSerial;
          } else if((en as enSerial).signal == 2) {
            _tx = en as enSerial;
          } else if((en as enSerial).signal == 3) {
            _de = en as enSerial;
          }
        }
        return true;
      }
      return false;

    }
  }

}
