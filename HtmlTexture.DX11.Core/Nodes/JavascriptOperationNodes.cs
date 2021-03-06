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
    public class JsOperationNode<T> : PersistentOperationNode<T> where T : HtmlTextureOperation, IJsOperation, new()
    {
        [Input("Script", Order = 10, BinOrder = 11)]
        public IDiffSpread<ISpread<string>> FScript;
        [Input("Execute", Order = 12, BinOrder = 13, IsBang = true)]
        public IDiffSpread<ISpread<bool>> FExec;
        [Input("Execute On Load", Order = 14, BinOrder = 15)]
        public IDiffSpread<ISpread<bool>> FExecOnLoad;

        protected override int BinCount()
        {
            return SpreadUtils.SpreadMax(FScript, FExec, FExecOnLoad);
        }

        protected override int SliceCount(int i)
        {
            return SpreadUtils.SpreadMax(FScript[i], FExec[i], FExecOnLoad[i]);
        }

        protected override void UpdateOps(ref T ops, int i, int j)
        {
            ops.Script = FScript.TryGetSlice(i, j) ?? "";
            ops.Execute = FExec.TryGetSlice(i, j);
            ops.ExecuteOnLoad = FExecOnLoad.TryGetSlice(i, j);
        }
    }

    [PluginInfo(
        Name = "ExecuteJavascript",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript, Vanadium",
        Help = "Execute Javascript without results"
    )]
    public class ExecuteJsOperationNode : JsOperationNode<ExecuteJsOperation> { }

    [PluginInfo(
        Name = "EvaluateJavascript",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript, Vanadium",
        Help = "Execute Javascript which will return data"
    )]
    public class EvaluateJsOperationNode : JsOperationNode<EvaluateJsOperation> { }

    [PluginInfo(
        Name = "GetJavascriptResult",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript, Vanadium",
        Help = "Gets the result of a script run from an EvaluateJavascript"
    )]
    public class GetJavascriptResultNode : GetOperationResultsNode<ResultFromJs, EvaluateJsOperation>
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

        protected override void OnValidResult(ResultFromJs result, EvaluateJsOperation ops, HtmlTextureWrapper wrapper, int i, int j)
        {
            FCompRes[i][j] = result.Result;
            FError[i][j] = result.Error;
        }
    }

    [PluginInfo(
        Name = "SplitJsResult",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "javascript, Vanadium",
        Help = "Splits a result coming from Javascript"
    )]
    public class SplitJsResultNode : ObjectSplitNode<ResultFromJs> { }
}
