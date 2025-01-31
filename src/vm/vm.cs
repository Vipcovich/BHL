using System;
using System.Collections.Generic;

namespace bhl {

public enum UpvalMode
{
  STRONG = 0,
  COPY   = 1
}

public class VM : INamedResolver
{
  //NOTE: why -2? we reserve some space before int.MaxValue so that 
  //      increasing some ip couple of times after it was assigned 
  //      a 'STOP_IP' value won't overflow int.MaxValue
  public const int STOP_IP = int.MaxValue - 2;

  public struct Region
  {
    public Frame frame;
    public IDeferSupport defer_support;
    //NOTE: if current ip is not within *inclusive* range of these values 
    //      the frame context execution is considered to be done
    public int min_ip;
    public int max_ip;

    public Region(Frame frame, IDeferSupport defer_support, int min_ip = -1, int max_ip = STOP_IP)
    {
      this.frame = frame;
      this.defer_support = defer_support;
      this.min_ip = min_ip;
      this.max_ip = max_ip;
    }
  }

  public class ExecState
  {
    internal int ip;
    internal Coroutine coroutine;
    internal FixedStack<Region> regions = new FixedStack<Region>(32);
    internal FixedStack<Frame> frames = new FixedStack<Frame>(256);
    public ValStack stack;
  }

  public struct FiberRef
  {
    int id;
    Fiber fiber;

    public FiberRef(Fiber fiber)
    {
      this.id = (fiber?.id ?? 0);
      this.fiber = fiber;
    }

    public Fiber Get()
    {
      return (fiber?.id ?? 0) == id ? fiber : null;
    }

    public void Set(Fiber fiber)
    {
      this.id = (fiber?.id ?? 0);
      this.fiber = fiber;
    }

    public void Clear()
    {
      this.fiber = null;
    }
  }

  public class Fiber : ITask
  {
    public VM vm;

    internal FiberRef parent;
    public FiberRef Parent => parent;

    internal List<FiberRef> children = new List<FiberRef>(); 

    public IReadOnlyList<FiberRef> Children => children;

    //NOTE: -1 means it's in released state,
    //      public only for inspection
    public int refs;

    internal int id;
    public int Id => id;

    internal int tick;

    internal ExecState exec = new ExecState();

    public VM.Frame frame0 {
      get {
        return exec.frames[0];
      }
    }
    public FixedStack<Val> result = new FixedStack<Val>(Frame.MAX_STACK);
    
    public BHS status;

    static public Fiber New(VM vm)
    {
      Fiber fb;
      if(vm.fibers_pool.stack.Count == 0)
      {
        ++vm.fibers_pool.miss;
        fb = new Fiber(vm);
      }
      else
      {
        ++vm.fibers_pool.hits;
        fb = vm.fibers_pool.stack.Pop();

        if(fb.refs != -1)
          throw new Exception("Expected to be released, refs " + fb.refs);
      }

      fb.refs = 1;
      fb.parent.Clear();
      fb.children.Clear();

      //0 index frame used for return values consistency
      fb.exec.frames.Push(Frame.New(vm));

      return fb;
    }

    static void Del(Fiber fb)
    {
      if(fb.refs != 0)
        throw new Exception("Freeing invalid object, refs " + fb.refs);

      fb.refs = -1;

      fb.Clear();
      fb.vm.fibers_pool.stack.Push(fb);
    }

    //NOTE: use New() instead
    internal Fiber(VM vm)
    {
      this.vm = vm;
    }

    internal void Clear()
    {
      if(exec.coroutine != null)
      {
        CoroutinePool.Del(exec.frames.Peek(), exec, exec.coroutine);
        exec.coroutine = null;
      }

      //we need to copy 0 index frame returned values 
      {
        result.Clear();
        for(int c=0;c<frame0._stack.Count;++c)
          result.Push(frame0._stack[c]);
        //let's clear the frame's stack so that values 
        //won't be released below
        frame0._stack.Clear();
      }

      for(int i=exec.frames.Count;i-- > 0;)
      {
        var frm = exec.frames[i];
        frm.ExitScope(null, exec);
      }

      //NOTE: we need to release frames only after we actually exited their scopes
      for(int i=exec.frames.Count;i-- > 0;)
      {
        var frm = exec.frames[i];
        frm.Release();
      }

      exec.regions.Clear();
      exec.frames.Clear();

      tick = 0;
    }

    internal void AddChild(Fiber fb)
    {
      fb.parent.Set(this);
      children.Add(new FiberRef(fb));
    }

    public void Retain()
    {
      if(refs == -1)
        throw new Exception("Invalid state(-1)");
      ++refs;
    }

    public void Release()
    {
      if(refs == -1)
        throw new Exception("Invalid state(-1)");
      if(refs == 0)
        throw new Exception("Double free(0)");

      --refs;
      if(refs == 0)
        Del(this);
    }

    public bool IsStopped()
    {
      return exec.ip >= STOP_IP;
    }

    static void GetCalls(VM.ExecState exec, List<VM.Frame> calls, int offset = 0)
    {
      for(int i=offset;i<exec.frames.Count;++i)
        calls.Add(exec.frames[i]);
    }

    public void GetStackTrace(List<VM.TraceItem> info)
    {
      var calls = new List<VM.Frame>();
      int coroutine_ip = -1; 
      GetCalls(exec, calls, offset: 1/*let's skip 0 fake frame*/);
      TryGetTraceInfo(exec.coroutine, ref coroutine_ip, calls);

      for(int i=0;i<calls.Count;++i)
      {
        var frm = calls[i];

        var item = new TraceItem(); 

        //NOTE: information about frame ip is taken from the 'next' frame, however 
        //      for the last frame we have a special case. In this case there's no
        //      'next' frame and we should consider taking ip from Fiber or an active
        //      coroutine
        if(i == calls.Count-1)
        {
          item.ip = coroutine_ip == -1 ? frm.fb.exec.ip : coroutine_ip;
        }
        else
        {
          //NOTE: retrieving last ip for the current Frame which 
          //      turns out to be return_ip assigned to the next Frame
          var next = calls[i+1];
          item.ip = next.return_ip;
        }

        if(frm.module != null)
        {
          var fsymb = TryMapIp2Func(frm.module, calls[i].start_ip);
          //NOTE: if symbol is missing it's a lambda
          if(fsymb == null) 
            item.file = frm.module.name + ".bhl";
          else
            item.file = fsymb._module.name + ".bhl";
          item.func = fsymb == null ? "?" : fsymb.name;
          item.line = frm.module.ip2src_line.TryMap(item.ip);
        }
        else
        {
          item.file = "?";
          item.func = "?";
        }

        info.Insert(0, item);
      }
    }

    public string GetStackTrace()
    {
      var trace = new List<TraceItem>();
      GetStackTrace(trace);
      return Error.ToString(trace);
    }

    static bool TryGetTraceInfo(ICoroutine i, ref int ip, List<VM.Frame> calls)
    {
      if(i is SeqBlock si)
      {
        GetCalls(si.exec, calls);
        if(!TryGetTraceInfo(si.exec.coroutine, ref ip, calls))
          ip = si.exec.ip;
        return true;
      }
      else if(i is ParalBranchBlock bi)
      {
        GetCalls(bi.exec, calls);
        if(!TryGetTraceInfo(bi.exec.coroutine, ref ip, calls))
          ip = bi.exec.ip;
        return true;
      }
      else if(i is ParalBlock pi && pi.i < pi.branches.Count)
        return TryGetTraceInfo(pi.branches[pi.i], ref ip, calls);
      else if(i is ParalAllBlock pai && pai.i < pai.branches.Count)
        return TryGetTraceInfo(pai.branches[pai.i], ref ip, calls);
      else
        return false;
    }

    bool ITask.Tick()
    {
      return vm.Tick(this);
    }

    void ITask.Stop()
    {
      vm.Stop(this);
    }
  }

  public class Frame : IDeferSupport
  {
    public const int MAX_LOCALS = 64;
    public const int MAX_STACK = 32;
    
    //NOTE: -1 means it's in released state,
    //      public only for inspection
    public int refs;

    public VM vm;
    public Fiber fb;
    public CompiledModule module;

    public byte[] bytecode;
    public List<Const> constants;
    public ValStack locals = new ValStack(MAX_LOCALS);
    public ValStack _stack = new ValStack(MAX_STACK);
    public int start_ip;
    public int return_ip;
    public Frame origin;
    public ValStack origin_stack;
    public List<DeferBlock> defers;

    static public Frame New(VM vm)
    {
      Frame frm;
      if(vm.frames_pool.stack.Count == 0)
      {
        ++vm.frames_pool.miss;
        frm = new Frame(vm);
      }
      else
      {
        ++vm.frames_pool.hits;
        frm = vm.frames_pool.stack.Pop();

        if(frm.refs != -1)
          throw new Exception("Expected to be released, refs " + frm.refs);
      }

      frm.refs = 1;

      return frm;
    }

