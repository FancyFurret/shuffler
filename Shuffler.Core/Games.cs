namespace Shuffler.Core;

public static class Games
{
    public static readonly GameConfig Notepad = new()
    {
        Name = "Notepad",
        ExePath = "notepad.exe"
    };

    public static readonly GameConfig Ryujinx = new()
    {
        Name = "Ryujinx",
        ExePath = "G:\\Old Games\\Emulators\\ryujinx\\publish\\Ryujinx.exe"
    };

    public static readonly GameConfig Cemu = new()
    {
        Name = "Cemu",
        ExePath = "G:\\Old Games\\Emulators\\Cemu\\Cemu.exe"
    };

    public static readonly GameConfig HollowKnight = new()
    {
        Name = "Hollow Knight",
        ExePath = "G:\\Games\\steamapps\\common\\Hollow Knight\\hollow_knight.exe",
        SteamAppId = 367520
    };

    public static readonly GameConfig Celeste = new()
    {
        Name = "Celeste",
        ExePath = "G:\\Games\\steamapps\\common\\Celeste\\Celeste.exe",
        SteamAppId = 504230
    };

    public static readonly GameConfig Spel2 = new()
    {
        Name = "Spel2",
        ExePath = "G:\\Games\\steamapps\\common\\Spelunky 2\\Spel2.exe",
        SteamAppId = 418530
    };

    public static readonly GameConfig Undertale = new()
    {
        Name = "Undertale",
        ExePath = "G:\\Games\\steamapps\\common\\Undertale\\UNDERTALE.exe",
        SteamAppId = 391540
    };

    public static readonly GameConfig GetToWork = new()
    {
        Name = "Get To Work",
        ExePath = "G:\\Games\\steamapps\\common\\Get To Work\\Get To Work.exe"
    };

    public static readonly GameConfig Hades = new()
    {
        Name = "Hades",
        ExePath = "G:\\Games\\steamapps\\common\\Hades\\x64\\Hades.exe",
        SteamAppId = 1145360
    };

    public static readonly GameConfig Spelunky = new()
    {
        Name = "Spelunky",
        ExePath = "G:\\Games\\steamapps\\common\\Spelunky HD\\Spelunky.exe",
        SteamAppId = 239350
    };

    public static readonly GameConfig PowerWashSimulator = new()
    {
        Name = "Power Wash Simulator",
        ExePath = "G:\\Games\\steamapps\\common\\PowerWashSimulator\\PowerWashSimulator.exe",
        SteamAppId = 1290000
    };

    public static readonly GameConfig Isaac = new()
    {
        Name = "Isaac",
        ExePath = "G:\\Games\\steamapps\\common\\The Binding of Isaac Rebirth\\isaac-ng.exe",
        SteamAppId = 250900
    };

    public static readonly GameConfig Funger = new()
    {
        Name = "Fear & Hunger",
        ExePath = "G:\\Games\\steamapps\\common\\Fear & Hunger\\Game.exe"
    };

    public static readonly GameConfig EldenRing = new()
    {
        Name = "Elden Ring",
        ExePath = "G:\\Games\\steamapps\\common\\Elden Ring\\EldenRing.exe",
        SteamAppId = 1245620
    };
}