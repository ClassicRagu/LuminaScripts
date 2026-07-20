using System.Reflection.Metadata.Ecma335;
using Lumina;
using Lumina.Data.Files;
using Lumina.Excel.Sheets.Experimental;
using Newtonsoft.Json;
using Lumina.Excel;
using Mogtomes;

#pragma warning disable PendingExcelSchema // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please include a file path to your sqpack folder");
            return;
        }

        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"json/");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string xivPath = args[0];

        var lumina = new GameData(xivPath, new() { DefaultExcelLanguage = Lumina.Data.Language.English });

        var csBonusSeasons = lumina.GetExcelSheet<CSBonusSeason>().Where(x => x.Text0.RowId > 0);

        foreach (CSBonusSeason cSBonusSeason in csBonusSeasons)
        {
            MogtomeObject output = ProcessMogtomeSeason(lumina, cSBonusSeason);
            File.WriteAllText(Path.Combine(directoryPath, $"{output.Currency}.json"), JsonConvert.SerializeObject(output, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
        }

    }

    static MogtomeObject ProcessMogtomeSeason(GameData lumina, CSBonusSeason csBonusSeason)
    {
        Console.WriteLine(csBonusSeason.Text1.Value.Text);
        var csBonusmissions = lumina.GetSubrowExcelSheet<CSBonusMission>();
        MogtomeObject mogtomeObject = new MogtomeObject()
        {
            Name = csBonusSeason.Text1.Value.Text.ExtractText(),
            Currency = csBonusSeason.Item.Value.Name.ExtractText()
        };

        Console.WriteLine("Standard/Weekly Objectives");
        mogtomeObject.StandardObjectives = ProcessMissions(lumina, csBonusmissions.GetRow(csBonusSeason.Category0));

        Console.WriteLine();
        Console.WriteLine("Minimog Objectives");
        mogtomeObject.MinimogObjectives = ProcessMissions(lumina, csBonusmissions.GetRow(csBonusSeason.Category2), "minimog");

        Console.WriteLine();
        Console.WriteLine("Ultimog Objectives");
        mogtomeObject.UltimogObjectives = ProcessMissions(lumina, csBonusmissions.GetRow(csBonusSeason.Category3), "ultimog").First();

        Console.WriteLine();
        return mogtomeObject;
    }

    static List<MogtomeContent> ProcessMissions(GameData lumina, SubrowCollection<CSBonusMission> csBonusMissions, string objectiveType = "standard")
    {
        List<MogtomeContent> mogtomeContents = new List<MogtomeContent>();
        var i = 1;
        foreach (CSBonusMission csBonusMission in csBonusMissions)
        {
            MogtomeContent? content0 = null;
            MogtomeContent? content1 = null;
            if (objectiveType == "minimog") Console.WriteLine($"Week {i}:");
            content0 = ProcessContent(lumina, csBonusMission.Content0.Value);
            if (objectiveType == "minimog")
            {
                content1 = ProcessContent(lumina, csBonusMission.Content1.Value);
                content0.Week = i;
                content1.Week = i;
            }
            
            mogtomeContents.Add(content0);
            if (content1 != null) mogtomeContents.Add(content1);
            i++;
        }
        return mogtomeContents;
    }

    static MogtomeContent ProcessContent(GameData lumina, CSBonusContent csBonusContent)
    {
        //Console.WriteLine($"Next Content {csBonusContent.ContentType.RowId}");
        var contentFinderCondition = lumina.GetExcelSheet<ContentFinderCondition>();
        var items = lumina.GetExcelSheet<Item>();
        var tripleTriadCardResidents = lumina.GetExcelSheet<TripleTriadCardResident>();

        MogtomeContent mogtomeContent = new MogtomeContent();
        List<ScoreReward> scoreRewards = new List<ScoreReward>();
        bool calculateScores = false;
        switch (csBonusContent.ContentType.RowId)
        {
            // Dungeons, Trials, Raids
            case 1:
            case 2:
            case 3:
            case 4:
                var meow = contentFinderCondition.First((x) => x.Content.GetValueOrDefault<InstanceContent>() != null && x.Content.RowId == csBonusContent.Content0.Value.Content.RowId);

                Console.Write(meow.Name);
                Console.Write(": ");

                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"{meow.Name}"
                };
                calculateScores = true;
                break;
            // Deep Dungeons
            case 5:
                // New Deep Dungeon identifiers use the DeepDungeon Sheet
                if (csBonusContent.Content0.Value.ContentLinkType == 9)
                {
                    // This is why we have to use experimental rn btw
                    var deepDungeon = csBonusContent.Content0.Value.Content.GetValueOrDefault<DeepDungeon>();
                    if (deepDungeon != null)
                    {
                        Console.WriteLine($"Complete a floor in {deepDungeon.Value.Name}");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Complete a floor in {deepDungeon.Value.Name}"
                        };
                    }
                }
                // Old Deep Dungeon identifiers use the InstanceContent Sheet
                else if (csBonusContent.Content0.Value.ContentLinkType == 1)
                {
                    var instanceContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<InstanceContent>();
                    if (instanceContent != null)
                    {
                        Console.WriteLine($"Clear {instanceContent.Value.ContentFinderCondition.Value.Name}");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Clear {instanceContent.Value.ContentFinderCondition.Value.Name}"
                        };
                    }
                }
                break;
            // Ocean Fishing
            case 6:
                Console.Write("Ocean Fishing: ");

                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Ocean Fishing"
                };

                calculateScores = true;
                break;
            // Triple Triad
            case 7:
                if (csBonusContent.Content0.Value.ContentLinkType == 7)
                {
                    var eNPCResident = csBonusContent.Content0.Value.Content.GetValueOrDefault<ENpcResident>();
                    if (eNPCResident != null)
                    {
                        var ttNPCLocal = tripleTriadCardResidents.First((x) => x.AcquisitionType.RowId == 6 && x.Acquisition.RowId == eNPCResident.Value.RowId);
                        Console.WriteLine($"Win a game of Triple Triad against {eNPCResident.Value.Singular} in {ttNPCLocal.Location.GetValueOrDefault<Level>().Value.Map.Value.PlaceName.Value.Name}.");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Win a game of Triple Triad against {eNPCResident.Value.Singular} in {ttNPCLocal.Location.GetValueOrDefault<Level>().Value.Map.Value.PlaceName.Value.Name}."
                        };
                    }
                }
                break;
            // Treasure Hunt (No Portal)
            case 8:
                Console.WriteLine("Decipher a timeworn map and collect the treasure.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Decipher a timeworn map and collect the treasure."
                };
                break;
            // The Hunt
            case 9:
                var huntName = csBonusContent.Content0.Value.Content.GetValueOrDefault<MobHuntOrderType>()?.EventItem.Value.Name;
                Console.WriteLine("Complete a " + huntName + ".");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Complete a {huntName}."
                };
                break;
            // Fishing
            case 10:
                var fishItem = csBonusContent.Content0.Value.Content.GetValueOrDefault<FishParameter>()?.Item.RowId;
                if (fishItem != null)
                {
                    var fishName = items.GetRow(fishItem ?? 0);
                    Console.WriteLine("Catch " + fishName.Name);
                    mogtomeContent = new MogtomeContent()
                    {
                        ContentType = csBonusContent.ContentType.RowId,
                        ComputedString = $"Catch {fishName.Name}."
                    };
                }
                break;
            // GATE
            case 11:
                if (csBonusContent.Content0.Value.ContentLinkType == 2)
                {
                    // This is why we have to use experimental rn btw
                    var gFateType = csBonusContent.Content0.Value.Content.GetValueOrDefault<GFateType>();
                    if (gFateType != null)
                    {
                        Console.Write($"{gFateType.Value.Name.Value.Text}: ");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"{gFateType.Value.Name.Value.Text}"
                        };
                    }
                }
                calculateScores = true;
                break;
            // FATE
            case 12:
                var territory1 = csBonusContent.Content0.Value.Content.GetValueOrDefault<TerritoryType>()?.PlaceName.Value.Name;
                var territory2 = csBonusContent.Content1.Value.Content.GetValueOrDefault<TerritoryType>()?.PlaceName.Value.Name;
                Console.WriteLine("Complete " + csBonusContent.Score1 + " FATEs in " + territory1 + " or " + territory2 + ".");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Complete {csBonusContent.Score1} FATEs in {territory1} or {territory2}."
                };
                break;
            // 13 is unused atm
            // Treasure Hunt (With Portal)
            case 14:
                Console.WriteLine($"Enter a treasure dungeon via a teleportation portal {csBonusContent.Score1} times.※Treasure hunts abandoned midway through will not count toward the total.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Enter a treasure dungeon via a teleportation portal {csBonusContent.Score1} times.※Treasure hunts abandoned midway through will not count toward the total."
                };
                break;
            // Eureka
            case 15:
                if (csBonusContent.Content0.Value.ContentLinkType == 8)
                {
                    var publicContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<PublicContent>();
                    if (publicContent != null)
                    {
                        Console.WriteLine($"Defeat {csBonusContent.Score1} notorious monster(s) in {publicContent.Value.ContentFinderCondition.Value.Name}");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Defeat {csBonusContent.Score1} notorious monster(s) in {publicContent.Value.ContentFinderCondition.Value.Name}"
                        };
                    }
                }
                break;
            // Bozja CEs
            case 16:
                if (csBonusContent.Content0.Value.ContentLinkType == 8)
                {
                    var publicContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<PublicContent>();
                    if (publicContent != null)
                    {
                        Console.WriteLine($"Complete {csBonusContent.Score1} Critical Engagements in {publicContent.Value.ContentFinderCondition.Value.Name}");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Complete {csBonusContent.Score1} critical engagements in {publicContent.Value.ContentFinderCondition.Value.Name}"
                        };
                    }
                }
                else if (csBonusContent.Content0.Value.ContentLinkType == 10)
                {
                    var dynamicEvent = csBonusContent.Content0.Value.Content.GetValueOrDefault<DynamicEvent>();
                    if (dynamicEvent != null)
                    {
                        Console.WriteLine($"Complete the critical engagement {dynamicEvent.Value.Name} {csBonusContent.Score1} time(s).");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Complete the critical engagement {dynamicEvent.Value.Name} {csBonusContent.Score1} time(s)."
                        };
                    }
                }
                break;
            // Masked Carnival
            case 17:
                Console.WriteLine("Complete a stage of the Masked Carnivale");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Complete a stage of the Masked Carnivale."
                };
                break;
            // Island Sanctuary
            case 18:
                Console.WriteLine("Gather materials " + csBonusContent.Score1 + " times in Island Sanctuary");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Gather materials {csBonusContent.Score1} times in Island Sanctuary."
                };
                break;
            // V&C
            case 19:
                var vcInstanceContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<InstanceContent>();
                if (vcInstanceContent != null)
                {
                    Console.WriteLine($"Complete {vcInstanceContent.Value.ContentFinderCondition.Value.Name} {csBonusContent.Score1} times.");
                    mogtomeContent = new MogtomeContent()
                    {
                        ContentType = csBonusContent.ContentType.RowId,
                        ComputedString = $"Complete {vcInstanceContent.Value.ContentFinderCondition.Value.Name} {csBonusContent.Score1} times."
                    };
                }
                break;
            // Custom Deliveries
            case 20:
                Console.WriteLine("Complete any custom delivery " + csBonusContent.Score1 + " times.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Complete any custom delivery {csBonusContent.Score1} times."
                };
                break;
            // Society Quests
            case 21:
                Console.WriteLine($"Complete any society daily quest {csBonusContent.Score1} times.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Complete any society daily quest {csBonusContent.Score1} times."
                };
                break;
            // Crystaline Conflict
            case 22:
                Console.WriteLine($"Participate in a casual match of Crystalline Conflict.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Participate in a casual match of Crystalline Conflict."
                };
                break;
            // Chocobo Racing
            case 23:
                Console.WriteLine($"Participate in {csBonusContent.Score1} chocobo races.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Participate in {csBonusContent.Score1} chocobo races."
                };
                break;
            // Wondrous Tails
            case 24:
                Console.WriteLine($"Collect {csBonusContent.Score1} seals for Wondrous Tails.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Collect {csBonusContent.Score1} seals for Wondrous Tails."
                };
                break;
            // Mahjong
            case 25:
                Console.WriteLine($"Play {csBonusContent.Score1} match of Doman mahjong with other players.※NPC matches will not be counted.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Play {csBonusContent.Score1} match of Doman mahjong with other players.※NPC matches will not be counted."
                };
                break;
            // Supply and Provisioning
            case 26:
                Console.WriteLine($"Complete {csBonusContent.Score1} Grand Company supply and provisioning missions.");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Complete {csBonusContent.Score1} Grand Company supply and provisioning missions."
                };
                break;
            // Occult Crescent CEs
            case 27:
                if (csBonusContent.Content0.Value.ContentLinkType == 8)
                {
                    var publicContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<PublicContent>();
                    if (publicContent != null)
                    {
                        Console.WriteLine($"Complete {csBonusContent.Score1} critical encounters in {publicContent.Value.ContentFinderCondition.Value.Name}");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Complete {csBonusContent.Score1} critical encounters in {publicContent.Value.ContentFinderCondition.Value.Name}"
                        };
                    }
                }
                break;
            // Bozja Skirmishes
            case 28:
                if (csBonusContent.Content0.Value.ContentLinkType == 8)
                {
                    var publicContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<PublicContent>();
                    if (publicContent != null)
                    {
                        Console.WriteLine($"Complete {csBonusContent.Score1} skirmishes in {publicContent.Value.ContentFinderCondition.Value.Name}");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Complete {csBonusContent.Score1} skirmishes in {publicContent.Value.ContentFinderCondition.Value.Name}"
                        };
                    }
                }
                break;
            // Delubrum Reginae Normal
            case 29:
                if (csBonusContent.Content0.Value.ContentLinkType == 8)
                {
                    var publicContent = csBonusContent.Content0.Value.Content.GetValueOrDefault<PublicContent>();
                    if (publicContent != null)
                    {
                        Console.WriteLine($"Complete {publicContent.Value.ContentFinderCondition.Value.Name} {csBonusContent.Score1} time(s).");
                        mogtomeContent = new MogtomeContent()
                        {
                            ContentType = csBonusContent.ContentType.RowId,
                            ComputedString = $"Complete {publicContent.Value.ContentFinderCondition.Value.Name} {csBonusContent.Score1} time(s)."
                        };
                    }
                }
                break;
            default:
                Console.WriteLine($"Unknown! ContentType: {csBonusContent.ContentType.RowId}");
                mogtomeContent = new MogtomeContent()
                {
                    ContentType = csBonusContent.ContentType.RowId,
                    ComputedString = $"Unknown! ContentType: {csBonusContent.ContentType.RowId}"
                };
                break;
        }

        if (calculateScores)
        {
            mogtomeContent.ScoreRewards = ScoreConverter(csBonusContent);
            Console.WriteLine();
        }

        return mogtomeContent;
    }

    static List<ScoreReward> ScoreConverter(CSBonusContent csBonusContent)
    {
        // Worst code implementation award goes to this entire function.
        // This is the laziest way to do this and I'm not changing it!
        List<ScoreReward> scoreRewards = new List<ScoreReward>();
        if (csBonusContent.Score1 > -1)
        {
            Console.Write(" " + csBonusContent.Score1 + " - " + csBonusContent.RewardCount0 + " |");
            scoreRewards.Add(new ScoreReward()
            {
                Score = csBonusContent.Score1,
                Reward = csBonusContent.RewardCount0
            });
        }
        else
        {
            Console.Write(csBonusContent.RewardCount0);
            scoreRewards.Add(new ScoreReward()
            {
                Score = 0,
                Reward = csBonusContent.RewardCount0
            });
            return scoreRewards;
        }
        if (csBonusContent.Score2 > -1)
        {
            Console.Write(" " + csBonusContent.Score2 + " - " + csBonusContent.RewardCount1 + " |");
            scoreRewards.Add(new ScoreReward()
            {
                Score = csBonusContent.Score2,
                Reward = csBonusContent.RewardCount1
            });
        }
        if (csBonusContent.Score3 > -1)
        {
            Console.Write(" " + csBonusContent.Score3 + " - " + csBonusContent.RewardCount2 + " |");
            scoreRewards.Add(new ScoreReward()
            {
                Score = csBonusContent.Score3,
                Reward = csBonusContent.RewardCount2
            });
        }
        if (csBonusContent.Score4 > -1)
        {
            Console.Write(" " + csBonusContent.Score4 + " - " + csBonusContent.RewardCount3 + " |");
            scoreRewards.Add(new ScoreReward()
            {
                Score = csBonusContent.Score4,
                Reward = csBonusContent.RewardCount3
            });
        }
        if (csBonusContent.Score5 > -1)
        {
            Console.Write(" " + csBonusContent.Score5 + " - " + csBonusContent.RewardCount4 + " |");
            scoreRewards.Add(new ScoreReward()
            {
                Score = csBonusContent.Score5,
                Reward = csBonusContent.RewardCount4
            });
        }

        return scoreRewards;
    }
}

#pragma warning restore PendingExcelSchema // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.