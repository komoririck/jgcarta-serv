namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerInfo
{
    public string PlayerID { get; set; }
    public string PlayerName { get; set; }
    public int PlayerIcon { get; set; }
    public int HoloCoins { get; set; }
    public int HoloGold { get; set; }
    public int NNMaterial { get; set; }
    public int RRMaterial { get; set; }
    public int SRMaterial { get; set; }
    public int URMaterial { get; set; }
    public int MatchVictory { get; set; }
    public int MatchLoses { get; set; }
    public int MatchesTotal { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public List<PlayerItemBox> PlayerItemBox { get; set; } = new List<PlayerItemBox>();
    public List<PlayerTitle> PlayerTitles { get; set; } = new List<PlayerTitle>();
    public List<PlayerBadge> Badges { get; set; } = new List<PlayerBadge>();
    public List<PlayerMission> PlayerMissionList { get; set; } = new List<PlayerMission>();
    public List<PlayerMessageBox> PlayerMessageBox { get; set; } = new List<PlayerMessageBox>();
}