    static void Del(Frame frm)
    {
      if(frm.refs != 0)
        throw new Exception("Freeing invalid object, refs " + frm.refs);

      //Console.WriteLine("DEL " + frm.GetHashCode() + " "/* + Environment.StackTrace*/);
      frm.refs = -1;

      frm.Clear();
      frm.vm.frames_pool.stack.Push(frm);
    }

    //NOTE: use New() instead
    internal Frame(VM vm)
    {
      this.vm = vm;
    }

    public void Init(Frame origin, ValStack origin_stack, int start_ip)
    {
      Init(
        origin.fb, 
        origin,
        origin_stack,
        origin.module, 
        origin.constants, 
        origin.bytecode, 
        start_ip
      );
    }

    public void Init(Fiber fb, Frame origin, ValStack origin_stack, CompiledModule module, int start_ip)
    {
      Init(
        fb, 
        origin,
        origin_stack,
        module, 
        module.constants, 
        module.bytecode, 
        start_ip
      );
    }

    internal void Init(Fiber fb, Frame origin, ValStack origin_stack, CompiledModule module, List<Const> constants, byte[] bytecode, int start_ip)
    {
      this.fb = fb;
      this.origin = origin;
      this.origin_stack = origin_stack;
      this.module = module;
      this.constants = constants;
      this.bytecode = bytecode;
      this.start_ip = start_ip;
      this.return_ip = -1;
    }

    internal void Clear()
    {
      for(int i=locals.Count;i-- > 0;)
      {
        var val = locals[i];
        if(val != null)
          val.RefMod(RefOp.DEC | RefOp.USR_DEC);
      }
      locals.Clear();

      for(int i=_stack.Count;i-- > 0;)
      {
        var val = _stack[i];
        val.RefMod(RefOp.DEC | RefOp.USR_DEC);
      }
      _stack.Clear();

      if(defers != null)
        defers.Clear();
    }

    public void RegisterDefer(DeferBlock dfb)
    {
      if(defers == null)
        defers = new List<DeferBlock>();

      defers.Add(dfb);
      //for debug
      //if(dfb.frm != this)
      //  throw new Exception("INVALID DEFER BLOCK: mine " + GetHashCode() + ", other " + dfb.frm.GetHashCode() + " " + fb.GetStackTrace());
    }

    public void ExitScope(VM.Frame _, ExecState exec)
    {
      DeferBlock.ExitScope(defers, exec);
    }

    public void Retain()
    {
      //Console.WriteLine("RTN " + GetHashCode() + " " + Environment.StackTrace);

      if(refs == -1)
        throw new Exception("Invalid state(-1)");
      ++refs;
    }

    public void Release()
    {
      //Console.WriteLine("REL " + refs + " " + GetHashCode() + " " + Environment.StackTrace);

      if(refs == -1)
        throw new Exception("Invalid state(-1)");
      if(refs == 0)
        throw new Exception("Double free(0)");

      --refs;
      if(refs == 0)
        Del(this);
    }
  }

  public class FuncPtr : IValRefcounted
  {
    //NOTE: -1 means it's in released state,
    //      public only for quick inspection
    public int _refs;

    public int refs => _refs; 

    public VM vm;

    public CompiledModule module;
    public int func_ip;
    public FuncSymbolNative native;
    public FixedStack<Val> upvals = new FixedStack<Val>(Frame.MAX_LOCALS);

    static public FuncPtr New(VM vm)
    {
      FuncPtr ptr;
      if(vm.fptrs_pool.stack.Count == 0)
      {
        ++vm.fptrs_pool.miss;
        ptr = new FuncPtr(vm);
      }
      else
      {
        ++vm.fptrs_pool.hits;
        ptr = vm.fptrs_pool.stack.Pop();

        if(ptr._refs != -1)
          throw new Exception("Expected to be released, refs " + ptr._refs);
      }

      ptr._refs = 1;

      return ptr;
    }

    static void Del(FuncPtr ptr)
    {
      if(ptr._refs != 0)
        throw new Exception("Freeing invalid object, refs " + ptr._refs);

      //Console.WriteLine("DEL " + ptr.GetHashCode() + " " + Environment.StackTrace);
      ptr._refs = -1;

      ptr.Clear();
      ptr.vm.fptrs_pool.stack.Push(ptr);
    }

    //NOTE: use New() instead
    internal FuncPtr(VM vm)
    {
      this.vm = vm;
    }

    public void Init(Frame origin, int func_ip)
    {
      this.module = origin.module;
      this.func_ip = func_ip;
      this.native = null;
    }

    public void Init(CompiledModule module, int func_ip)
    {
      this.module = module;
      this.func_ip = func_ip;
      this.native = null;
    }

    public void Init(FuncSymbolNative native)
    {
      this.module = null;
      this.func_ip = -1;
      this.native = native;
    }

    public void Init(FuncAddr addr)
    {
      this.module = addr.module;
      this.func_ip = addr.ip;
      this.native = null;
    }

    void Clear()
    {
      this.module = null;
      this.func_ip = -1;
      this.native = null;
      for(int i=upvals.Count;i-- > 0;)
      {
        var val = upvals[i];
        //NOTE: let's check if it exists
        if(val != null)
          val.RefMod(RefOp.DEC | RefOp.USR_DEC);
      }
      upvals.Clear();
    }

    public void Retain()
    {
      //Console.WriteLine("RTN " + GetHashCode() + " " + Environment.StackTrace);

      if(_refs == -1)
        throw new Exception("Invalid state(-1)");
      ++_refs;
    }

    public void Release()
    {
      //Console.WriteLine("REL " + GetHashCode() + " " + Environment.StackTrace);

      if(_refs == -1)
        throw new Exception("Invalid state(-1)");
      if(_refs == 0)
        throw new Exception("Double free(0)");

      --_refs;
      if(_refs == 0)
        Del(this);
    }

    public Frame MakeFrame(VM vm, Frame curr_frame, ValStack curr_stack)
    {
      var frm = Frame.New(vm);
      if(module != null)
        frm.Init(curr_frame.fb, curr_frame, curr_stack, module, func_ip);
      else
        frm.Init(curr_frame, curr_stack, func_ip);

      for(int i=0;i<upvals.Count;++i)
      {
        var upval = upvals[i];
        if(upval != null)
        {
          frm.locals.Resize(i+1);
          upval.RefMod(RefOp.USR_INC | RefOp.INC);
          frm.locals[i] = upval;
        }
      }
      return frm;
    }

    public override string ToString()
    {
      return "(FPTR refs:" + _refs + ",upvals:" + upvals.Count + " " + this.GetHashCode() + ")"; 
    }
  }

  public struct TraceItem
  {
    public string file;
    public string func;
    public int line;
    public int ip; 
  }

  public class Error : Exception
  {
    public List<TraceItem> trace;

    public Error(List<TraceItem> trace, Exception e)
      : base(ToString(trace), e)
    {
      this.trace = trace;
    }

    static public string ToString(List<TraceItem> trace)
    {
      string s = "\n";
      foreach(var t in trace)
        s += "at " + t.func + "(..) +" + t.ip + " in " + t.file + ":" + t.line + "\n";
      return s;
    }
  }

  public class Pool<T> where T : class
  {
    internal Stack<T> stack = new Stack<T>();
    internal int hits;
    internal int miss;

    public int HitCount {
      get { return hits; }
    }

    public int MissCount {
      get { return miss; }
    }

    public int IdleCount {
      get { return stack.Count; }
    }

    public int BusyCount {
      get { return miss - IdleCount; }
    }
  }

  //TODO: can we make Module a key instead of a string?
  Dictionary<string, CompiledModule> compiled_mods = new Dictionary<string, CompiledModule>();

  internal class LoadingModule
  {
    internal string name;
    internal CompiledModule module;
  }
  List<LoadingModule> loading_modules = new List<LoadingModule>();

  Types types;

  //TODO: add support for native funcs?
  public struct FuncAddr
  {
    public CompiledModule module;
    public FuncSymbolScript fs;
    public int ip;
  }

  public struct VarAddr
  {
    public CompiledModule module;
    public VariableSymbol vs;
    public Val val;
  }

  public struct SymbolSpec : IEquatable<SymbolSpec>
  {
    public string module;
    public string path;

    public SymbolSpec(string module, string path)
    {
      this.module = module;
      this.path = path;
    }

    public bool Equals(SymbolSpec other)
    {
      return module == other.module && path == other.path;
    }

    public override bool Equals(object obj)
    {
      return obj is SymbolSpec other && Equals(other);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(module, path);
    }
  }

  public struct ModuleSymbol
  {
    public CompiledModule cm;
    public Symbol symbol;
  }

  int fibers_ids = 0;
  List<Fiber> fibers = new List<Fiber>();
  public Fiber last_fiber = null;

  public delegate void OnNewFiberCb(Fiber fb);
  public event OnNewFiberCb OnNewFiber;

  IModuleLoader loader;

  public delegate void ClassCreator(VM.Frame frm, ref Val res, IType type);

  public class ValPool : Pool<Val>
  {
    //NOTE: used for debug tracking of not-freed Vals
    internal struct Tracking
    {
      internal Val v;
      internal string stack_trace;
    }
    internal List<Tracking> debug_track = new List<Tracking>();

