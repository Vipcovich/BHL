using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Mono.Options;

namespace bhl {

public class CompileCmd : ICmd
{
  const int ERROR_EXIT_CODE = 2;

  public static void Usage(string msg = "")
  {
    Console.WriteLine("Usage:");
    Console.WriteLine("bhl compile [-c <bhl.proj file>] [--dir=<src dirs separated with ;>] [--files=<file>] [--result=<result file>] " + 
                     "[--tmp-dir=<tmp dir>] [--error=<err file>] [--postproc-dll=<postproc dll path>] [-d] [--deterministic] [--module-fmt=<1,2>]");
    Console.WriteLine(msg);
    Environment.Exit(1);
  }

  public void Run(string[] args)
  {
    var files = new List<string>();

    var proj = new ProjectConf();

    var p = new OptionSet() {
      { "c", "project config file",
        v => { 
          proj = JsonConvert.DeserializeObject<ProjectConf>(File.ReadAllText(v));
          proj.proj_file = v;
        } },
      { "dir=", "source directories separated by ;",
        v => proj.src_dirs = v },
      { "files=", "file containing all source files list",
        v => files.AddRange(File.ReadAllText(v).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) },
      { "result=", "resulting file",
        v => proj.result_file = v },
      { "tmp-dir=", "tmp dir",
        v => proj.tmp_dir = v },
      { "C", "don't use cache",
        v => proj.use_cache = v == null },
      { "postproc-dll=", "posprocess dll file path",
        v => proj.postproc_dll_file = v },
      { "bindings-dll=", "bindings dll file path",
        v => proj.userbindings_dll_file = v },
      { "error=", "error file",
        v => proj.error_file = v },
      { "deterministic", "deterministic build (sorts files by name)",
        v => proj.deterministic = v != null },
      { "threads=", "number of threads",
          v => proj.max_threads = int.Parse(v) },
      { "d", "debug verbosity level",
        v => proj.verbosity = v != null ? 1 : 0 },
      { "module-fmt=", "binary module format",
        v => proj.module_fmt = (ModuleBinaryFormat)int.Parse(v) }
     };

    var extra = new List<string>();
    try
    {
      extra = p.Parse(args);
    }
    catch(OptionException e)
    {
      Usage(e.Message);
    }

    proj.Setup();

    files.AddRange(extra);

    for(int i=0;i<proj.inc_path.Count;++i)
      if(!Directory.Exists(proj.inc_path[i]))
        Usage("Source directory not found: " + proj.inc_path[i]);

    if(string.IsNullOrEmpty(proj.result_file))
      Usage("Result file path not set");

    if(string.IsNullOrEmpty(proj.tmp_dir))
      Usage("Tmp dir not set");

    IUserBindings userbindings = new EmptyUserBindings();
    if(!string.IsNullOrEmpty(proj.userbindings_dll_file))
    {
      var userbindings_assembly = System.Reflection.Assembly.LoadFrom(proj.userbindings_dll_file);
      var userbindings_class = userbindings_assembly.GetTypes()[0];
      userbindings = System.Activator.CreateInstance(userbindings_class) as IUserBindings;
      if(userbindings == null)
        Usage("User bindings are invalid");
    }

    IFrontPostProcessor postproc = new EmptyPostProcessor();
    if(!string.IsNullOrEmpty(proj.postproc_dll_file))
    {
      var postproc_assembly = System.Reflection.Assembly.LoadFrom(proj.postproc_dll_file);
      var postproc_class = postproc_assembly.GetTypes()[0];
      postproc = System.Activator.CreateInstance(postproc_class) as IFrontPostProcessor;
      if(postproc == null)
        Usage("User postprocessor is invalid");
    }

    if(files.Count == 0)
    {
      for(int i=0;i<proj.inc_path.Count;++i)
        CompilationExecutor.AddFilesFromDir(proj.inc_path[i], files);
    }

    for(int i=files.Count;i-- > 0;)
    {
      if(string.IsNullOrEmpty(files[i]))
        files.RemoveAt(i);
    }

    if(proj.deterministic)
      files.Sort();

    Console.WriteLine("BHL({2}) files: {0}, cache: {1}", files.Count, proj.use_cache, Version.Name);
    var conf = new CompileConf();
    conf.proj = proj;
    conf.args = string.Join(";", args);
    conf.self_file = GetSelfFile();
    conf.files = Util.NormalizeFilePaths(files);
    conf.userbindings = userbindings;
    conf.postproc = postproc;

    var cmp = new CompilationExecutor();
    var errors = cmp.Exec(conf);
    if(errors.Count > 0)
    {
      if(string.IsNullOrEmpty(proj.error_file))
      {
        foreach(var err in errors)
          ErrorUtils.OutputError(err.file, err.range.start.line, err.range.start.column, err.text);
      }
      Environment.Exit(ERROR_EXIT_CODE);
    }
  }

  public static string GetSelfFile()
  {
    return System.Reflection.Assembly.GetExecutingAssembly().Location;
  }
}

}
