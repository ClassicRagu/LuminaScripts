namespace Mogtomes
{
    public class MogtomeObject
    {
        public string Name { get; set; }

    }

    public class MogtomeContent
    {
        public uint ContentType { get; set; }
        public string ComputedString { get; set; }
        public List<ScoreReward> ScoreRewards { get; set; }
        public string Content0 { get; set; }
        public string Content1 { get; set; }
    }

    public class ScoreReward
    {
        public int Score {get; set;}
        public int Reward {get; set;}
    }
}