    public void Alloc(VM vm, int num)
    {
      for(int i=0;i<num;++i)
      {
        ++miss;
        var tmp = new Val(vm); 
        stack.Push(tmp);
      }
    }

    public string Dump()
    {
      string res = "=== Val POOL ===\n";
      res += "busy:" + BusyCount + " idle:" + IdleCount + "\n";

      var dvs = new Val[stack.Count];
      stack.CopyTo(dvs, 0);
      for(int i=dvs.Length;i-- > 0;)
      {
        var v = dvs[i];
        res += v + " (refs:" + v._refs + ") " + v.GetHashCode() + "\n"; 
      }

      if(debug_track.Count > 0)
      {
        var dangling = new List<Tracking>();
        foreach(var t in debug_track)
          if(t.v._refs != -1)
            dangling.Add(t);

        res += "== dangling:" + dangling.Count + " ==\n";
        foreach(var t in dangling)
          res += t.v + " (refs:" + t.v._refs + ") " + t.v.GetHashCode() + "\n" + t.stack_trace + "\n<<<<<\n"; 
      }

      return res;
    }
  }

  public ValPool vals_pool = new ValPool();
  public Pool<ValList> vlsts_pool = new Pool<ValList>();
  public Pool<ValMap> vmaps_pool = new Pool<ValMap>();
  public Pool<Frame> frames_pool = new Pool<Frame>();
  public Pool<Fiber> fibers_pool = new Pool<Fiber>();
  public Pool<FuncPtr> fptrs_pool = new Pool<FuncPtr>();
  public CoroutinePool coro_pool = new CoroutinePool();

  //fake frame used for module's init code
  Frame init_frame;

  //special case 'null' value
  Val null_val = null;
  public Val Null {
    get {
      null_val.Retain();
      return null_val;
    }
  }

  public const int EXIT_OFFSET = 2;

  public VM(Types types = null, IModuleLoader loader = null)
  {
    if(types == null)
      types = new Types();
    this.types = types;
    this.loader = loader;

    init_frame = new Frame(this);

    null_val = new Val(this);
    null_val.SetObj(null, Types.Null);
    //NOTE: we don't want to store it in the values pool,
    //      still we need to retain it so that it's never 
    //      accidentally released when pushed/popped
    null_val.Retain();
  }

  public bool LoadModule(string module_name)
  {
    //Console.WriteLine("==START LOAD " + module_name);
    if(loading_modules.Count > 0)
      throw new Exception("Already loading modules");

    //let's check if it's already loaded
    if(!TryAddToLoadingList(module_name))
      return true;

    if(loading_modules.Count == 0)
      return false;

    //NOTE: registering modules in reverse order
    for(int i=loading_modules.Count;i-- > 0;)
      FinishRegistration(loading_modules[i].module);
    loading_modules.Clear();

    return true;
  }

  //NOTE: this method is public only for testing convenience
  public void LoadModule(CompiledModule cm)
  {
    BeginRegistration(cm);
    FinishRegistration(cm);
  }

  //NOTE: this method is public only for testing convenience
  public CompiledModule FindModule(string module_name)
  {
    CompiledModule cm;
    compiled_mods.TryGetValue(module_name, out cm);
    return cm;
  }

  //NOTE: returns false if module is already loaded
  bool TryAddToLoadingList(string module_name)
  {
    //let's check if it's already available
    if(FindModuleNamespace(module_name) != null)
      return false;

    //let's check if it's already loading
    foreach(var tmp in loading_modules)
      if(tmp.name == module_name)
        return false;

    var lm = new LoadingModule();
    lm.name = module_name;
    loading_modules.Add(lm);

    //NOTE: passing self as a type proxies 'resolver'
    var loaded = loader.Load(module_name, this, OnImport);

    //if no such a module let's remove it from the loading list
    if(loaded == null)
    {
      loading_modules.Remove(lm);
    }
    else
    {
      lm.module = loaded;

      BeginRegistration(loaded);
    }

    return true;
  }

  void OnImport(string origin_module, string import_name)
  {
    TryAddToLoadingList(import_name);
  }

  void BeginRegistration(CompiledModule cm)
  {
    //NOTE: for simplicity we add it to the modules at once,
    //      this is probably a bit 'smelly' but makes further
    //      symbols setup logic easier
    compiled_mods[cm.name] = cm;

    //let's init all our own global variables
    for(int g=0;g<cm.module.local_gvars_num;++g)
      cm.gvars[g] = Val.New(this); 
  }

  void FinishRegistration(CompiledModule cm)
  {
    SetupModule(cm);
    ExecInitCode(cm);
    ExecModuleInitFunc(cm);
  }

  Namespace FindModuleNamespace(string module_name)
  {
    var rm = types.FindRegisteredModule(module_name);
    if(rm != null)
      return rm.ns;

    CompiledModule cm;
    if(compiled_mods.TryGetValue(module_name, out cm))
      return cm.ns;
    return null;
  }

  void SetupModule(CompiledModule cm)
  {
    foreach(var imp in cm.imports)
      cm.ns.Link(FindModuleNamespace(imp));

    int gvars_offset = cm.module.local_gvars_num;
    foreach(var imp in cm.imports)
    {
      CompiledModule imp_mod;
      //TODO: what about 'native' modules?
      if(!compiled_mods.TryGetValue(imp, out imp_mod))
        continue;
      
      //NOTE: taking only local imported module's gvars
      for(int g=0;g<imp_mod.module.local_gvars_num;++g)
      {
        var imp_gvar = imp_mod.gvars[g];
        imp_gvar.Retain();
        cm.gvars[gvars_offset] = imp_gvar;
        ++gvars_offset;
      }
    }

    cm.ns.SetupSymbols();

    cm.ns.ForAllLocalSymbols(delegate(Symbol s)
      {
        if(s is ClassSymbol cs)
        {
          foreach(var kv in cs._vtable)
          {
            if(kv.Value is FuncSymbolScript vfs)
              PrepareFuncSymbol(cm, vfs);
          }
        }
        else if(s is FuncSymbolScript fs && !(fs.scope is InterfaceSymbol))
          PrepareFuncSymbol(cm, fs);
      }
    );
  }

  void PrepareFuncSymbol(CompiledModule cm, FuncSymbolScript fss)
  {
    if(fss._module == null)
      fss._module = compiled_mods[fss.GetNamespace().module.name];

    if(fss.ip_addr == -1)
      throw new Exception("Func ip_addr is not set: " + fss.GetFullPath());
  }

  public void UnloadModule(string module_name)
  {
    CompiledModule m;
    if(!compiled_mods.TryGetValue(module_name, out m))
      return;

    for(int i=0;i<m.gvars.Count;++i)
    {
      var val = m.gvars[i];
      val.Release();
    }
    m.gvars.Clear();

    compiled_mods.Remove(module_name);
  }

  public void UnloadModules()
  {
    var keys = new List<string>();
    foreach(var kv in compiled_mods)
      keys.Add(kv.Key);
    foreach(string key in keys)
      UnloadModule(key);
  }

  void ExecInitCode(CompiledModule module)
  {
    var bytecode = module.initcode;
    if(bytecode == null || bytecode.Length == 0)
      return;

    var constants = module.constants;

    int ip = 0;

    while(ip < bytecode.Length)
    {
      var opcode = (Opcodes)bytecode[ip];
      //Util.Debug("EXEC INIT " + opcode);
      switch(opcode)
      {
        //NOTE: operates on global vars
        case Opcodes.DeclVar:
        {
          int var_idx = (int)Bytecode.Decode8(bytecode, ref ip);
          int type_idx = (int)Bytecode.Decode24(bytecode, ref ip);
          var type = constants[type_idx].itype.Get();

          InitDefaultVal(type, module.gvars[var_idx]);
        }
        break;
        //NOTE: operates on global vars
        case Opcodes.SetVar:
        {
          int var_idx = (int)Bytecode.Decode8(bytecode, ref ip);

          var new_val = init_frame._stack.Pop();
          module.gvars.Assign(this, var_idx, new_val);
          new_val.Release();
        }
        break;
        case Opcodes.Constant:
        {
          int const_idx = (int)Bytecode.Decode24(bytecode, ref ip);
          var cn = constants[const_idx];
          var cv = cn.ToVal(this);
          init_frame._stack.Push(cv);
        }
        break;
        case Opcodes.Add:
        case Opcodes.Sub:
        case Opcodes.Div:
        case Opcodes.Mod:
        case Opcodes.Mul:
        case Opcodes.And:
        case Opcodes.Or:
        case Opcodes.BitAnd:
        case Opcodes.BitOr:
        case Opcodes.Equal:
        case Opcodes.NotEqual:
        case Opcodes.LT:
        case Opcodes.LTE:
        case Opcodes.GT:
        case Opcodes.GTE:
        {
          ExecuteBinaryOp(opcode, init_frame._stack);
        }
        break;
        case Opcodes.UnaryNot:
        case Opcodes.UnaryNeg:
        {
          ExecuteUnaryOp(opcode, init_frame._stack);
        }
        break;
        case Opcodes.New:
        {
          int type_idx = (int)Bytecode.Decode24(bytecode, ref ip);
          var type = constants[type_idx].itype.Get();
          HandleNew(init_frame, init_frame._stack, type);
        }
        break;
        case Opcodes.SetAttrInplace:
        {
          int fld_idx = (int)Bytecode.Decode16(bytecode, ref ip);
          var val = init_frame._stack.Pop();
          var obj = init_frame._stack.Peek();
          var class_symb = (ClassSymbol)obj.type;
          var field_symb = (FieldSymbol)class_symb._all_members[fld_idx];
          field_symb.setter(init_frame, ref obj, val, field_symb);
          val.Release();
        }
        break;
        case Opcodes.ArrAddInplace:
        {
          var self = init_frame._stack[init_frame._stack.Count - 2];
          self.Retain();
          var class_type = ((ArrayTypeSymbol)self.type);
          var status = BHS.SUCCESS;
          ((FuncSymbolNative)class_type._all_members[0]).cb(init_frame, init_frame._stack, new FuncArgsInfo(), ref status);
          init_frame._stack.Push(self);
        }
        break;
        case Opcodes.MapAddInplace:
        {
          var self = init_frame._stack[init_frame._stack.Count - 3];
          self.Retain();
          var class_type = ((MapTypeSymbol)self.type);
          var status = BHS.SUCCESS;
          ((FuncSymbolNative)class_type._all_members[0]).cb(init_frame, init_frame._stack, new FuncArgsInfo(), ref status);
          init_frame._stack.Push(self);
        }
        break;
        default:
          throw new Exception("Not supported opcode: " + opcode);
      }
      ++ip;
    }
  }

