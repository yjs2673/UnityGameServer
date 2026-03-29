namespace MyGameServer.Models;

// 랭킹 정보 모델
public class RankData
{
    public int id { get; set; }
    public string nickname { get; set; } = string.Empty;
    public int level { get; set; }
}
