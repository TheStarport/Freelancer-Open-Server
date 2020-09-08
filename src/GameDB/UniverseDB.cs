using System.IO;
using System.Threading.Tasks;
using FLDataFile;
using NLog;

namespace FLServer.GameDB
{
    static class UniverseDB
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
// ReSharper disable once CSharpWarnings::CS1998
        public static async Task LoadUniverse(string flPath)
        {
            var flFile = new DataFile(Path.Combine(flPath, @"EXE\Freelancer.ini"));

            var flDataPath = Path.Combine(flPath, @"EXE", flFile.GetSetting("Freelancer", "data path")[0]);

            Parallel.ForEach(flFile.GetSettings("Data", "goods"),
                gentry => BaseDB.LoadGoods(
                    new DataFile(
                        flDataPath + Path.DirectorySeparatorChar + gentry[0]
                        )
                    ));

            Logger.Info("Loaded {0} goods", BaseDB.UniGoods.Count);

            foreach (var uset in flFile.GetSettings("Data", "universe"))
            {
                var uniFile = new DataFile(flDataPath + Path.DirectorySeparatorChar + uset[0]);



                //TODO: load loadout

                //TODO: load factions

                //TODO: load system

                //load bases
                BaseDB.LoadBases(uniFile.GetSections("Base"),flDataPath);
                Logger.Info("Loaded {0} bases", BaseDB.BaseCount);

                BaseDB.LoadNews(new DataFile(flDataPath + Path.DirectorySeparatorChar + @"missions" + Path.DirectorySeparatorChar + @"news.ini"));
                Logger.Info("Loaded news");

                BaseDB.LoadMBase(new DataFile(flDataPath + @"\missions\mbases.ini"));
                Logger.Info("Loaded mbase");
                BaseDB.LoadGenericScripts(new DataFile(flDataPath + @"\scripts\gcs\genericscripts.ini"));
                Logger.Info("Loaded generic scripts");
                //load goods


                //load markets
                Parallel.ForEach(uniFile.GetSettings("Data", "markets"),
                    mentry => BaseDB.LoadBaseMarketData(
                        new DataFile(
                            flDataPath + Path.DirectorySeparatorChar + mentry[0]
                            )
                            ));
                Logger.Info("Loaded markets");

            }

            //TODO: check all the links

        }

    }
}
