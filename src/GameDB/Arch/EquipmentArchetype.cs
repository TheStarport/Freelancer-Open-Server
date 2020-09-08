using FLDataFile;

namespace FLServer.GameDB.Arch
{
    class EquipmentArchetype :Archetype
    {
        public bool Lootable;
        public float UnitsPerContainer;
        public float Volume;
        public EquipmentArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("lootable", out tmpSet))
                Lootable = bool.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("units_per_container", out tmpSet))
                UnitsPerContainer = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("volume", out tmpSet))
                Volume = float.Parse(tmpSet[0]);
        }

    }
}
