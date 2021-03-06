﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Chromium.Remote;
using mp.pddn;
using SlimDX.D3DCompiler;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    [PluginInfo(
        Name = "BindObject",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "Bind, javascript, Vanadium",
        Help = "Bind objects with functions which can be called by Javascript with arguments, Get those arguments in vvvv and return an object in Javascript",
        Warnings = "Supply only numbers, strings, V8Objects, simple structs or classes with primitive members, Do not supply complex self referencing big objects as return values. It can crash vvvv"
    )]
    public class ObjectBindingNode : PersistentOperationNode<BindObjectOperation>, IPartImportsSatisfiedNotification
    {
        [Import] public IHDEHost Hde;
        [Import] public IPluginHost2 Host;

        private GenericBinSizedInput _input;

        [Input("Object Name", Order = 6, BinOrder = 7)]
        public ISpread<string> FObj;
        [Input("Function Name", Order = 8, BinOrder = 9)]
        public ISpread<ISpread<string>> FFunc;
        [Input("Bind", Order = 12, BinOrder = 13, IsBang = true)]
        public IDiffSpread<bool> FBind;
        [Input("Bind On Load", Order = 14, BinOrder = 15)]
        public IDiffSpread<bool> FBindOnLoad;

        protected override int BinCount()
        {
            return Math.Max(SpreadUtils.SpreadMax(FObj, FFunc, FBind, FBindOnLoad), _input.SliceCount) *
                   (_input.SliceCount > 0 ? 1 : 0);
        }

        protected override int SliceCount(int i) => 1;

        protected override BindObjectOperation CreateOps(int i, int j)
        {
            var ops = base.CreateOps(i, j);

            void OpsOnBeforeExecute(HtmlTextureOperation operation, HtmlTextureWrapper wrapper)
            {
                if (!(operation is BindObjectOperation bops)) return;
                bops.Binding = new JsBinding
                {
                    Name = FObj[i]
                };

                for (int jj = 0; jj < FFunc[i].SliceCount; jj++)
                {
                    if (string.IsNullOrWhiteSpace(FFunc[i][jj])) continue;
                    var func = new SimpleReturnObjectBinding
                    {
                        Name = FFunc[i][jj],
                        ReturnObject = _input[i]?[jj]
                    };
                    bops.Binding.AddFunction(func);
                }
            }

            ops.OnBeforeExecute += OpsOnBeforeExecute;

            return ops;
        }

        protected override void UpdateOps(ref BindObjectOperation ops, int i, int j)
        {
            ops.Execute = FBind[i];
            ops.ExecuteOnLoad = FBindOnLoad[i];
            
            if(ops.Binding == null) return;

            for (int jj = 0; jj < FFunc[i].SliceCount; jj++)
            {
                if(!ops.Binding.Functions.ContainsKey(FFunc[i][jj])) continue;
                var func = ops.Binding.Functions[FFunc[i][jj]];

                if (func is SimpleReturnObjectBinding sfunc)
                {
                    sfunc.ReturnObject = _input[i]?[VMath.Zmod(jj, _input[i].Count)];
                }
            }
        }

        public void OnImportsSatisfied()
        {
            _input = new GenericBinSizedInput(Host, new InputAttribute("Return Objects")
            {
                Order = 10,
                BinOrder = 11
            }, Hde.MainLoop);
        }
    }

    [PluginInfo(
        Name = "GetBoundObject",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "Bind, javascript, Vanadium",
        Help = "Get data of bound functions from javascript"
    )]
    public class GetJsBindingNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<HtmlTextureWrapper> FWrapper;

        [Input("Object Name")]
        public ISpread<ISpread<string>> FObj;
        [Input("Function Name")]
        public ISpread<ISpread<string>> FFunc;

        [Output("Output")]
        public ISpread<ISpread<JsBindingFunction>> FOut;
        [Output("Valid")]
        public ISpread<ISpread<bool>> FValid;

        public void Evaluate(int SpreadMax)
        {
            if (FWrapper.IsConnected)
            {
                FOut.SliceCount = FValid.SliceCount = FWrapper.SliceCount;
                for (int i = 0; i < FWrapper.SliceCount; i++)
                {
                    var wrapper = FWrapper[i];

                    if (wrapper == null)
                    {
                        FOut[i].SliceCount = FValid[i].SliceCount = 0;
                        continue;
                    }

                    var slc = SpreadUtils.SpreadMax(FObj[i], FFunc[i]);
                    FOut[i].SliceCount = FValid[i].SliceCount = slc;

                    for (int j = 0; j < slc; j++)
                    {
                        var objname = FObj[i][j];
                        var funcname = FFunc[i][j];
                        if (wrapper.JsBindings.ContainsKey(objname))
                        {
                            var obj = wrapper.JsBindings[objname];
                            if (obj.Functions.ContainsKey(funcname))
                            {
                                FOut[i][j] = obj.Functions[funcname];
                                FValid[i][j] = true;
                            }
                            else
                            {
                                FOut[i][j] = null;
                                FValid[i][j] = false;
                            }
                        }
                        else
                        {
                            FOut[i][j] = null;
                            FValid[i][j] = false;
                        }
                    }
                }
            }
            else
            {
                FOut.SliceCount = FValid.SliceCount = 0;
            }
        }
    }

    [PluginInfo(
        Name = "GetBoundFunction",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "Bind, javascript, Vanadium",
        Help = "Get data of a bound function from javascript"
    )]
    public class SplitJsBindingFuncNode : IPluginEvaluate
    {
        [Input("Function")]
        public Pin<JsBindingFunction> FFunc;
        
        [Output("Arguments")]
        public ISpread<ISpread<ResultFromJs>> FArgs;

        [Output("Invoke Counter")]
        public ISpread<int> FInvokeCount;
        [Output("Invoked", IsBang = true)]
        public ISpread<bool> FInvoked;

        [Output("Valid")]
        public ISpread<bool> FValid;

        public void Evaluate(int SpreadMax)
        {
            if (FFunc.IsConnected)
            {
                FArgs.SliceCount = FInvokeCount.SliceCount = FInvoked.SliceCount = FValid.SliceCount = FFunc.SliceCount;
                for (int i = 0; i < FFunc.SliceCount; i++)
                {
                    if (FFunc[i] == null)
                    {
                        FArgs[i].SliceCount = 0;
                        FInvokeCount[i] = 0;
                        FInvoked[i] = false;
                        FValid[i] = false;
                        continue;
                    }

                    FInvoked[i] = FFunc[i].InvokeCount != FInvokeCount[i];
                    FInvokeCount[i] = FFunc[i].InvokeCount;
                    FValid[i] = true;

                    if (FFunc[i] is SimpleReturnObjectBinding sfunc)
                    {
                        FArgs[i].AssignFrom(sfunc.Result);
                    }
                    else
                    {
                        FArgs[i].SliceCount = 0;
                    }
                }
            }
            else
            {
                FArgs.SliceCount = FInvokeCount.SliceCount = FInvoked.SliceCount = FValid.SliceCount = 0;
            }
        }
    }
}
