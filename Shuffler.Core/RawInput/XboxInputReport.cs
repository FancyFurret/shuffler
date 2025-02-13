using System.Runtime.InteropServices;

namespace Shuffler.Core.RawInput;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct XboxInputReport
{
    public ushort GamePadX;  // 0 to 65535
    public ushort GamePadY;  // 0 to 65535
    public ushort GamePadRx; // 0 to 65535
    public ushort GamePadRy; // 0 to 65535

    private ushort _zAndPadding;
    public int GamePadZ
    {
        get => _zAndPadding & 0x3FF;
        set => _zAndPadding = (ushort)(((_zAndPadding & ~0x3FF) | (value & 0x3FF)));
    }

    private ushort _rzAndPadding;
    public int GamePadRz
    {
        get => _rzAndPadding & 0x3FF;
        set => _rzAndPadding = (ushort)(((_rzAndPadding & ~0x3FF) | (value & 0x3FF)));
    }

    private byte _buttons1;
    public bool Button1 { get => (_buttons1 & 0x01) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x01 : _buttons1 & ~0x01); }
    public bool Button2 { get => (_buttons1 & 0x02) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x02 : _buttons1 & ~0x02); }
    public bool Button3 { get => (_buttons1 & 0x04) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x04 : _buttons1 & ~0x04); }
    public bool Button4 { get => (_buttons1 & 0x08) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x08 : _buttons1 & ~0x08); }
    public bool Button5 { get => (_buttons1 & 0x10) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x10 : _buttons1 & ~0x10); }
    public bool Button6 { get => (_buttons1 & 0x20) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x20 : _buttons1 & ~0x20); }
    public bool Button7 { get => (_buttons1 & 0x40) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x40 : _buttons1 & ~0x40); }
    public bool Button8 { get => (_buttons1 & 0x80) != 0; set => _buttons1 = (byte)(value ? _buttons1 | 0x80 : _buttons1 & ~0x80); }

    private byte _buttons2;
    public bool Button9 { get => (_buttons2 & 0x01) != 0; set => _buttons2 = (byte)(value ? _buttons2 | 0x01 : _buttons2 & ~0x01); }
    public bool Button10 { get => (_buttons2 & 0x02) != 0; set => _buttons2 = (byte)(value ? _buttons2 | 0x02 : _buttons2 & ~0x02); }

    private byte _hatAndPadding;
    public byte HatSwitch
    {
        get => (byte)(_hatAndPadding & 0x0F);
        set => _hatAndPadding = (byte)((_hatAndPadding & ~0x0F) | (value & 0x0F));
    }

    private byte _systemControlAndPadding;
    public bool SystemMainMenu
    {
        get => (_systemControlAndPadding & 0x01) != 0;
        set => _systemControlAndPadding = (byte)(value ? _systemControlAndPadding | 0x01 : _systemControlAndPadding & ~0x01);
    }

    public byte BatteryStrength;
}