  void ExecModuleInitFunc(CompiledModule cm)
  {
    if(cm.init_func_idx == -1)
      return;
    
    var fs = (FuncSymbolScript)cm.ns.members[cm.init_func_idx];
    var addr = new FuncAddr() {
      module = cm,
      fs = fs,
      ip = fs.ip_addr
    };
    var fb = Start(addr);
    if(Tick(fb))
      throw new Exception("Module '" + cm.name + "' init function is still running");
  }

  public Fiber Start(string func, params Val[] args)
  {
    return Start(func, 0, args);
  }

  public Fiber Start(string func, FuncArgsInfo args_info, params Val[] args)
  {
    return Start(func, args_info.bits, args);
  }

  public Fiber Start(string func, uint cargs_bits, params Val[] args)
  {
    return Start(func, cargs_bits, new StackList<Val>(args));
  }

  public Fiber Start(string func, StackList<Val> args)
  {
    return Start(func, 0, args);
  }

  public Fiber Start(string func, FuncArgsInfo args_info, StackList<Val> args)
  {
    return Start(func, args_info.bits, args);
  }

  public Fiber Start(string func, uint cargs_bits, StackList<Val> args)
  {
    if(!TryFindFuncAddr(func, out var addr))
      return null;

    return Start(addr, cargs_bits, args);
  }

  public Fiber Start(FuncAddr addr, uint cargs_bits = 0, params Val[] args)
  {
    return Start(addr, cargs_bits, new StackList<Val>(args));
  }

  public Fiber Start(FuncAddr addr, StackList<Val> args)
  {
    return Start(addr, 0, args);
  }

  public Fiber Start(FuncAddr addr, uint cargs_bits, StackList<Val> args)
  {
    var fb = Fiber.New(this);
    Register(fb);

    var frame = Frame.New(this);
    frame.Init(fb, fb.frame0, fb.frame0._stack, addr.module, addr.ip);

    for (int i = args.Count; i-- > 0;)
    {
      var arg = args[i];
      frame._stack.Push(arg);
    }
    //cargs bits
    frame._stack.Push(Val.NewInt(this, cargs_bits));

    Attach(fb, frame);

    return fb;
  }

  public bool TryFindFuncAddr(string path, out FuncAddr addr)
  {
    addr = default(FuncAddr);

    var fs = ResolveNamedByPath(path) as FuncSymbolScript;
    if(fs == null)
      return false;

    var cm = compiled_mods[((Namespace)fs.scope).module.name];

    addr = new FuncAddr() {
      module = cm,
      fs = fs,
      ip = fs.ip_addr
    };

    return true;
  }

  [Obsolete("Use TryFindFuncAddr(string path, out FuncAddr addr) instead.")]
  public bool TryFindFuncAddr(string path, out FuncAddr addr, out FuncSymbolScript fs)
  {
    bool yes = TryFindFuncAddr(path, out addr);
    fs = addr.fs;
    return yes;
  }

  public bool TryFindVarAddr(string path, out VarAddr addr)
  {
    addr = default(VarAddr);

    var vs = ResolveNamedByPath(path) as VariableSymbol;
    if(vs == null)
      return false;

    var cm = compiled_mods[((Namespace)vs.scope).module.name];

    addr = new VarAddr() {
      module = cm,
      vs = vs,
      val = cm.gvars[vs.scope_idx]
    };

    return true;
  }

  Dictionary<SymbolSpec, ModuleSymbol> symbol_spec2module = new Dictionary<SymbolSpec, ModuleSymbol>();

  public enum LoadModuleSymbolError
  {
    Ok,
    ModuleNotFound,
    SymbolNotFound
  }

  public LoadModuleSymbolError TryLoadModuleSymbol(SymbolSpec spec, out ModuleSymbol ms)
  {
    if(symbol_spec2module.TryGetValue(spec, out ms))
      return LoadModuleSymbolError.Ok;
    
    if(!LoadModule(spec.module))
      return LoadModuleSymbolError.ModuleNotFound;

    var symb = ResolveNamedByPath(spec.path) as Symbol;
    if(symb == null)
      return LoadModuleSymbolError.SymbolNotFound;

    //TODO: should we actually check if loaded module matches 
    //      the module where the found symbol actually resides?
    var cm = compiled_mods[((Namespace)symb.scope).module.name];

    ms.cm = cm;
    ms.symbol = symb;

    symbol_spec2module.Add(spec, ms);

    return LoadModuleSymbolError.Ok;
  }

  public INamed ResolveNamedByPath(string path)
  {
    foreach(var kv in compiled_mods)
    {
      var s = kv.Value.ns.ResolveSymbolByPath(path);
      if(s != null)
        return s;
    }
    return null;
  }

  static FuncSymbolScript TryMapIp2Func(CompiledModule cm, int ip)
  {
    FuncSymbolScript fsymb = null;
    cm.ns.ForAllLocalSymbols(delegate(Symbol s) {
      if(s is FuncSymbolScript ftmp && ftmp.ip_addr == ip)
        fsymb = ftmp;
    });
    return fsymb;
  }

  //NOTE: adding special bytecode which makes the fake Frame to exit
  //      after executing the coroutine
  static byte[] RETURN_BYTES = new byte[] {(byte)Opcodes.ExitFrame};

  public Fiber Start(FuncPtr ptr, Frame curr_frame, ValStack curr_stack)
  {
    var fb = Fiber.New(this);
    Register(fb, curr_frame.fb);

    //checking native call
    if(ptr.native != null)
    {
      //let's create a fake frame for a native call
      var frame = Frame.New(this);
      frame.Init(fb, curr_frame, curr_stack, null, null, RETURN_BYTES, 0);
      Attach(fb, frame);
      fb.exec.coroutine = ptr.native.cb(curr_frame, curr_stack, new FuncArgsInfo(0)/*cargs bits*/, ref fb.status);
      //NOTE: before executing a coroutine VM will increment ip optimistically
      //      but we need it to remain at the same position so that it points at
      //      the fake return opcode
      if(fb.exec.coroutine != null)
        --fb.exec.ip;
    }
    else
    {
      var frame = ptr.MakeFrame(this, curr_frame, curr_stack);
      Attach(fb, frame);
      //cargs bits
      frame._stack.Push(Val.NewNum(this, 0));
    }

    return fb;
  }

  public Fiber Start(FuncPtr ptr, Frame curr_frame, ValStack curr_stack, params Val[] args)
  {
    return Start(ptr, curr_frame, curr_stack, args.AsSpan());
  }

  public Fiber Start(FuncPtr ptr, Frame curr_frame, ValStack curr_stack, ReadOnlySpan<Val> args)
  {
    var fb = Fiber.New(this);
    Register(fb, curr_frame.fb);

    //checking native call
    if(ptr.native != null)
    {
      //let's create a fake frame for a native call
      var frame = Frame.New(this);
      frame.Init(fb, curr_frame, curr_stack, null, null, RETURN_BYTES, 0);

      for(int i=args.Length;i-- > 0;)
      {
        var arg = args[i];
        frame._stack.Push(arg);
      }
      //cargs bits
      frame._stack.Push(Val.NewInt(this, args.Length));

      Attach(fb, frame);
      fb.exec.coroutine = ptr.native.cb(curr_frame, curr_stack, new FuncArgsInfo(0)/*cargs bits*/, ref fb.status);
      //NOTE: before executing a coroutine VM will increment ip optimistically
      //      but we need it to remain at the same position so that it points at
      //      the fake return opcode
      if(fb.exec.coroutine != null)
        --fb.exec.ip;
    }
    else
    {
      var frame = ptr.MakeFrame(this, curr_frame, curr_stack);

      for(int i=args.Length;i-- > 0;)
      {
        var arg = args[i];
        frame._stack.Push(arg);
      }

      Attach(fb, frame);
      //cargs bits
      frame._stack.Push(Val.NewNum(this, args.Length));
    }

    return fb;
  }

