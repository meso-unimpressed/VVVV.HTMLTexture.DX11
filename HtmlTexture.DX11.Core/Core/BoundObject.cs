﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using Chromium.Remote;
using Chromium.Remote.Event;
using md.stdl.Coding;
using md.stdl.Interfaces;

namespace VVVV.HtmlTexture.DX11.Core
{
    public delegate void JsFunctionEventHandler(JsBindingFunction func, HtmlTextureWrapper wrapper);

    //TODO: possible memory leak undisposed disposables, but wrong disposition might be fatal
    public class JsBinding : ICloneable<JsBinding>
    {
        public string Name { get; set; }
        public bool Executed { get; private set; }
        public HtmlTextureWrapper Wrapper { get; private set; }
        public Dictionary<string, JsBindingFunction> Functions { get; } = new Dictionary<string, JsBindingFunction>();

        public T AddFunction<T>(T func) where T : JsBindingFunction
        {
            Functions.UpdateGeneric(func.Name, func);
            return func;
        }

        public void MigrateFunctions(JsBinding other)
        {
            if(!other.Name.Equals(Name, StringComparison.InvariantCulture)) return;
            foreach (var otherFunction in other.Functions.Values.ToArray())
            {
                AddFunction(otherFunction);
            }
        }

        public void Bind(HtmlTextureWrapper wrapper, CfrV8Handler v8handler)
        {
            Wrapper = wrapper;
            Wrapper.OnMainLoopBegin += (sender, args) => Executed = false;
            
            v8handler.Execute += JavascriptCallback;
        }

        public void JavascriptCallback(object sender, CfrV8HandlerExecuteEventArgs e)
        {
            if(!Functions.ContainsKey(e.Name)) return;
            var func = Functions[e.Name];
            func.Arguments = e.Arguments;
            func.LastEventArgs = e;
            e.SetReturnValue(func.Invoke(e, this, Wrapper));
        }

        public JsBinding Copy()
        {
            var res = new JsBinding { Name = Name };
            foreach (var funcpair in Functions)
            {
                res.Functions.Add(funcpair.Key, funcpair.Value);
            }
            return res;
        }

        public object Clone()
        {
            return Copy();
        }
        
        //~JsBinding()
        //{
        //    if (_prevHandler != null)
        //    {
        //        _prevHandler.Execute -= JavascriptCallback;
        //        _prevHandler.Dispose();
        //    }
        //}
    }

    //TODO: possible memory leak undisposed disposables, but wrong disposition might be fatal
    public abstract class JsBindingFunction : ICloneable<JsBindingFunction>
    {
        public string Name { get; set; }
        public CfrV8Value[] Arguments { get; set; } = new CfrV8Value[0];
        public CfrV8HandlerExecuteEventArgs LastEventArgs { get; set; }
        public int InvokeCount;
        public event JsFunctionEventHandler Invoked;

        public virtual CfrV8Value Invoke(CfrV8HandlerExecuteEventArgs args, JsBinding binding, HtmlTextureWrapper wrapper)
        {
            var res = Function(args, binding, wrapper);
            Invoked?.Invoke(this, wrapper);
            InvokeCount++;
            return res;
        }

        protected abstract CfrV8Value Function(CfrV8HandlerExecuteEventArgs args, JsBinding binding, HtmlTextureWrapper wrapper);

        public abstract JsBindingFunction Copy();

        public object Clone()
        {
            return Copy();
        }

        //~JsBindingFunction()
        //{
        //    foreach (var value in Arguments)
        //    {
        //        value.Dispose();
        //    }
        //}
    }

    public class SimpleReturnObjectBinding : JsBindingFunction
    {
        public object ReturnObject { get; set; }
        public ResultFromJs[] Result { get; set; } = new ResultFromJs[0];
        protected override CfrV8Value Function(CfrV8HandlerExecuteEventArgs args, JsBinding binding, HtmlTextureWrapper wrapper)
        {
            Result = Arguments.Select(a =>
            {
                var res = new ResultFromJs
                {
                    Result = a.CfrV8ValueToString(),
                    Results = a.CfrV8ValueToStringArray(),
                    Error = ""
                };
                return res;
            }).ToArray();
            return ReturnObject.V8Serialize();
        }

        public override JsBindingFunction Copy()
        {
            return new SimpleReturnObjectBinding
            {
                Name = Name,
                ReturnObject = ReturnObject
            };
        }
    }
}
