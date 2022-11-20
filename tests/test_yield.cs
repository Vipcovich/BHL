using System;
using bhl;

public class TestYield : BHL_TestBase
{
  [IsTested()]
  public void TestFuncWithYieldMustByAsync()
  {
    string bhl = @"
    func test() {
      yield()
    }
    ";

    AssertError<Exception>(
      delegate() { 
        Compile(bhl);
      },
      "function with yield calls must be async",
      new PlaceAssert(bhl, @"
    func test() {
----^"
      )
    );
  }

  [IsTested()]
  public void TestBasicYield()
  {
    string bhl = @"
    async func test()
    {
      yield()
    }
    ";

    var vm = MakeVM(bhl);
    vm.Start("test");
    AssertTrue(vm.Tick());
    AssertFalse(vm.Tick());
    CommonChecks(vm);
  }

  [IsTested()]
  public void TestYieldSeveralTimesAndReturnValue()
  {
    string bhl = @"
    async func int test()
    {
      yield()
      int a = 1
      yield()
      return a
    }
    ";

    var ts = new Types();

    var vm = MakeVM(bhl, ts);
    var fb = vm.Start("test");
    AssertTrue(vm.Tick());
    AssertTrue(vm.Tick());
    AssertFalse(vm.Tick());
    AssertEqual(fb.result.PopRelease().num, 1);
    CommonChecks(vm);
  }

  [IsTested()]
  public void TestYieldInParal()
  {
    string bhl = @"

    async func int test() 
    {
      int i = 0
      paral {
        while(i < 3) { yield() }
        while(true) {
          i = i + 1
          yield()
        }
      }
      return i
    }
    ";

    var vm = MakeVM(bhl);
    var fb = vm.Start("test");

    AssertTrue(vm.Tick());
    AssertTrue(vm.Tick());
    AssertTrue(vm.Tick());
    AssertFalse(vm.Tick());
    AssertEqual(3, fb.result.PopRelease().num);
    CommonChecks(vm);
  }

  [IsTested()]
  public void TestFuncWithYieldWhileMustByAsync()
  {
    string bhl = @"
    func test() {
      yield while(false)
    }
    ";

    AssertError<Exception>(
      delegate() { 
        Compile(bhl);
      },
      "function with yield calls must be async",
      new PlaceAssert(bhl, @"
    func test() {
----^"
      )
    );
  }

  [IsTested()]
  public void TestYieldWhileInParal()
  {
    string bhl = @"

    async func int test() 
    {
      int i = 0
      paral {
        yield while(i < 3)
        while(true) {
          i = i + 1
          yield()
        }
      }
      return i
    }
    ";

    var c = Compile(bhl);

    var vm = MakeVM(c);
    var fb = vm.Start("test");

    AssertTrue(vm.Tick());
    AssertTrue(vm.Tick());
    AssertTrue(vm.Tick());
    AssertFalse(vm.Tick());
    var val = fb.result.PopRelease();
    AssertEqual(3, val.num);
    CommonChecks(vm);
  }

  [IsTested()]
  public void TestSeveralYieldWhilesInParal()
  {
    string bhl = @"

    async func int test() 
    {
      int i = 0
      paral {
        yield while(i < 5)
        while(true) {
          yield while(i < 7)
        }
        while(true) {
          i = i + 1
          yield()
        }
      }
      return i
    }
    ";

    var vm = MakeVM(bhl);
    AssertEqual(5, Execute(vm, "test").result.PopRelease().num);
    CommonChecks(vm);
  }

  [IsTested()]
  public void TestYieldWhileBugInParal()
  {
    string bhl = @"
    async func Foo() {
      yield suspend()
    }

    async func test() {
      paral {
        yield while(true)

        while(true) {
          yield while(false)
          Foo()
        }
      }
    }
    ";

    var vm = MakeVM(bhl);
    var fb = vm.Start("test");
    for(int i=0;i<5;++i)
      AssertTrue(vm.Tick());
    //...will be running forever, well, we assume that :)
    vm.Stop(fb);
    CommonChecks(vm);
  }

  [IsTested()]
  public void TestFuncWithYieldFuncCallMustByAsync()
  {
    string bhl = @"
    func test() {
      yield suspend()
    }
    ";

    AssertError<Exception>(
      delegate() { 
        Compile(bhl);
      },
      "function with yield calls must be async",
      new PlaceAssert(bhl, @"
    func test() {
----^"
      )
    );
  }

  [IsTested()]
  public void TestSuspend()
  {
    string bhl = @"
    async func test()
    {
      yield suspend()
    }
    ";

    var ts = new Types();
    var c = Compile(bhl, ts);

    var expected = 
      new ModuleCompiler()
      .UseCode()
      .EmitThen(Opcodes.InitFrame, new int[] { 1 /*args info*/ })
      .EmitThen(Opcodes.CallNative, new int[] { ts.nfunc_index.IndexOf("suspend"), 0 })
      .EmitThen(Opcodes.ExitFrame)
      ;
    AssertEqual(c, expected);

    var vm = MakeVM(c);
    var fb = vm.Start("test");
    for(int i=0;i<99;i++)
      AssertTrue(vm.Tick());
    vm.Stop(fb);
    CommonChecks(vm);
  }
}
