﻿using VVVV.PluginInterfaces.V2;
using VVVV.Vanadium.Core;

namespace VVVV.Vanadium.Nodes
{
    [PluginInfo(
        Name = "Navigate",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Help = "Go back or forward in browser history"
    )]
    public class NavigationOperationNodes : PersistentOperationNode<NavigationOperation>
    {
        [Input("Back", Order = 10, BinOrder = 11, IsBang = true)]
        public ISpread<bool> FBack;
        [Input("Forward", Order = 12, BinOrder = 13, IsBang = true)]
        public ISpread<bool> FForw;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FBack, FForw);
        }

        protected override void UpdateOps(ref NavigationOperation ops, int i)
        {
            ops.Backward = FBack[i];
            ops.Forward = FForw[i];
            ops.Execute = ops.Backward || ops.Forward;
        }
    }
}
