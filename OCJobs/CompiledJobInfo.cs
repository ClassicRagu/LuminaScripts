namespace OCJobs
{
	public class CompiledJobInfo
	{
		public string PJob { get; set; }
        public string PJobDescription {get; set;}
        public uint MaxLevel {get; set;}
        public List<int> EXPValues {get; set;}
        public int TotalEXP {get
            {
                return EXPValues.Sum();
            }
        }
        public List<LevelUnlock>[] LevelUnlocks {get; set;}

	}

    public class LevelUnlock
    {
        public uint Level {get; set;}
        public string UnlockType {get; set;}
        public uint ActionTraitRowID {get; set;}
        public string ActionTraitName {get; set;}
        public string ActionTraitEffect {get; set;}
        public uint ActionTraitIconID {get; set;}
        public string ActionType {get; set;}
        public sbyte ActionRange {get; set;}
    }
}