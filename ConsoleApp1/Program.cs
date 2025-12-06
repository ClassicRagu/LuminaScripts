using ConsoleApp1;
using Lumina.Excel.Sheets.Experimental;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please include a file path");
            return;
        }

        string xivPath = args[0];

        var lumina = new Lumina.GameData(xivPath, new() { DefaultExcelLanguage = Lumina.Data.Language.English });
        #pragma warning disable PendingExcelSchema // Non-experimental doesn't have defined offsets
        var mkdSupportJobs = lumina.GetExcelSheet<MKDSupportJob>();
        var mkdTraits = lumina.GetExcelSheet<MKDTrait>();
        var mkdGrowDataSJob = lumina.GetSubrowExcelSheet<MKDGrowDataSJob>();
        var actionTransient = lumina.GetExcelSheet<ActionTransient>();
        List<CompiledJobInfo> compiledJobInfos = new List<CompiledJobInfo>();

        foreach (MKDSupportJob mkdSupportJob in mkdSupportJobs)
        {
            CompiledJobInfo compiledJobInfo = new CompiledJobInfo();
            compiledJobInfo.PJob = mkdSupportJob.Name.ExtractText();
            compiledJobInfo.PJobDescription = mkdSupportJob.Description.ExtractText();
            compiledJobInfo.MaxLevel = mkdSupportJob.LevelMax;
            // surely casting a uint to an int wouldn't cause issues :)
            List<int> expData = mkdGrowDataSJob[mkdSupportJob.RowId].Select(z => (int)z.Unknown0).ToList();
            compiledJobInfo.EXPValues = expData;
            // Freelancer's Level Max is 0, since we don't know how many actions it will get we just make it have 6 entries
            List<LevelUnlock>[] levelUnlocks = new List<LevelUnlock>[mkdSupportJob.LevelMax != 0 ? mkdSupportJob.LevelMax : 6];
            uint x = 0;
            while (x < levelUnlocks.Length)
            {
                levelUnlocks[x] = new List<LevelUnlock>();
                x++;
            }
            foreach (MKDSupportJob.ActionsStruct action in mkdSupportJob.Actions)
            {
                if (action.LevelUnlock != 0)
                {
                    LevelUnlock levelUnlock = new LevelUnlock();
                    levelUnlock.Level = action.LevelUnlock;
                    levelUnlock.UnlockType = "Action";
                    levelUnlock.ActionTraitRowID = action.Action.RowId;
                    levelUnlock.ActionTraitName = action.Action.Value.Name.ExtractText();
                    levelUnlock.ActionTraitEffect = actionTransient.GetRow(action.Action.RowId).Description.ExtractText();
                    levelUnlock.ActionType = action.Action.Value.ActionCategory.Value.Name.ExtractText();
                    levelUnlock.ActionTraitIconID = action.Action.Value.Icon;
                    levelUnlock.ActionRange = action.Action.Value.Range;
                    levelUnlocks[mkdSupportJob.RowId != 0 ? action.LevelUnlock - 1 : action.LevelUnlock / 5 - 1].Add(levelUnlock);
                }
            }
            compiledJobInfo.LevelUnlocks = levelUnlocks;
            compiledJobInfos.Add(compiledJobInfo);
        }
        foreach (MKDTrait mkdTrait in mkdTraits)
        {
            if (mkdTrait.Unknown4 > 0)
            {
                LevelUnlock levelUnlock = new LevelUnlock();
                levelUnlock.Level = mkdTrait.Unknown4;
                levelUnlock.UnlockType = "Trait";
                levelUnlock.ActionTraitRowID = mkdTrait.RowId;
                levelUnlock.ActionTraitName = mkdTrait.Unknown0.ExtractText();
                levelUnlock.ActionTraitEffect = mkdTrait.Unknown1.ExtractText();
                levelUnlock.ActionTraitIconID = (uint)mkdTrait.Unknown2;
                compiledJobInfos[mkdTrait.Unknown3].LevelUnlocks[mkdTrait.Unknown4 - 1].Add(levelUnlock);
            }
        }
        // Console.WriteLine(JsonConvert.SerializeObject(compiledJobInfos, Formatting.Indented));
#pragma warning restore PendingExcelSchema
        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"json/");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        File.WriteAllText(Path.Combine(directoryPath, "PhantomJobs.json"), JsonConvert.SerializeObject(compiledJobInfos, Formatting.Indented));
    }
}