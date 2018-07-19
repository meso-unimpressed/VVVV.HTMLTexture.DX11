﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    [PluginInfo(
        Name = "ExecuteJavascript",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript",
        Help = "Execute Javascript without results"
    )]
    public class ExecuteJsOperationNode : PersistentOperationNode<ExecuteJsOperation>
    {
        [Input("Script", Order = 10, BinOrder = 11)]
        public IDiffSpread<string> FScript;
        [Input("Execute", Order = 12, BinOrder = 13, IsBang = true)]
        public IDiffSpread<bool> FExec;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FScript, FExec);
        }

        protected override void UpdateOps(ExecuteJsOperation ops, int i)
        {
            ops.Script = FScript[i];
            ops.Execute = FExec[i];
        }
    }

    [PluginInfo(
        Name = "EvaluateJavascript",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript",
        Help = "Execute Javascript which will return data"
    )]
    public class EvaluateJsOperationNode : PersistentOperationNode<EvaluateJsOperation>
    {
        [Input("Script", Order = 10, BinOrder = 11)]
        public IDiffSpread<string> FScript;
        [Input("Execute", Order = 12, BinOrder = 13, IsBang = true)]
        public IDiffSpread<bool> FExec;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FScript, FExec);
        }

        protected override void UpdateOps(EvaluateJsOperation ops, int i)
        {
            ops.Script = FScript[i];
            ops.Execute = FExec[i];
        }
    }

    [PluginInfo(
        Name = "GetJavascriptResult",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript",
        Help = "Gets the result of a script run from an EvaluateJavascript"
    )]
    public class GetJavascriptResultNode : GetOperationResultsNode<EvaluateJsOperationResult, EvaluateJsOperation>
    {
        [Output("Compound Result")]
        public ISpread<ISpread<string>> FCompRes;
        [Output("Error")]
        public ISpread<ISpread<string>> FError;

        protected override void OnWrapperSliceCount(int slc)
        {
            FCompRes.SliceCount = FError.SliceCount = slc;
        }

        protected override void OnOpsSliceCount(int slc, int i)
        {
            FCompRes[i].SliceCount = FError[i].SliceCount = slc;
        }

        protected override void OnInvalidResult(EvaluateJsOperation ops, HtmlTextureWrapper wrapper, int i, int j)
        {
            FCompRes[i][j] = FError[i][j] = "";
        }

        protected override void OnValidResult(EvaluateJsOperationResult result, EvaluateJsOperation ops, HtmlTextureWrapper wrapper, int i, int j)
        {
            FCompRes[i][j] = result.Result;
            FError[i][j] = result.Error;
        }
    }

    [PluginInfo(
        Name = "SplitJsResult",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript",
        Help = "Splits the result of a script run from an EvaluateJavascript"
    )]
    public class SplitJsResultNode : ObjectSplitNode<EvaluateJsOperationResult> { }
}