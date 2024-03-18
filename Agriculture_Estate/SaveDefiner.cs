using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace AgricultureEstate
{
    public class SaveDefiner : SaveableTypeDefiner
    {
        public SaveDefiner()
          : base(1436500002)
        {
        }

        protected override void DefineClassTypes() => this.AddClassDefinition(typeof(VillageLand), 1);

        protected override void DefineContainerDefinitions() => this.ConstructContainerDefinition(typeof(Dictionary<Settlement, VillageLand>));
    }
}
