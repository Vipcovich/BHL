using System;
using System.IO;
using System.Collections.Generic;
using bhl;

public class BHL_TestInterpreter : BHL_TestBase
{
  public class Color
  {
    public float r;
    public float g;

    public override string ToString()
    {
      return "[r="+r+",g="+g+"]";
    }
  }

  ClassSymbolNative BindColor(GlobalScope globs)
  {
    var cl = new ClassSymbolNative("Color",
      delegate(ref DynVal v) 
      { 
        v.obj = new Color();
      }
    );

    globs.Define(cl);
    globs.Define(new ArrayTypeSymbolT<Color>(globs, new TypeRef(cl), delegate() { return new List<Color>(); } ));
    cl.Define(new FieldSymbol("r", globs.Type("float"),
      delegate(DynVal ctx, ref DynVal v)
      {
        var c = (Color)ctx.obj;
        v.SetNum(c.r);
      },
      delegate(ref DynVal ctx, DynVal v)
      {
        var c = (Color)ctx.obj;
        c.r = (float)v.num; 
        ctx.obj = c;
      }
    ));
    cl.Define(new FieldSymbol("g", globs.Type("float"),
      delegate(DynVal ctx, ref DynVal v)
      {
        var c = (Color)ctx.obj;
        v.SetNum(c.g);
      },
      delegate(ref DynVal ctx, DynVal v)
      {
        var c = (Color)ctx.obj;
        c.g = (float)v.num; 
        ctx.obj = c;
      }
    ));

    
    return cl;
  }

  public class TraceNode : BehaviorTreeTerminalNode
  {
    Stream sm;

    public TraceNode(Stream sm)
    {
      this.sm = sm;
    }

    public override void init()
    {
      var interp = Interpreter.instance;
      var s = interp.PopValue();

      //Console.WriteLine("==============\n" + s.str + "\n" + Environment.StackTrace);

      var sw = new StreamWriter(sm);
      sw.Write(s.str);
      sw.Flush();
    }
  }

  void BindTrace(GlobalScope globs, MemoryStream trace_stream)
  {
    {
      var fn = new FuncSymbolNative("trace", globs.Type("void"),
          delegate() { return new TraceNode(trace_stream); } );
      fn.Define(new FuncArgSymbol("str", globs.Type("string")));

      globs.Define(fn);
    }
  }

  string GetString(MemoryStream s)
  {
    s.Position = 0;
    var sr = new StreamReader(s);
    return sr.ReadToEnd();
  }