  public void Detach(Fiber fb)
  {
    fibers.Remove(fb);
  }

  void Attach(Fiber fb, Frame frm)
  {
    frm.fb = fb;
    fb.exec.ip = frm.start_ip;
    fb.exec.frames.Push(frm);
    fb.exec.regions.Push(new Region(frm, frm));
    fb.exec.stack = frm._stack;
  }

  void Register(Fiber fb, Fiber parent = null)
  {
    fb.id = ++fibers_ids;
    fibers.Add(fb);
    parent?.AddChild(fb);

    OnNewFiber?.Invoke(fb);
  }

  public void Stop(Fiber fb)
  {
    try
    {
      _Stop(fb);
    }
    catch(Exception e)
    {
      var trace = new List<VM.TraceItem>();
      try
      {
        fb.GetStackTrace(trace);
      }
      catch(Exception) 
      {}
      throw new Error(trace, e); 
    }
  }

  public void StopChildren(Fiber fb)
  {
    foreach(var child_ref in fb.children)
    {
      var child = child_ref.Get();
      if(child != null)
      {
        StopChildren(child);
        _Stop(child);
      }
    }
  }

  internal void _Stop(Fiber fb)
  {
    if(fb.IsStopped())
      return;

    fb.Release();
    //NOTE: we assing Fiber ip to a special value which is just one value after STOP_IP
    //      this way Fiber breaks its current Frame execution loop.
    fb.exec.ip = STOP_IP + 1;
  }

  public void Stop(int fid)
  {
    var fb = FindFiber(fid);
    if(fb == null)
      return;
    Stop(fb);
  }

  public void Stop()
  {
    for(int i=fibers.Count;i-- > 0;)
    {
      Stop(fibers[i]);
      fibers.RemoveAt(i);
    }
  }

  public Fiber FindFiber(int fid)
  {
    for(int i=0;i<fibers.Count;++i)
      if(fibers[i].id == fid)
        return fibers[i];
    return null;
  }

  public void GetStackTrace(Dictionary<Fiber, List<TraceItem>> info)
  {
    for(int i=0;i<fibers.Count;++i)
    {
      var fb = fibers[i];
      var trace = new List<TraceItem>();
      fb.GetStackTrace(trace);
      info[fb] = trace;
    }
  }

  internal BHS Execute(ExecState exec, int exec_waterline_idx = 0)
  {
    var status = BHS.SUCCESS;
    while(exec.regions.Count > exec_waterline_idx && status == BHS.SUCCESS)
    {
      status = ExecuteOnce(exec);
    }
    return status;
  }

  BHS ExecuteOnce(ExecState exec)
  { 
    var item = exec.regions.Peek();

    var curr_frame = item.frame;

#if BHL_TDEBUG
    Util.Debug("EXEC TICK " + curr_frame.fb.tick + " " + exec.GetHashCode() + ":" + exec.regions.Count + ":" + exec.frames.Count + " (" + curr_frame.GetHashCode() + "," + curr_frame.fb.id + ") IP " + exec.ip + "(min:" + item.min_ip + ", max:" + item.max_ip + ")" + (exec.ip > -1 && exec.ip < curr_frame.bytecode.Length ? " OP " + (Opcodes)curr_frame.bytecode[exec.ip] : " OP ? ") + " CORO " + exec.coroutine?.GetType().Name + "(" + exec.coroutine?.GetHashCode() + ")" + " DEFERABLE " + item.defer_support?.GetType().Name + "(" + item.defer_support?.GetHashCode() + ") " + curr_frame.bytecode.Length /* + " " + curr_frame.fb.GetStackTrace()*/ /* + " " + Environment.StackTrace*/);
#endif

    //NOTE: if there's an active coroutine it has priority over simple 'code following' via ip
    if(exec.coroutine != null)
      return ExecuteCoroutine(curr_frame, exec);

    if(exec.ip < item.min_ip || exec.ip > item.max_ip)
    {
      exec.regions.Pop();
      return BHS.SUCCESS;
    }

    var opcode = (Opcodes)curr_frame.bytecode[exec.ip];
    //Console.WriteLine("OP " + opcode + " " + ip);
    switch(opcode)
    {
      case Opcodes.Constant:
      {
        int const_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var cn = curr_frame.constants[const_idx];
        var cv = cn.ToVal(this);
        exec.stack.Push(cv);
      }
      break;
      case Opcodes.TypeCast:
      {
        int cast_type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        bool force_type = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip) == 1;

        var cast_type = curr_frame.constants[cast_type_idx].itype.Get();

        HandleTypeCast(exec, cast_type, force_type);
      }
      break;
      case Opcodes.TypeAs:
      {
        int cast_type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var as_type = curr_frame.constants[cast_type_idx].itype.Get();
        bool force_type = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip) == 1;

        HandleTypeAs(exec, as_type, force_type);
      }
      break;
      case Opcodes.TypeIs:
      {
        int cast_type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var as_type = curr_frame.constants[cast_type_idx].itype.Get();

        HandleTypeIs(exec, as_type);
      }
      break;
      case Opcodes.Typeof:
      {
        int type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var type = curr_frame.constants[type_idx].itype.Get();

        exec.stack.Push(Val.NewObj(this, type, Types.ClassType));
      }
      break;
      case Opcodes.Inc:
      {
        int var_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        ++curr_frame.locals[var_idx]._num;
      }
      break;
      case Opcodes.Dec:
      {
        int var_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        --curr_frame.locals[var_idx]._num;
      }
      break;
      case Opcodes.ArrIdx:
      {
        var self = exec.stack[exec.stack.Count - 2];
        var class_type = ((ArrayTypeSymbol)self.type);
        var status = BHS.SUCCESS;
        class_type.FuncArrIdx.cb(curr_frame, exec.stack, new FuncArgsInfo(), ref status);
      }
      break;
      case Opcodes.ArrIdxW:
      {
        var self = exec.stack[exec.stack.Count - 2];
        var class_type = ((ArrayTypeSymbol)self.type);
        var status = BHS.SUCCESS;
        class_type.FuncArrIdxW.cb(curr_frame, exec.stack, new FuncArgsInfo(), ref status);
      }
      break;
      case Opcodes.ArrAddInplace:
      {
        var self = exec.stack[exec.stack.Count - 2];
        self.Retain();
        var class_type = ((ArrayTypeSymbol)self.type);
        var status = BHS.SUCCESS;
        ((FuncSymbolNative)class_type._all_members[0]).cb(curr_frame, exec.stack, new FuncArgsInfo(), ref status);
        exec.stack.Push(self);
      }
      break;
      case Opcodes.MapIdx:
      {
        var self = exec.stack[exec.stack.Count - 2];
        var class_type = ((MapTypeSymbol)self.type);
        var status = BHS.SUCCESS;
        class_type.FuncMapIdx.cb(curr_frame, exec.stack, new FuncArgsInfo(), ref status);
      }
      break;
      case Opcodes.MapIdxW:
      {
        var self = exec.stack[exec.stack.Count - 2];
        var class_type = ((MapTypeSymbol)self.type);
        var status = BHS.SUCCESS;
        class_type.FuncMapIdxW.cb(curr_frame, exec.stack, new FuncArgsInfo(), ref status);
      }
      break;
      case Opcodes.MapAddInplace:
      {
        var self = exec.stack[exec.stack.Count - 3];
        self.Retain();
        var class_type = ((MapTypeSymbol)self.type);
        var status = BHS.SUCCESS;
        ((FuncSymbolNative)class_type._all_members[0]).cb(curr_frame, exec.stack, new FuncArgsInfo(), ref status);
        exec.stack.Push(self);
      }
      break;
      case Opcodes.Add:
      case Opcodes.Sub:
      case Opcodes.Div:
      case Opcodes.Mod:
      case Opcodes.Mul:
      case Opcodes.And:
      case Opcodes.Or:
      case Opcodes.BitAnd:
      case Opcodes.BitOr:
      case Opcodes.Equal:
      case Opcodes.NotEqual:
      case Opcodes.LT:
      case Opcodes.LTE:
      case Opcodes.GT:
      case Opcodes.GTE:
      {
        ExecuteBinaryOp(opcode, exec.stack);
      }
      break;
      case Opcodes.UnaryNot:
      case Opcodes.UnaryNeg:
      {
        ExecuteUnaryOp(opcode, exec.stack);
      }
      break;
      case Opcodes.GetVar:
      {
        int local_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        exec.stack.PushRetain(curr_frame.locals[local_idx]);
      }
      break;
      case Opcodes.SetVar:
      {
        int local_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        var new_val = exec.stack.Pop();
        curr_frame.locals.Assign(this, local_idx, new_val);
        new_val.Release();
      }
      break;
      case Opcodes.ArgVar:
      {
        int local_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        var arg_val = exec.stack.Pop();
        var loc_var = Val.New(this);
        loc_var.ValueCopyFrom(arg_val);
        loc_var.RefMod(RefOp.USR_INC);
        curr_frame.locals[local_idx] = loc_var;
        arg_val.Release();
      }
      break;
      case Opcodes.ArgRef:
      {
        int local_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        curr_frame.locals[local_idx] = exec.stack.Pop();
      }
      break;
      case Opcodes.DeclVar:
      {
        int local_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        int type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var type = curr_frame.constants[type_idx].itype.Get();

        var curr = curr_frame.locals[local_idx];
        //NOTE: handling case when variables are 're-declared' within the nested loop
        if(curr != null)
          curr.Release();
        curr_frame.locals[local_idx] = MakeDefaultVal(type);
      }
      break;
      case Opcodes.GetAttr:
      {
        int fld_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        var obj = exec.stack.Pop();
        var class_symb = (ClassSymbol)obj.type;
        var res = Val.New(this);
        var field_symb = (FieldSymbol)class_symb._all_members[fld_idx];
        field_symb.getter(curr_frame, obj, ref res, field_symb);
        //NOTE: we retain only the payload since we make the copy of the value 
        //      and the new res already has refs = 1 while payload's refcount 
        //      is not incremented
        res.RefMod(RefOp.USR_INC);
        exec.stack.Push(res);
        obj.Release();
      }
      break;
      case Opcodes.RefAttr:
      {
        int fld_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        var obj = exec.stack.Pop();
        var class_symb = (ClassSymbol)obj.type;
        var field_symb = (FieldSymbol)class_symb._all_members[fld_idx];
        Val res;
        field_symb.getref(curr_frame, obj, out res, field_symb);
        exec.stack.PushRetain(res);
        obj.Release();
      }
      break;
      case Opcodes.SetAttr:
      {
        int fld_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);

