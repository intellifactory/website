(function (Global) {
  "use strict";

  // Polyfill

  if (!Date.now) {
    Date.now = function () {
      return new Date().getTime();
    };
  }

  if (!Math.trunc) {
    Math.trunc = function (x) {
      return x < 0 ? Math.ceil(x) : Math.floor(x);
    }
  }

  if (!Object.setPrototypeOf) {
    Object.setPrototypeOf = function (obj, proto) {
      obj.__proto__ = proto;
      return obj;
    }
  }

  Global.IntelliFactory = {
    Runtime: {
      Ctor: function (ctor, typeFunction) {
        ctor.prototype = typeFunction.prototype;
        return ctor;
      },

      Class: function (members, base, statics) {
        var proto = members;
        if (base) {
          proto = new base();
          for (var m in members) { proto[m] = members[m] }
        }
        var typeFunction = function (copyFrom) {
          if (copyFrom) {
            for (var f in copyFrom) { this[f] = copyFrom[f] }
          }
        }
        typeFunction.prototype = proto;
        if (statics) {
          for (var f in statics) { typeFunction[f] = statics[f] }
        }
        return typeFunction;
      },

      Clone: function (obj) {
        var res = {};
        for (var p of Object.getOwnPropertyNames(obj)) { res[p] = obj[p] }
        Object.setPrototypeOf(res, Object.getPrototypeOf(obj));
        return res;
      },

      NewObject:
        function (kv) {
          var o = {};
          for (var i = 0; i < kv.length; i++) {
            o[kv[i][0]] = kv[i][1];
          }
          return o;
        },

      PrintObject:
        function (obj) {
          var res = "{ ";
          var empty = true;
          for (var field of Object.getOwnPropertyNames(obj)) {
            if (empty) {
              empty = false;
            } else {
              res += ", ";
            }
            res += field + " = " + obj[field];
          }
          if (empty) {
            res += "}";
          } else {
            res += " }";
          }
          return res;
        },

      DeleteEmptyFields:
        function (obj, fields) {
          for (var i = 0; i < fields.length; i++) {
            var f = fields[i];
            if (obj[f] === void (0)) { delete obj[f]; }
          }
          return obj;
        },

      GetOptional:
        function (value) {
          return (value === void (0)) ? null : { $: 1, $0: value };
        },

      SetOptional:
        function (obj, field, value) {
          if (value) {
            obj[field] = value.$0;
          } else {
            delete obj[field];
          }
        },

      SetOrDelete:
        function (obj, field, value) {
          if (value === void (0)) {
            delete obj[field];
          } else {
            obj[field] = value;
          }
        },

      Apply: function (f, obj, args) {
        return f.apply(obj, args);
      },

      Bind: function (f, obj) {
        return function () { return f.apply(this, arguments) };
      },

      CreateFuncWithArgs: function (f) {
        return function () { return f(Array.prototype.slice.call(arguments)) };
      },

      CreateFuncWithOnlyThis: function (f) {
        return function () { return f(this) };
      },

      CreateFuncWithThis: function (f) {
        return function () { return f(this).apply(null, arguments) };
      },

      CreateFuncWithThisArgs: function (f) {
        return function () { return f(this)(Array.prototype.slice.call(arguments)) };
      },

      CreateFuncWithRest: function (length, f) {
        return function () { return f(Array.prototype.slice.call(arguments, 0, length).concat([Array.prototype.slice.call(arguments, length)])) };
      },

      CreateFuncWithArgsRest: function (length, f) {
        return function () { return f([Array.prototype.slice.call(arguments, 0, length), Array.prototype.slice.call(arguments, length)]) };
      },

      BindDelegate: function (func, obj) {
        var res = func.bind(obj);
        res.$Func = func;
        res.$Target = obj;
        return res;
      },

      CreateDelegate: function (invokes) {
        if (invokes.length == 0) return null;
        if (invokes.length == 1) return invokes[0];
        var del = function () {
          var res;
          for (var i = 0; i < invokes.length; i++) {
            res = invokes[i].apply(null, arguments);
          }
          return res;
        };
        del.$Invokes = invokes;
        return del;
      },

      CombineDelegates: function (dels) {
        var invokes = [];
        for (var i = 0; i < dels.length; i++) {
          var del = dels[i];
          if (del) {
            if ("$Invokes" in del)
              invokes = invokes.concat(del.$Invokes);
            else
              invokes.push(del);
          }
        }
        return IntelliFactory.Runtime.CreateDelegate(invokes);
      },

      DelegateEqual: function (d1, d2) {
        if (d1 === d2) return true;
        if (d1 == null || d2 == null) return false;
        var i1 = d1.$Invokes || [d1];
        var i2 = d2.$Invokes || [d2];
        if (i1.length != i2.length) return false;
        for (var i = 0; i < i1.length; i++) {
          var e1 = i1[i];
          var e2 = i2[i];
          if (!(e1 === e2 || ("$Func" in e1 && "$Func" in e2 && e1.$Func === e2.$Func && e1.$Target == e2.$Target)))
            return false;
        }
        return true;
      },

      ThisFunc: function (d) {
        return function () {
          var args = Array.prototype.slice.call(arguments);
          args.unshift(this);
          return d.apply(null, args);
        };
      },

      ThisFuncOut: function (f) {
        return function () {
          var args = Array.prototype.slice.call(arguments);
          return f.apply(args.shift(), args);
        };
      },

      ParamsFunc: function (length, d) {
        return function () {
          var args = Array.prototype.slice.call(arguments);
          return d.apply(null, args.slice(0, length).concat([args.slice(length)]));
        };
      },

      ParamsFuncOut: function (length, f) {
        return function () {
          var args = Array.prototype.slice.call(arguments);
          return f.apply(null, args.slice(0, length).concat(args[length]));
        };
      },

      ThisParamsFunc: function (length, d) {
        return function () {
          var args = Array.prototype.slice.call(arguments);
          args.unshift(this);
          return d.apply(null, args.slice(0, length + 1).concat([args.slice(length + 1)]));
        };
      },

      ThisParamsFuncOut: function (length, f) {
        return function () {
          var args = Array.prototype.slice.call(arguments);
          return f.apply(args.shift(), args.slice(0, length).concat(args[length]));
        };
      },

      Curried: function (f, n, args) {
        args = args || [];
        return function (a) {
          var allArgs = args.concat([a === void (0) ? null : a]);
          if (n == 1)
            return f.apply(null, allArgs);
          if (n == 2)
            return function (a) { return f.apply(null, allArgs.concat([a === void (0) ? null : a])); }
          return IntelliFactory.Runtime.Curried(f, n - 1, allArgs);
        }
      },

      Curried2: function (f) {
        return function (a) { return function (b) { return f(a, b); } }
      },

      Curried3: function (f) {
        return function (a) { return function (b) { return function (c) { return f(a, b, c); } } }
      },

      UnionByType: function (types, value, optional) {
        var vt = typeof value;
        for (var i = 0; i < types.length; i++) {
          var t = types[i];
          if (typeof t == "number") {
            if (Array.isArray(value) && (t == 0 || value.length == t)) {
              return { $: i, $0: value };
            }
          } else {
            if (t == vt) {
              return { $: i, $0: value };
            }
          }
        }
        if (!optional) {
          throw new Error("Type not expected for creating Choice value.");
        }
      },

      ScriptBasePath: "./",

      ScriptPath: function (a, f) {
        return this.ScriptBasePath + (this.ScriptSkipAssemblyDir ? "" : a + "/") + f;
      },

      OnLoad:
        function (f) {
          if (!("load" in this)) {
            this.load = [];
          }
          this.load.push(f);
        },

      Start:
        function () {
          function run(c) {
            for (var i = 0; i < c.length; i++) {
              c[i]();
            }
          }
          if ("load" in this) {
            run(this.load);
            this.load = [];
          }
        },
    }
  }

  Global.IntelliFactory.Runtime.OnLoad(function () {
    if (Global.WebSharper && WebSharper.Activator && WebSharper.Activator.Activate)
      WebSharper.Activator.Activate()
  });

  Global.ignore = function() { };
  Global.id = function(x) { return x };
  Global.fst = function(x) { return x[0] };
  Global.snd = function(x) { return x[1] };
  Global.trd = function(x) { return x[2] };

  if (!Global.console) {
    Global.console = {
      count: ignore,
      dir: ignore,
      error: ignore,
      group: ignore,
      groupEnd: ignore,
      info: ignore,
      log: ignore,
      profile: ignore,
      profileEnd: ignore,
      time: ignore,
      timeEnd: ignore,
      trace: ignore,
      warn: ignore
    }
  }
}(self));
;
(function(Global)
{
 "use strict";
 var Client,Highlight,Newsletter,Contact,EventTarget,Node,WebSharper,Operators,Unchecked,Arrays,Element,HTMLElement,WindowOrWorkerGlobalScope,Obj,Strings,hljs,IntelliFactory,Runtime;
 Client=Global.Client=Global.Client||{};
 Highlight=Client.Highlight=Client.Highlight||{};
 Newsletter=Client.Newsletter=Client.Newsletter||{};
 Contact=Client.Contact=Client.Contact||{};
 EventTarget=Global.EventTarget;
 Node=Global.Node;
 WebSharper=Global.WebSharper=Global.WebSharper||{};
 Operators=WebSharper.Operators=WebSharper.Operators||{};
 Unchecked=WebSharper.Unchecked=WebSharper.Unchecked||{};
 Arrays=WebSharper.Arrays=WebSharper.Arrays||{};
 Element=Global.Element;
 HTMLElement=Global.HTMLElement;
 WindowOrWorkerGlobalScope=Global.WindowOrWorkerGlobalScope;
 Obj=WebSharper.Obj=WebSharper.Obj||{};
 Strings=WebSharper.Strings=WebSharper.Strings||{};
 hljs=Global.hljs;
 IntelliFactory=Global.IntelliFactory;
 Runtime=IntelliFactory&&IntelliFactory.Runtime;
 Client.Main=function()
 {
  Highlight.Run();
  Newsletter.SignUpAction();
  Contact.SendMessageAction();
 };
 Highlight.Run=function()
 {
  function a(node,a$1,a$2,a$3)
  {
   hljs.highlightBlock(node);
  }
  self.document.querySelectorAll("code[class^=language-]").forEach(Runtime.CreateFuncWithArgs(function($1)
  {
   return a($1[0],$1[1],$1[2],$1[3]);
  }),void 0);
 };
 Newsletter.SignUpAction=function()
 {
  var button,newsletterForm;
  button=self.document.getElementById("signup");
  newsletterForm=self.document.getElementById("newsletter-form");
  if(!Unchecked.Equals(newsletterForm,null)&&!Unchecked.Equals(newsletterForm,void 0))
   newsletterForm.addEventListener("submit",function(ev)
   {
    return ev.preventDefault();
   });
  if(!Unchecked.Equals(button,null)&&!Unchecked.Equals(button,void 0))
   button.addEventListener("click",function(ev)
   {
    var email,alertList,fd,r;
    ev.preventDefault();
    email=self.document.getElementById("newsletter-input").value;
    return Strings.Trim(email)!==""?(alertList=self.document.getElementById("newsletter-alert-list"),(alertList.replaceChildren.apply(alertList,[]),button.setAttribute("disabled","disabled"),button.classList.add("btn-disabled"),fd=new Global.FormData(),fd.append("email",email),fd.append("type","Blogs"),void(self.fetch("https://api.intellifactory.com/api/newsletter",(r={},r.method="POST",r.body=fd,r)).then(function()
    {
     var successMessage;
     successMessage=self.document.createElement("div");
     successMessage.className="success-alert";
     successMessage.textContent="You have successfully signed up!";
     button.removeAttribute("disabled");
     button.classList.remove("btn-disabled");
     return alertList.appendChild(successMessage);
    }))["catch"](function()
    {
     var errorMessage;
     errorMessage=self.document.createElement("div");
     errorMessage.className="error-alert";
     errorMessage.textContent="Sorry, we could not sign you for the newsletter!";
     button.removeAttribute("disabled");
     button.classList.remove("btn-disabled");
     return alertList.appendChild(errorMessage);
    }))):null;
   });
 };
 Contact.SendMessageAction=function()
 {
  var button,contactForm;
  button=self.document.getElementById("contact-button");
  contactForm=self.document.getElementById("contact-form");
  if(!Unchecked.Equals(contactForm,null)&&!Unchecked.Equals(contactForm,void 0))
   contactForm.addEventListener("submit",function(ev)
   {
    return ev.preventDefault();
   });
  if(!Unchecked.Equals(button,null)&&!Unchecked.Equals(button,void 0))
   button.addEventListener("click",function(ev)
   {
    var emailInput,subjectInput,messageInput,termsInput,o,x,o$1,x$1,o$2,x$2,o$3,x$3,email,subject,message,terms,o$4,x$4,o$5,x$5,o$6,x$6,o$7,x$7,alertList,fd,r;
    ev.preventDefault();
    emailInput=self.document.querySelector("#contact-form *[name=\"email\"]");
    subjectInput=self.document.querySelector("#contact-form *[name=\"subject\"]");
    messageInput=self.document.querySelector("#contact-form *[name=\"message\"]");
    termsInput=self.document.querySelector("#contact-form *[name=\"accept_terms\"]");
    emailInput.classList.remove("input-failed-validation");
    o=(x=emailInput.nextElementSibling,x!==void 0?{
     $:1,
     $0:x
    }:null);
    if(o==null)
     ;
    else
     o.$0.classList.add("hidden");
    subjectInput.classList.remove("input-failed-validation");
    o$1=(x$1=subjectInput.nextElementSibling,x$1!==void 0?{
     $:1,
     $0:x$1
    }:null);
    if(o$1==null)
     ;
    else
     o$1.$0.classList.add("hidden");
    messageInput.classList.remove("input-failed-validation");
    o$2=(x$2=messageInput.nextElementSibling,x$2!==void 0?{
     $:1,
     $0:x$2
    }:null);
    if(o$2==null)
     ;
    else
     o$2.$0.classList.add("hidden");
    o$3=(x$3=termsInput.nextElementSibling,x$3!==void 0?{
     $:1,
     $0:x$3
    }:null);
    if(o$3==null)
     ;
    else
     o$3.$0.classList.remove("text-red");
    email=emailInput.value;
    subject=subjectInput.value;
    message=messageInput.value;
    terms=termsInput.checked;
    if(emailInput.validity.typeMismatch||Strings.Trim(email)==="")
     {
      emailInput.classList.add("input-failed-validation");
      o$4=(x$4=emailInput.nextElementSibling,x$4!==void 0?{
       $:1,
       $0:x$4
      }:null);
      o$4==null?void 0:o$4.$0.classList.remove("hidden");
     }
    if(Strings.Trim(subject)==="")
     {
      subjectInput.classList.add("input-failed-validation");
      o$5=(x$5=subjectInput.nextElementSibling,x$5!==void 0?{
       $:1,
       $0:x$5
      }:null);
      o$5==null?void 0:o$5.$0.classList.remove("hidden");
     }
    if(Strings.Trim(message)==="")
     {
      messageInput.classList.add("input-failed-validation");
      o$6=(x$6=messageInput.nextElementSibling,x$6!==void 0?{
       $:1,
       $0:x$6
      }:null);
      o$6==null?void 0:o$6.$0.classList.remove("hidden");
     }
    if(!terms)
     {
      o$7=(x$7=termsInput.nextElementSibling,x$7!==void 0?{
       $:1,
       $0:x$7
      }:null);
      o$7==null?void 0:o$7.$0.classList.add("text-red");
     }
    return!emailInput.validity.typeMismatch&&Strings.Trim(subject)!==""&&Strings.Trim(message)!==""&&terms?(alertList=self.document.getElementById("contact-alert-list"),(alertList.replaceChildren.apply(alertList,[]),button.setAttribute("disabled","disabled"),button.classList.add("btn-disabled"),fd=new Global.FormData(),fd.append("email",email),fd.append("name",subject),fd.append("message",message),void(self.fetch("https://api.intellifactory.com/api/contact",(r={},r.method="POST",r.body=fd,r)).then(function()
    {
     var modal;
     modal=self.document.querySelector("#contact-form .modal");
     self.document.querySelector("#contact-form .modal .modal-button").addEventListener("click",function()
     {
      var modal$1;
      modal$1=self.document.querySelector("#contact-form .modal");
      emailInput.value="";
      subjectInput.value="";
      messageInput.value="";
      termsInput.checked=false;
      modal$1.classList.add("hidden");
      button.removeAttribute("disabled");
      return button.classList.remove("btn-disabled");
     });
     return modal.classList.remove("hidden");
    }))["catch"](function()
    {
     var errorMessage;
     errorMessage=self.document.createElement("div");
     errorMessage.className="error-alert";
     errorMessage.textContent="Sorry, we could not sign you for the newsletter!";
     button.removeAttribute("disabled");
     button.classList.remove("btn-disabled");
     return alertList.appendChild(errorMessage);
    }))):null;
   });
 };
 Operators.FailWith=function(msg)
 {
  throw new Global.Error(msg);
 };
 Unchecked.Equals=function(a,b)
 {
  var m,eqR,k,k$1;
  if(a===b)
   return true;
  else
   {
    m=typeof a;
    if(m=="object")
    {
     if(a===null||a===void 0||b===null||b===void 0||!Unchecked.Equals(typeof b,"object"))
      return false;
     else
      if("Equals"in a)
       return a.Equals(b);
      else
       if("Equals"in b)
        return false;
       else
        if(a instanceof Global.Array&&b instanceof Global.Array)
         return Unchecked.arrayEquals(a,b);
        else
         if(a instanceof Global.Date&&b instanceof Global.Date)
          return Unchecked.dateEquals(a,b);
         else
          {
           eqR=[true];
           for(var k$2 in a)if(function(k$3)
           {
            eqR[0]=!a.hasOwnProperty(k$3)||b.hasOwnProperty(k$3)&&Unchecked.Equals(a[k$3],b[k$3]);
            return!eqR[0];
           }(k$2))
            break;
           if(eqR[0])
            {
             for(var k$3 in b)if(function(k$4)
             {
              eqR[0]=!b.hasOwnProperty(k$4)||a.hasOwnProperty(k$4);
              return!eqR[0];
             }(k$3))
              break;
            }
           return eqR[0];
          }
    }
    else
     return m=="function"&&("$Func"in a?a.$Func===b.$Func&&a.$Target===b.$Target:"$Invokes"in a&&"$Invokes"in b&&Unchecked.arrayEquals(a.$Invokes,b.$Invokes));
   }
 };
 Unchecked.arrayEquals=function(a,b)
 {
  var eq,i;
  if(Arrays.length(a)===Arrays.length(b))
   {
    eq=true;
    i=0;
    while(eq&&i<Arrays.length(a))
     {
      !Unchecked.Equals(Arrays.get(a,i),Arrays.get(b,i))?eq=false:void 0;
      i=i+1;
     }
    return eq;
   }
  else
   return false;
 };
 Unchecked.dateEquals=function(a,b)
 {
  return a.getTime()===b.getTime();
 };
 Arrays.get=function(arr,n)
 {
  Arrays.checkBounds(arr,n);
  return arr[n];
 };
 Arrays.length=function(arr)
 {
  return arr.dims===2?arr.length*arr.length:arr.length;
 };
 Arrays.checkBounds=function(arr,n)
 {
  if(n<0||n>=arr.length)
   Operators.FailWith("Index was outside the bounds of the array.");
 };
 Obj=WebSharper.Obj=Runtime.Class({
  Equals:function(obj)
  {
   return this===obj;
  }
 },null,Obj);
 Strings.Trim=function(s)
 {
  return s.replace(new Global.RegExp("^\\s+"),"").replace(new Global.RegExp("\\s+$"),"");
 };
 Runtime.OnLoad(function()
 {
  Client.Main();
 });
}(self));


if (typeof IntelliFactory !=='undefined') {
  IntelliFactory.Runtime.ScriptBasePath = '/Content/';
  IntelliFactory.Runtime.Start();
}