  [IsTested()]
  public void TestUserClassMethodDecl()
  {
    string bhl = @"

    class Foo {
      
      int a

      func int getA() 
      {
        return this.a
      }
    }

    func int test()
    {
      Foo f = {}
      f.a = 10
      return f.getA()
    }
    ";

    var intp = Interpret(bhl, null);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    AssertEqual(res, 10);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestSeveralUserClassMethodDecl()
  {
    string bhl = @"

    class Foo {
      
      int a
      int b

      func int getA()
      {
        return this.b
      }

      func int getB() 
      {
        return this.a
      }
    }

    func int test()
    {
      Foo f = {}
      f.a = 10
      f.b = 10
      return f.getA() + f.getB()
    }
    ";

    var intp = Interpret(bhl, null);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    AssertEqual(res, 20);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestUserClassMethodSameNameLikeClass()
  {
    string bhl = @"

    class Foo {
      
      func int Foo()
      {
        return 0
      }
    }

    func int test()
    {
      Foo f = {}
      return f.Foo()
    }
    ";

    var intp = Interpret(bhl, null);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    AssertEqual(res, 0);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestUserClassMethodDeclVarLikeClassVar()
  {
    string bhl = @"
      class Foo {
        
        int a

        func int Foo()
        {
          a = 10
          return this.a + a
        }
      }

      func int test()
      {
        Foo f = {}
        f.a = 10
        return f.Foo()
      }
    ";

    var intp = Interpret(bhl, null);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    AssertEqual(res, 20);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidIsEncodedInUserClass()
  {
    string bhl = @"

    class Foo { }
      
    func Foo test() 
    {
      return {}
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExecNode(node);

    AssertEqual(res.val.num, (new HashedName("Foo")).n);
    //NOTE: returned value must be manually removed
    DynValDict.Del((res.val.obj as DynValDict));
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidIsEncodedInUserClassInHierarchy()
  {
    string bhl = @"

    class Foo { }
    class Bar : Foo { }
      
    func Bar test() 
    {
      return {}
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExecNode(node);

    AssertEqual(res.val.num, (new HashedName("Bar")).n);
    //NOTE: returned value must be manually removed
    DynValDict.Del((res.val.obj as DynValDict));
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidForUserClass()
  {
    string bhl = @"

    class Foo { }
      
    func int test() 
    {
      return typeid(Foo)
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    //NodeDump(node);
    AssertEqual(res, (new HashedName("Foo")).n);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidForBuiltinType()
  {
    string bhl = @"

    func int test() 
    {
      return typeid(int)
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    //NodeDump(node);
    AssertEqual(res, (new HashedName("int")).n);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidForBuiltinArrType()
  {
    string bhl = @"

    func int test() 
    {
      return typeid(int[])
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractNum(ExecNode(node));

    //NodeDump(node);
    AssertEqual(res, (new HashedName("int[]")).n);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidEqual()
  {
    string bhl = @"

    class Foo { }
      
    func bool test() 
    {
      return typeid(Foo) == typeid(Foo)
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractBool(ExecNode(node));

    //NodeDump(node);
    AssertTrue(res);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidNotEqual()
  {
    string bhl = @"

    class Foo { }
    class Bar { }
      
    func bool test() 
    {
      return typeid(Foo) == typeid(Bar)
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractBool(ExecNode(node));

    //NodeDump(node);
    AssertTrue(res == false);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidNotEqualArrType()
  {
    string bhl = @"

    func bool test() 
    {
      return typeid(int[]) == typeid(float[])
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");
    var res = ExtractBool(ExecNode(node));

    //NodeDump(node);
    AssertTrue(res == false);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestTypeidBadType()
  {
    string bhl = @"

    func int test() 
    {
      return typeid(Foo)
    }
    ";

    AssertError<UserError>(
      delegate() { 
        Interpret(bhl);
      },
      @"type 'Foo' not found"
    );
  }

  [IsTested()]
  public void TestImportGlobalVar()
  {
    string bhl1 = @"
    import ""bhl3""  
    func float test() 
    {
      return foo.x
    }
    ";

    string bhl2 = @"

    class Foo
    {
      float x
    }

    ";

    string bhl3 = @"
    import ""bhl2""  

    Foo foo = {x : 10}

    ";

    var globs = SymbolTable.CreateBuiltins();

    TestCleanDir();
    var files = new List<string>();
    TestNewFile("bhl1.bhl", bhl1, files);
    TestNewFile("bhl2.bhl", bhl2, files);
    TestNewFile("bhl3.bhl", bhl3, files);
    
    var intp = CompileFiles(files, globs);

    var node = intp.GetFuncCallNode("bhl1", "test");
    //NodeDump(node);
    var res = ExecNode(node).val;

    AssertEqual(res.num, 10);
    CommonChecks(intp);
  }

  [IsTested()]
  public void TestImportGlobalVarConflict()
  {
    string bhl1 = @"
    import ""bhl2""  

    int foo = 10

    func test() { }
    ";

    string bhl2 = @"
    float foo = 100
    ";

    var globs = SymbolTable.CreateBuiltins();

    TestCleanDir();
    var files = new List<string>();
    TestNewFile("bhl1.bhl", bhl1, files);
    TestNewFile("bhl2.bhl", bhl2, files);
    
    AssertError<UserError>(
      delegate() { 
        CompileFiles(files, globs);
      },
      @"already defined symbol 'foo'"
    );
  }

  [IsTested()]
  public void TestImportGlobalExecutionOnlyOnce()
  {
    string bhl1 = @"
    import ""bhl2""  
    import ""bhl2""

    func test1() { }
    ";

    string bhl2 = @"
    func int dummy()
    {
      trace(""once"")
      return 1
    }

    int a = dummy()

    ";

    var globs = SymbolTable.CreateBuiltins();
    var trace_stream = new MemoryStream();
    BindTrace(globs, trace_stream);

    TestCleanDir();
    var files = new List<string>();
    TestNewFile("bhl1.bhl", bhl1, files);
    TestNewFile("bhl2.bhl", bhl2, files);

    var intp = CompileFiles(files, globs);
    intp.LoadModule("bhl1");

    var str = GetString(trace_stream);
    AssertEqual("once", str);

    CommonChecks(intp);
  }

  [IsTested()]
  public void TestImportGlobalExecutionOnlyOnceNested()
  {
    string bhl1 = @"
    import ""bhl2""  

    func test1() { }
    ";

    string bhl2 = @"
    import ""bhl3""

    func int dummy()
    {
      trace(""once"")
      return 1
    }

    class Foo {
      int a
    }

    Foo foo = { a : dummy() }

    func test2() { }
    ";

    string bhl3 = @"
    func test3() { }
    ";

    var globs = SymbolTable.CreateBuiltins();
    var trace_stream = new MemoryStream();
    BindTrace(globs, trace_stream);

    TestCleanDir();
    var files = new List<string>();
    TestNewFile("bhl1.bhl", bhl1, files);
    TestNewFile("bhl2.bhl", bhl2, files);
    TestNewFile("bhl3.bhl", bhl3, files);

    var intp = CompileFiles(files, globs);
    intp.LoadModule("bhl1");

    var str = GetString(trace_stream);
    AssertEqual("once", str);

    CommonChecks(intp);
  }

  static int Fib(int x)
  {
    if(x == 0)
      return 0;
    else 
    {
      if(x == 1) 
        return 1;
      else
        return Fib(x-1) + Fib(x-2);
    }
  }

  [IsTested()]
  public void TestFib()
  {
    string bhl = @"

    func int fib(int x)
    {
      if(x == 0) {
        return 0
      } else {
        if(x == 1) {
          return 1
        } else {
          return fib(x - 1) + fib(x - 2)
        }
      }
    }
      
    func int test(int x) 
    {
      return fib(x)
    }
    ";

    var intp = Interpret(bhl);
    var node = intp.GetFuncCallNode("test");

    const int x = 15;

    {
      var stopwatch = System.Diagnostics.Stopwatch.StartNew();
      node.SetArgs(DynVal.NewNum(x));
      ExtractNum(ExecNode(node));
      stopwatch.Stop();
      Console.WriteLine("bhl fib ticks: {0}", stopwatch.ElapsedTicks);
    }

    {
      var stopwatch = System.Diagnostics.Stopwatch.StartNew();
      node.SetArgs(DynVal.NewNum(x));
      ExtractNum(ExecNode(node));
      stopwatch.Stop();
      Console.WriteLine("bhl fib ticks2: {0}", stopwatch.ElapsedTicks);
    }
    CommonChecks(intp);

    {
      var stopwatch = System.Diagnostics.Stopwatch.StartNew();
      Fib(x);
      stopwatch.Stop();
      Console.WriteLine("C# fib ticks: {0}", stopwatch.ElapsedTicks);
    }
  }

  ////////////////////////////////////////////////

  static string TestDirPath()
  {
    string self_bin = System.Reflection.Assembly.GetExecutingAssembly().Location;
    return Path.GetDirectoryName(self_bin) + "/tmp/tests";
  }

  static void TestCleanDir()
  {
    string dir = TestDirPath();
    if(Directory.Exists(dir))
      Directory.Delete(dir, true/*recursive*/);
  }

  static void TestNewFile(string path, string text, List<string> files)
  {
    string full_path = TestDirPath() + "/" + path;
    Directory.CreateDirectory(Path.GetDirectoryName(full_path));
    File.WriteAllText(full_path, text);
    files.Add(full_path);
  }

  static void SharedInit()
  {
    DynVal.PoolClear();
    DynValList.PoolClear();
    DynValDict.PoolClear();
    FuncCallNode.PoolClear();
    FuncCtx.PoolClear();
  }

  static Interpreter CompileFiles(List<string> test_files, GlobalScope globs = null)
  {
    globs = globs == null ? SymbolTable.CreateBuiltins() : globs;
    //NOTE: we want interpreter to work with original globs
    var globs_copy = globs.Clone();
    SharedInit();

    var conf = new BuildConf();
    conf.compile_fmt = CompileFormat.AST;
    conf.globs = globs;
    conf.files = test_files;
    conf.res_file = TestDirPath() + "/result.bin";
    conf.inc_dir = TestDirPath();
    conf.cache_dir = TestDirPath() + "/cache";
    conf.err_file = TestDirPath() + "/error.log";
    conf.use_cache = false;
    conf.debug = true;

    var bld = new Build();
    int res = bld.Exec(conf);
    if(res != 0)
      throw new UserError(File.ReadAllText(conf.err_file));

    var intp = Interpreter.instance;
    var bin = new MemoryStream(File.ReadAllBytes(conf.res_file));
    var mloader = new ModuleLoader(bin);

    intp.Init(globs_copy, mloader);

    return intp;
  }

  static void NodeDump(BehaviorTreeNode node)
  {
    Util.NodeDump(node);
  }

  public struct Result
  {
    public BHS status;
    public DynVal[] vals;

    public DynVal val 
    {
      get {
        return vals != null ? vals[0] : null;
      }
    }
  }

  static Result ExecNode(BehaviorTreeNode node, int ret_vals = 1, bool keep_running = true)
  {
    Result res = new Result();

    res.status = BHS.NONE;
    while(true)
    {
      res.status = node.run();
      if(res.status != BHS.RUNNING)
        break;

      if(!keep_running)
        break;
    }
    if(ret_vals > 0)
    {
      res.vals = new DynVal[ret_vals];
      for(int i=0;i<ret_vals;++i)
        res.vals[i] = Interpreter.instance.PopValue();
    }
    else
      res.vals = null;
    return res;
  }

  static Interpreter Interpret(string src, GlobalScope globs = null, bool show_ast = false)
  {
    globs = globs == null ? SymbolTable.CreateBuiltins() : globs;
    //NOTE: we want interpreter to work with original globs
    var globs_copy = globs.Clone();
    SharedInit();

    var intp = Interpreter.instance;
    intp.Init(globs_copy, null/*empty module loader*/);

    var mreg = new ModuleRegistry();
    //fake module for this specific case
    var mod = new bhl.Module("", "");
    var bin = new MemoryStream();
    Frontend.Source2Bin(mod, src.ToStream(), bin, globs, mreg);
    bin.Position = 0;

    var ast = Util.Bin2Meta<AST_Module>(bin);
    if(show_ast)
      Util.ASTDump(ast);

    intp.Interpret(ast);
  
    return intp;
  }

  void CommonChecks(Interpreter intp)
  {
    intp.glob_mem.Clear();

    //for extra debug
    //Console.WriteLine(DynVal.PoolDump());

    if(intp.stack.Count > 0)
    {
      Console.WriteLine("=== Dangling stack values ===");
      for(int i=0;i<intp.stack.Count;++i)
        Console.WriteLine("Stack value #" + i + " " + intp.stack[i]);
    }
    AssertEqual(intp.stack.Count, 0);
    AssertEqual(intp.node_ctx_stack.Count, 0);
    AssertEqual(DynVal.PoolCount, DynVal.PoolCountFree);
    AssertEqual(DynValList.PoolCount, DynValList.PoolCountFree);
    AssertEqual(DynValDict.PoolCount, DynValDict.PoolCountFree);
    AssertEqual(FuncCtx.PoolCount, FuncCtx.PoolCountFree);
  }

  static double ExtractNum(Result res)
  {
    return res.val.num;
  }

  static bool ExtractBool(Result res)
  {
    return res.val.bval;
  }

  static string ExtractStr(Result res)
  {
    return res.val.str;
  }

  static object ExtractObj(Result res)
  {
    return res.val.obj;
  }
}