        var obj = exec.stack.Pop();
        var class_symb = (ClassSymbol)obj.type;
        var val = exec.stack.Pop();
        var field_symb = (FieldSymbol)class_symb._all_members[fld_idx];
        field_symb.setter(curr_frame, ref obj, val, field_symb);
        val.Release();
        obj.Release();
      }
      break;
      case Opcodes.SetAttrInplace:
      {
        int fld_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        var val = exec.stack.Pop();
        var obj = exec.stack.Peek();
        var class_symb = (ClassSymbol)obj.type;
        var field_symb = (FieldSymbol)class_symb._all_members[fld_idx];
        field_symb.setter(curr_frame, ref obj, val, field_symb);
        val.Release();
      }
      break;
      case Opcodes.GetGVar:
      {
        int var_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);

        exec.stack.PushRetain(curr_frame.module.gvars[var_idx]);
      }
      break;
      case Opcodes.SetGVar:
      {
        int var_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);

        var new_val = exec.stack.Pop();
        curr_frame.module.gvars.Assign(this, var_idx, new_val);
        new_val.Release();
      }
      break;
      case Opcodes.ExitFrame:
      {
        exec.ip = curr_frame.return_ip;
        exec.stack = curr_frame.origin_stack;
        curr_frame.ExitScope(null, exec);
        curr_frame.Release();
        exec.frames.Pop();
        exec.regions.Pop();
      }
      break;
      case Opcodes.Return:
      {
        //NOTE: we jump to ExitFrame opcode of the last function in the module
        //TODO: probably we should jump to our 'local' frame ExitCode so that 
        //      we don't have to fetch a way too far slot in the memory (it might affect performance?)
        exec.ip = curr_frame.bytecode.Length - EXIT_OFFSET;
      }
      break;
      case Opcodes.ReturnVal:
      {
        int ret_num = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);

        int stack_offset = exec.stack.Count; 
        for(int i=0;i<ret_num;++i)
          curr_frame.origin_stack.Push(exec.stack[stack_offset-ret_num+i]);
        exec.stack.head -= ret_num;

        //NOTE: we jump to ExitFrame opcode of the last function in the module
        //TODO: probably we should jump to our 'local' frame ExitCode so that 
        //      we don't have to fetch a way too far slot in the memory (it might affect performance?)
        exec.ip = curr_frame.bytecode.Length - EXIT_OFFSET;
      }
      break;
      case Opcodes.GetLocalPtr:
      {
        int ip_addr = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);

        var ptr = FuncPtr.New(this);
        ptr.Init(curr_frame.module, ip_addr);
        var func_symb = curr_frame.module.module._ip2func[ip_addr];
        exec.stack.Push(Val.NewObj(this, ptr, func_symb.signature));
      }
      break;
      case Opcodes.GetFuncPtr:
      {
        int named_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var func_symb = (FuncSymbolScript)curr_frame.constants[named_idx].inamed.Get();

        var ptr = FuncPtr.New(this);
        ptr.Init(func_symb._module, func_symb.ip_addr);
        exec.stack.Push(Val.NewObj(this, ptr, func_symb.signature));
      }
      break;
      case Opcodes.GetFuncNativePtr:
      {
        int func_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var func_symb = (FuncSymbolNative)types.nfunc_index[func_idx];
        var ptr = FuncPtr.New(this);
        ptr.Init(func_symb);
        exec.stack.Push(Val.NewObj(this, ptr, func_symb.signature));
      }
      break;
      case Opcodes.GetFuncPtrFromVar:
      {
        int local_var_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        var val = curr_frame.locals[local_var_idx];
        val.Retain();
        exec.stack.Push(val);
      }
      break;
      case Opcodes.LastArgToTop:
      {
        //NOTE: we need to move arg (e.g. func ptr) to the top of the stack
        //      so that it fullfills Opcode.Call requirements 
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 
        int args_num = (int)(args_bits & FuncArgsInfo.ARGS_NUM_MASK); 
        int arg_idx = exec.stack.Count - args_num - 1; 
        var arg = exec.stack[arg_idx];
        exec.stack.RemoveAt(arg_idx);
        exec.stack.Push(arg);
      }
      break;
      case Opcodes.CallLocal:
      {
        int func_ip = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip); 
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        var frm = Frame.New(this);
        frm.Init(curr_frame, exec.stack, func_ip);
        Call(curr_frame, exec, frm, args_bits);
      }
      break;
      case Opcodes.CallNative:
      {
        int func_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        var native = (FuncSymbolNative)types.nfunc_index[func_idx];

        BHS status;
        if(CallNative(curr_frame, exec.stack, native, args_bits, out status, ref exec.coroutine))
          return status;
      }
      break;
      case Opcodes.CallFunc:
      {
        int named_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        var func_symb = (FuncSymbolScript)curr_frame.constants[named_idx].inamed.Get();

        var frm = Frame.New(this);
        frm.Init(curr_frame.fb, curr_frame, exec.stack, func_symb._module, func_symb.ip_addr);
        Call(curr_frame, exec, frm, args_bits);
      }
      break;
      case Opcodes.CallMethod:
      {
        int func_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        //TODO: use a simpler schema where 'self' is passed on the top
        int args_num = (int)(args_bits & FuncArgsInfo.ARGS_NUM_MASK); 
        int self_idx = exec.stack.Count - args_num - 1;
        var self = exec.stack[self_idx];
        exec.stack.RemoveAt(self_idx);

        var class_type = ((ClassSymbolScript)self.type);

        var func_symb = (FuncSymbolScript)class_type._all_members[func_idx];

        var frm = Frame.New(this);
        frm.Init(curr_frame.fb, curr_frame, exec.stack, func_symb._module, func_symb.ip_addr);

        frm.locals.head = 1;
        frm.locals[0] = self;

        Call(curr_frame, exec, frm, args_bits);
      }
      break;
      case Opcodes.CallMethodNative:
      {
        int func_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        int args_num = (int)(args_bits & FuncArgsInfo.ARGS_NUM_MASK); 
        int self_idx = exec.stack.Count - args_num - 1;
        var self = exec.stack[self_idx];

        var class_type = (ClassSymbol)self.type;

        BHS status;
        if(CallNative(curr_frame, exec.stack, (FuncSymbolNative)class_type._all_members[func_idx], args_bits, out status, ref exec.coroutine))
          return status;
      }
      break;
      case Opcodes.CallMethodVirt:
      {
        int virt_func_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        //TODO: use a simpler schema where 'self' is passed on the top
        int args_num = (int)(args_bits & FuncArgsInfo.ARGS_NUM_MASK); 
        int self_idx = exec.stack.Count - args_num - 1;
        var self = exec.stack[self_idx];
        exec.stack.RemoveAt(self_idx);

        var class_type = (ClassSymbol)self.type;
        var func_symb = (FuncSymbolScript)class_type._vtable[virt_func_idx];

        var frm = Frame.New(this);
        frm.Init(curr_frame.fb, curr_frame, exec.stack, func_symb._module, func_symb.ip_addr);

        frm.locals.head = 1;
        frm.locals[0] = self;

        Call(curr_frame, exec, frm, args_bits);
      }
      break;
      case Opcodes.CallMethodIface:
      {
        int iface_func_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        int iface_type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        //TODO: use a simpler schema where 'self' is passed on the top
        int args_num = (int)(args_bits & FuncArgsInfo.ARGS_NUM_MASK); 
        int self_idx = exec.stack.Count - args_num - 1;
        var self = exec.stack[self_idx];
        exec.stack.RemoveAt(self_idx);

        var iface_symb = (InterfaceSymbol)curr_frame.constants[iface_type_idx].itype.Get(); 
        var class_type = (ClassSymbol)self.type;
        var func_symb = (FuncSymbolScript)class_type._itable[iface_symb][iface_func_idx];

        var frm = Frame.New(this);
        frm.Init(curr_frame.fb, curr_frame, exec.stack, func_symb._module, func_symb.ip_addr);

        frm.locals.head = 1;
        frm.locals[0] = self;

        Call(curr_frame, exec, frm, args_bits);
      }
      break;
      case Opcodes.CallMethodIfaceNative:
      {
        int iface_func_idx = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        int iface_type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        var iface_symb = (InterfaceSymbol)curr_frame.constants[iface_type_idx].itype.Get(); 
        var func_symb = (FuncSymbolNative)iface_symb.members[iface_func_idx];

        BHS status;
        if(CallNative(curr_frame, exec.stack, func_symb, args_bits, out status, ref exec.coroutine))
          return status;
      }
      break;
      case Opcodes.CallFuncPtr:
      {
        uint args_bits = Bytecode.Decode32(curr_frame.bytecode, ref exec.ip); 

        var val_ptr = exec.stack.Pop();
        var ptr = (FuncPtr)val_ptr._obj;

        //checking native call
        if(ptr.native != null)
        {
          BHS status;
          bool return_status = CallNative(curr_frame, exec.stack, ptr.native, args_bits, out status, ref exec.coroutine);
          val_ptr.Release();
          if(return_status)
            return status;
        }
        else
        {
          var frm = ptr.MakeFrame(this, curr_frame, exec.stack);
          val_ptr.Release();
          Call(curr_frame, exec, frm, args_bits);
        }
      }
      break;
      case Opcodes.InitFrame:
      {
        int local_vars_num = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        var args_bits = exec.stack.Pop(); 
        curr_frame.locals.Resize(local_vars_num);
        //NOTE: we need to store arg info bits locally so that
        //      this information will be available to func 
        //      args related opcodes
        curr_frame.locals[local_vars_num-1] = args_bits;
      }
      break;
      case Opcodes.Lambda:
      {             
        short offset = (short)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        var ptr = FuncPtr.New(this);
        ptr.Init(curr_frame, exec.ip+1);
        exec.stack.Push(Val.NewObj(this, ptr, Types.Any/*TODO: should be a FuncPtr type*/));

        exec.ip += offset;
      }
      break;
      case Opcodes.UseUpval:
      {
        int up_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        int local_idx = (int)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        var mode = (UpvalMode)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);

        var addr = (FuncPtr)exec.stack.Peek()._obj;

        //TODO: amount of local variables must be known ahead and
        //      initialized during Frame initialization
        //NOTE: we need to reflect the updated max amount of locals,
        //      otherwise they might not be cleared upon Frame exit
        addr.upvals.Resize(local_idx+1);

        var upval = curr_frame.locals[up_idx];
        if(mode == UpvalMode.COPY)
        {
          var copy = Val.New(this);
          copy.ValueCopyFrom(upval);
          addr.upvals[local_idx] = copy;
        }
        else
        {
          upval.RefMod(RefOp.USR_INC | RefOp.INC);
          addr.upvals[local_idx] = upval;
        }
      }
      break;
      case Opcodes.Pop:
      {
        exec.stack.PopRelease();
      }
      break;
      case Opcodes.Jump:
      {
        short offset = (short)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        exec.ip += offset;
      }
      break;
      case Opcodes.JumpZ:
      {
        ushort offset = Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        if(exec.stack.PopRelease().bval == false)
          exec.ip += offset;
      }
      break;
      case Opcodes.JumpPeekZ:
      {
        ushort offset = Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        var v = exec.stack.Peek();
        if(v.bval == false)
          exec.ip += offset;
      }
      break;
      case Opcodes.JumpPeekNZ:
      {
        ushort offset = Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        var v = exec.stack.Peek();
        if(v.bval == true)
          exec.ip += offset;
      }
      break;
      case Opcodes.DefArg:
      {
        byte def_arg_idx = (byte)Bytecode.Decode8(curr_frame.bytecode, ref exec.ip);
        int jump_pos = (int)Bytecode.Decode16(curr_frame.bytecode, ref exec.ip);
        uint args_bits = (uint)curr_frame.locals[curr_frame.locals.Count-1]._num; 
        var args_info = new FuncArgsInfo(args_bits);
        //Console.WriteLine("DEF ARG: " + def_arg_idx + ", jump pos " + jump_pos + ", used " + args_info.IsDefaultArgUsed(def_arg_idx) + " " + args_bits);
        //NOTE: if default argument is not used we need to jump out of default argument calculation code
        if(!args_info.IsDefaultArgUsed(def_arg_idx))
          exec.ip += jump_pos;
      }
      break;
      case Opcodes.Block:
      {
        var new_coroutine = VisitBlock(exec, curr_frame, item.defer_support);
        if(new_coroutine != null)
        {
          //NOTE: since there's a new coroutine we want to skip ip incrementing
          //      which happens below and proceed right to the execution of 
          //      the new coroutine
          exec.coroutine = new_coroutine;
          return BHS.SUCCESS;
        }
      }
      break;
      case Opcodes.New:
      {
        int type_idx = (int)Bytecode.Decode24(curr_frame.bytecode, ref exec.ip);
        var type = curr_frame.constants[type_idx].itype.Get();
        HandleNew(curr_frame, exec.stack, type);
      }
      break;
      default:
        throw new Exception("Not supported opcode: " + opcode);
    }

    ++exec.ip;
    return BHS.SUCCESS;
  }

  internal Val MakeDefaultVal(IType type)
  {
    var v = Val.New(this);
    InitDefaultVal(type, v);
    return v;
  }

  internal void InitDefaultVal(IType type, Val v)
  {
    //TODO: make type responsible for default initialization 
    //      of the value
    if(type == Types.Int)
      v.SetNum(0);
    else if(type == Types.Float)
      v.SetFlt((double)0);
    else if(type == Types.String)
      v.SetStr("");
    else if(type == Types.Bool)
      v.SetBool(false);
    else
      v.type = type;
  }

  void Call(Frame curr_frame, ExecState exec, Frame new_frame, uint args_bits)
  {
    int args_num = (int)(args_bits & FuncArgsInfo.ARGS_NUM_MASK); 
    for(int i = 0; i < args_num; ++i)
      new_frame._stack.Push(exec.stack.Pop());
    new_frame._stack.Push(Val.NewInt(this, args_bits));

    //let's remember ip to return to
    new_frame.return_ip = exec.ip;
    exec.stack = new_frame._stack;
    exec.frames.Push(new_frame);
    exec.regions.Push(new Region(new_frame, new_frame));
    //since ip will be incremented below we decrement it intentionally here
    exec.ip = new_frame.start_ip - 1; 
  }

  //NOTE: returns whether further execution should be stopped and status returned immediately (e.g in case of RUNNING or FAILURE)
  static bool CallNative(Frame curr_frame, ValStack curr_stack, FuncSymbolNative native, uint args_bits, out BHS status, ref Coroutine coroutine)
  {
    status = BHS.SUCCESS;
    var new_coroutine = native.cb(curr_frame, curr_stack, new FuncArgsInfo(args_bits), ref status);

    if(new_coroutine != null)
    {
      //NOTE: since there's a new coroutine we want to skip ip incrementing
      //      which happens below and proceed right to the execution of 
      //      the new coroutine
      coroutine = new_coroutine;
      return true;
    }
    else if(status != BHS.SUCCESS)
      return true;
    else 
      return false;
  }

  static BHS ExecuteCoroutine(Frame curr_frame, ExecState exec)
  {
    var status = BHS.SUCCESS;
    //NOTE: optimistically stepping forward so that for simple  
    //      bindings you won't forget to do it
    ++exec.ip;
    exec.coroutine.Tick(curr_frame, exec, ref status);

    if(status == BHS.RUNNING)
    {
      --exec.ip;
      return status;
    }
    else if(status == BHS.FAILURE)
    {
      CoroutinePool.Del(curr_frame, exec, exec.coroutine);
      exec.coroutine = null;

      //NOTE: we jump to ExitFrame opcode of the last function in the module
      //TODO: probably we should jump to our 'local' frame ExitCode so that 
      //      we don't have to fetch a way too far slot in the memory (it might affect performance?)
      exec.ip = curr_frame.bytecode.Length - EXIT_OFFSET;
      exec.regions.Pop();

      return status;
    }
    else if(status == BHS.SUCCESS)
    {
      CoroutinePool.Del(curr_frame, exec, exec.coroutine);
      exec.coroutine = null;

      return status;
    }
    else
      throw new Exception("Bad status: " + status);
  }

  //TODO: make it more universal and robust
  void HandleTypeCast(ExecState exec, IType cast_type, bool force_type)
  {
    var new_val = Val.New(this);
    var val = exec.stack.Pop();

    if(cast_type == Types.Int)
      new_val.SetNum((long)val.num);
    else if(cast_type == Types.String && val.type != Types.String)
      new_val.SetStr(val.num.ToString(System.Globalization.CultureInfo.InvariantCulture));
    else
    {
      //TODO: think better about run-time invalid casting error
      //if(!force_type && cast_type is IInstanceType && val.type is IInstanceType && !Types.Is(val.type, cast_type))
      //  throw new Exception("Invalid type cast");
      new_val.ValueCopyFrom(val);
      if(force_type)
        new_val.type = cast_type;
      new_val.RefMod(RefOp.USR_INC);
    }

    val.Release();

    exec.stack.Push(new_val);
  }

  void HandleTypeAs(ExecState exec, IType cast_type, bool force_type)
  {
    var val = exec.stack.Pop();

    if(Types.Is(val, cast_type))
    {
      var new_val = Val.New(this);
      new_val.ValueCopyFrom(val);
      if(force_type)
        new_val.type = cast_type;
      new_val.RefMod(RefOp.USR_INC);
      exec.stack.Push(new_val);
    }
    else
      exec.stack.Push(Null);

    val.Release();
  }

  void HandleTypeIs(ExecState exec, IType type)
  {
    var val = exec.stack.Pop();
    exec.stack.Push(Val.NewBool(this, Types.Is(val, type)));
    val.Release();
  }

  void HandleNew(Frame curr_frame, ValStack stack, IType type)
  {
    var cls = type as ClassSymbol;
    if(cls == null)
      throw new Exception("Not a class symbol: " + type);

    var val = Val.New(this); 
    cls.creator(curr_frame, ref val, cls);
    stack.Push(val);
  }

  static void ReadBlockHeader(ref int ip, Frame curr_frame, out BlockType type, out int size)
  {
    type = (BlockType)Bytecode.Decode8(curr_frame.bytecode, ref ip);
    size = (int)Bytecode.Decode16(curr_frame.bytecode, ref ip);
  }

  Coroutine TryMakeBlockCoroutine(ref int ip, Frame curr_frame, ExecState exec, out int size, IDeferSupport defer_support)
  {
    BlockType type;
    ReadBlockHeader(ref ip, curr_frame, out type, out size);

    if(type == BlockType.SEQ)
    {
      if(defer_support is IBranchyCoroutine)
      {
        var br = CoroutinePool.New<ParalBranchBlock>(this);
        br.Init(curr_frame, ip + 1, ip + size);
        return br;
      }
      else
      {
        var seq = CoroutinePool.New<SeqBlock>(this);
        seq.Init(curr_frame, exec.stack, ip + 1, ip + size);
        return seq;
      }
    }
    else if(type == BlockType.PARAL)
    {
      var paral = CoroutinePool.New<ParalBlock>(this);
      paral.Init(ip + 1, ip + size);
      return paral;
    }
    else if(type == BlockType.PARAL_ALL) 
    {
      var paral = CoroutinePool.New<ParalAllBlock>(this);
      paral.Init(ip + 1, ip + size);
      return paral;
    }
    else if(type == BlockType.DEFER)
    {
      var d = new DeferBlock(curr_frame, ip + 1, ip + size);
      defer_support.RegisterDefer(d);
      //NOTE: we need to skip defer block
      //Console.WriteLine("DEFER SKIP " + ip + " " + (ip+size) + " " + Environment.StackTrace);
      ip += size;
      return null;
    }
    else
      throw new Exception("Not supported block type: " + type);
  }

  Coroutine VisitBlock(ExecState exec, Frame curr_frame, IDeferSupport defer_support)
  {
    int block_size;
    var block_coro = TryMakeBlockCoroutine(ref exec.ip, curr_frame, exec, out block_size, defer_support);

    //Console.WriteLine("BLOCK CORO " + block_coro?.GetType().Name + " " + block_coro?.GetHashCode());
    if(block_coro is IBranchyCoroutine bi) 
    {
      int tmp_ip = exec.ip;
      while(tmp_ip < (exec.ip + block_size))
      {
        ++tmp_ip;

        int tmp_size;
        var branch = TryMakeBlockCoroutine(ref tmp_ip, curr_frame, exec, out tmp_size, (IDeferSupport)block_coro);

       //Console.WriteLine("BRANCH INST " + tmp_ip + " " + branch?.GetType().Name);

        //NOTE: branch == null is a special case for defer {..} block
        if(branch != null)
        {
          bi.Attach(branch);
          tmp_ip += tmp_size;
        }
      }
    }
    return block_coro; 
  }

  public bool Tick()
  {
    return Tick(fibers);
  }

  public bool Tick(List<Fiber> fibers)
  {
    for(int i=0;i<fibers.Count;++i)
    {
      var fb = fibers[i];
      Tick(fb);
    }

    for(int i=fibers.Count;i-- > 0;)
    {
      var fb = fibers[i];
      if(fb.IsStopped())
        fibers.RemoveAt(i);
    }

    return fibers.Count != 0;
  }

  public bool Tick(Fiber fb)
  {
    if(fb.IsStopped())
      return false;

    try
    {
      last_fiber = fb;

      fb.Retain();

      ++fb.tick;
      fb.status = Execute(fb.exec);

      fb.Release();

      if(fb.status != BHS.RUNNING)
        _Stop(fb);
    }
    catch(Exception e)
    {
      var trace = new List<VM.TraceItem>();
      try
      {
        fb.GetStackTrace(trace);
      }
      catch(Exception) 
      {}
      throw new Error(trace, e); 
    }
    return !fb.IsStopped();
  }

  void ExecuteUnaryOp(Opcodes op, ValStack stack)
  {
    var operand = stack.PopRelease().num;
    switch(op)
    {
      case Opcodes.UnaryNot:
        stack.Push(Val.NewBool(this, operand != 1));
      break;
      case Opcodes.UnaryNeg:
        stack.Push(Val.NewFlt(this, operand * -1));
      break;
    }
  }

  void ExecuteBinaryOp(Opcodes op, ValStack stack)
  {
    var r_operand = stack.Pop();
    var l_operand = stack.Pop();

    switch(op)
    {
      case Opcodes.Add:
      {
        //TODO: add Opcodes.Concat?
        if((r_operand.type == Types.String) && (l_operand.type == Types.String))
          stack.Push(Val.NewStr(this, (string)l_operand._obj + (string)r_operand._obj));
        else
          stack.Push(Val.NewFlt(this, l_operand._num + r_operand._num));
      }
      break;
      case Opcodes.Sub:
        stack.Push(Val.NewFlt(this, l_operand._num - r_operand._num));
      break;
      case Opcodes.Div:
        stack.Push(Val.NewFlt(this, l_operand._num / r_operand._num));
      break;
      case Opcodes.Mul:
        stack.Push(Val.NewFlt(this, l_operand._num * r_operand._num));
      break;
      case Opcodes.Equal:
        stack.Push(Val.NewBool(this, l_operand.IsValueEqual(r_operand)));
      break;
      case Opcodes.NotEqual:
        stack.Push(Val.NewBool(this, !l_operand.IsValueEqual(r_operand)));
      break;
      case Opcodes.LT:
        stack.Push(Val.NewBool(this, l_operand._num < r_operand._num));
      break;
      case Opcodes.LTE:
        stack.Push(Val.NewBool(this, l_operand._num <= r_operand._num));
      break;
      case Opcodes.GT:
        stack.Push(Val.NewBool(this, l_operand._num > r_operand._num));
      break;
      case Opcodes.GTE:
        stack.Push(Val.NewBool(this, l_operand._num >= r_operand._num));
      break;
      case Opcodes.And:
        stack.Push(Val.NewBool(this, l_operand._num == 1 && r_operand._num == 1));
      break;
      case Opcodes.Or:
        stack.Push(Val.NewBool(this, l_operand._num == 1 || r_operand._num == 1));
      break;
      case Opcodes.BitAnd:
        stack.Push(Val.NewNum(this, (int)l_operand._num & (int)r_operand._num));
      break;
      case Opcodes.BitOr:
        stack.Push(Val.NewNum(this, (int)l_operand._num | (int)r_operand._num));
      break;
      case Opcodes.Mod:
        stack.Push(Val.NewFlt(this, l_operand._num % r_operand._num));
      break;
    }

    r_operand.Release();
    l_operand.Release();
  }
}

} //namespace bhl
