using Newtonsoft.Json;

namespace Mogtomes
{
    public class MogtomeObject
    {
        public string Name { get; set; }
        public string Currency { get; set; }
        public List<MogtomeContent> StandardObjectives { get; set; }
        public List<MogtomeContent> MinimogObjectives { get; set; }
        public MogtomeContent UltimogObjectives { get; set; }

    }

    public class MogtomeContent
    {
        public uint ContentType { get; set; }
        public string ComputedString { get; set; }
        public int? Week { get; set; }
        public List<ScoreReward> ScoreRewards { get; set; }
    }

    public class ScoreReward
    {
        public int Score { get; set; }
        public int Reward { get; set; }
    }
}