// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.SaveDefiner
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

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
