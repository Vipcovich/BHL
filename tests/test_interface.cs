using System;
using System.IO;
using System.Text;
using bhl;

public class TestInterfaces : BHL_TestBase
{
  [IsTested()]
  public void TestEmptyUserInterface()
  {
    string bhl = @"
    interface Foo { }
    ";

    var vm = MakeVM(bhl);
    var symb = vm.Types.Resolve("Foo") as InterfaceSymbolScript;
    AssertTrue(symb != null);
  }

  [IsTested()]
  public void TestUserInterfaceWithMethod()
  {
    string bhl = @"
    interface Foo { 
      func hey(int a, float b)
    }
    ";

    var vm = MakeVM(bhl);
    var symb = vm.Types.Resolve("Foo") as InterfaceSymbolScript;
    AssertTrue(symb != null);
    var hey = symb.FindMethod("hey").GetSignature();
    AssertTrue(hey != null);
    AssertEqual(2, hey.arg_types.Count);
    AssertEqual(Types.Int, hey.arg_types[0].Get());
    AssertEqual(Types.Float, hey.arg_types[1].Get());
    AssertEqual(Types.Void, hey.ret_type.Get());
  }

  [IsTested()]
  public void TestUserInterfaceWithSeveralMethods()
  {
    string bhl = @"
    class Bar { 
    }

    interface Foo { 
      func bool hey(int a, float b)
      func Bar,int bar(string s)
    }
    ";

    var vm = MakeVM(bhl);
    var symb = vm.Types.Resolve("Foo") as InterfaceSymbolScript;
    AssertTrue(symb != null);

    var hey = symb.FindMethod("hey").GetSignature();
    AssertTrue(hey != null);
    AssertEqual(2, hey.arg_types.Count);
    AssertEqual(Types.Int, hey.arg_types[0].Get());
    AssertEqual(Types.Float, hey.arg_types[1].Get());
    AssertEqual(Types.Bool, hey.ret_type.Get());

    var bar = symb.FindMethod("bar").GetSignature();
    AssertTrue(bar != null);
    AssertEqual(1, bar.arg_types.Count);
    AssertEqual(Types.String, bar.arg_types[0].Get());
    var tuple = (TupleType)bar.ret_type.Get();
    AssertEqual(2, tuple.Count);
    AssertEqual("Bar", tuple[0].name);
    AssertEqual(Types.Int, tuple[1].Get());
  }

  [IsTested()]
  public void TestUserInterfaceInheritance()
  {
    string bhl = @"
    class Bar { 
    }

    interface Wow 
    {
      func Bar,int bar(string s)
    }

    interface Foo : Wow { 
      func bool hey(int a, float b)
    }
    ";

    var vm = MakeVM(bhl);
    var symb = vm.Types.Resolve("Foo") as InterfaceSymbolScript;
    AssertTrue(symb != null);

    var hey = symb.FindMethod("hey").GetSignature();
    AssertTrue(hey != null);
    AssertEqual(2, hey.arg_types.Count);
    AssertEqual(Types.Int, hey.arg_types[0].Get());
    AssertEqual(Types.Float, hey.arg_types[1].Get());
    AssertEqual(Types.Bool, hey.ret_type.Get());

    var bar = symb.FindMethod("bar").GetSignature();
    AssertTrue(bar != null);
    AssertEqual(1, bar.arg_types.Count);
    AssertEqual(Types.String, bar.arg_types[0].Get());
    var tuple = (TupleType)bar.ret_type.Get();
    AssertEqual(2, tuple.Count);
    AssertEqual("Bar", tuple[0].name);
    AssertEqual(Types.Int, tuple[1].Get());
  }

  [IsTested()]
  public void TestUserInterfaceMethodDefaultValuesNotAllowed()
  {
    string bhl = @"
    interface Foo { 
      func hey(int a, float b = 1)
    }
    ";

    AssertError<Exception>(
      delegate() { 
        Compile(bhl);
      },
      "default value is not allowed"
    );
  }

  [IsTested()]
  public void TestClassDoesntImplementInterface()
  {
    {
      string bhl = @"
      interface IFoo { 
        func int bar(int i)
      }
      class Foo : IFoo {
      }
      ";
      AssertError<Exception>(
        delegate() { 
          Compile(bhl);
        },
        "class 'Foo' doesn't implement interface 'IFoo' method 'func int bar(int)'"
      );
    }

    {
      string bhl = @"
      interface IFoo { 
        func int bar(int i)
      }
      class Foo : IFoo {
        func bar(int i) { } 
      }
      ";
      AssertError<Exception>(
        delegate() { 
          Compile(bhl);
        },
        "class 'Foo' doesn't implement interface 'IFoo' method 'func int bar(int)'"
      );
    }

    {
      string bhl = @"
      interface IFoo { 
        func int,string bar(int i)
      }
      class Foo : IFoo {
        func int,int bar(int i) { 
          return 1,2
        } 
      }
      ";
      AssertError<Exception>(
        delegate() { 
          Compile(bhl);
        },
        "class 'Foo' doesn't implement interface 'IFoo' method 'func int,string bar(int)'"
      );
    }

    {
      string bhl = @"
      interface IFoo { 
        func int,string bar(int i)
      }

      class Foo {
      }

      func foo(IFoo ifoo) { 
      }

      func test() {
        Foo f = {} 
        foo(f)
      }
      ";
      AssertError<Exception>(
        delegate() { 
          Compile(bhl);
        },
        "incompatible types"
      );
    }
  }

  [IsTested()]
  public void TestClassImplementsInterface()
  {
    {
      string bhl = @"
      interface IFoo { 
        func int bar(int i)
      }
      class Foo : IFoo {
        func int bar(int i) {
          return i
        }
      }
      ";
      Compile(bhl);
    }

    {
      string bhl = @"
      interface IFoo { 
        func int,string bar(int i)
      }
      class Foo : IFoo {
        func int,string bar(int i) {
          return i,""foo""
        }
      }
      ";
      Compile(bhl);
    }
  }

  [IsTested()]
  public void TestCallByInterfaceMethod()
  {
    {
      string bhl = @"
      interface IFoo { 
        func int bar(int i)
      }
      class Foo : IFoo {
        func int bar(int i) {
          return i
        }
      }

      func int call(IFoo f, int i) {
        return f.bar(i)
      }

      func int test() {
        Foo f = {}
        return call(f, 42)
      }
      ";
      var vm = MakeVM(bhl);
      AssertEqual(42, Execute(vm, "test").result.PopRelease().num);
      CommonChecks(vm);
    }
  }
}
