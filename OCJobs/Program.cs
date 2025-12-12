using OCJobs;
using Lumina;
using Lumina.Data.Files;
using Lumina.Excel.Sheets.Experimental;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class Program
{

    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string IconHDFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please include a file path");
            return;
        }

        string xivPath = args[0];

        var lumina = new GameData(xivPath, new() { DefaultExcelLanguage = Lumina.Data.Language.English });
#pragma warning disable PendingExcelSchema // Non-experimental doesn't have defined offsets
        var mkdSupportJobs = lumina.GetExcelSheet<MKDSupportJob>();
        var mkdTraits = lumina.GetExcelSheet<MKDTrait>();
        var mkdGrowDataSJob = lumina.GetSubrowExcelSheet<MKDGrowDataSJob>();
        var actionTransient = lumina.GetExcelSheet<ActionTransient>();
        List<CompiledJobInfo> compiledJobInfos = new List<CompiledJobInfo>();

        string actionDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"actions/");
        if (!Directory.Exists(actionDirectoryPath))
        {
            Directory.CreateDirectory(actionDirectoryPath);
        }

        string traitDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"traits/");
        if (!Directory.Exists(traitDirectoryPath))
        {
            Directory.CreateDirectory(traitDirectoryPath);
        }

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

                    try
                    {
                        var icon = GetIcon(lumina, "en/", action.Action.Value.Icon, true);
                        if (icon != null)
                        {
                            var image = Image.LoadPixelData<Bgra32>(icon.ImageData, icon.Header.Width, icon.Header.Height);
                            var iconFilePath = Path.Combine(actionDirectoryPath, $"{action.Action.Value.Name.ExtractText()}.png");
                            image.Save(iconFilePath);
                        }
                    }
                    catch
                    {
                        // :3 lets make sure the json file gets created at least just incase they change icon formats
                    }
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

                try
                {
                    var icon = GetIcon(lumina, "en/", mkdTrait.Unknown2, true);
                    if (icon != null)
                    {
                        var image = Image.LoadPixelData<Bgra32>(icon.ImageData, icon.Header.Width, icon.Header.Height);
                        var iconFilePath = Path.Combine(traitDirectoryPath, $"{mkdTrait.Unknown0.ExtractText()}.png");
                        image.Save(iconFilePath);
                    }
                }
                catch
                {
                    // :3 lets make sure the json file gets created at least just incase they change icon formats
                }
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

    private static TexFile? GetIcon(GameData gameData, string type, int iconId, bool hd)
    {
        type ??= string.Empty;
        if (type.Length > 0 && !type.EndsWith("/"))
            type += "/";

        var filePath = string.Format(hd ? IconHDFileFormat : IconFileFormat, iconId / 1000, type, iconId);
        try
        {
            var file = gameData.GetFile<TexFile>(filePath);

            if (file != default(TexFile) || type.Length <= 0) return file;

            // Couldn't get specific type, try for generic version.
            filePath = string.Format(hd ? IconHDFileFormat : IconFileFormat, iconId / 1000, string.Empty, iconId);
            file = gameData.GetFile<TexFile>(filePath);
            return file;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}