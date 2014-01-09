using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Diagnostics;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;

namespace netmf_msbuild_parse
{
    // 引数：プロジェクトファイル
    class Program
    {
        /*
         * 
         */
        /// <summary>環境変数の設定(setenv_base.cmd, init.cmd を参考に)</summary>
        /// <remarks>
        /// 前提：
        ///   ターゲット gcc のバージョン 4.6.2
        ///   ターゲット gcc のターゲット arm-none-eabi
        ///   ターゲット gcc のインストール先 c:\gnu\gcc
        ///   Visual Studio 2010 を使用
        ///   gcc の存在確認はしているが，visual studio はしていないので注意
        /// </remarks>
        /// <returns>正常終了 true, 異常終了 false</returns>
        static bool setup_environments()
        {
            // setenv_base.cmd の引数として与えるパラメータ({GCC, 4.6.2，c:\gnu\gcc} できめうち)
            String compiler_tool = "GCC";
            String compiler_tool_version_num = "4.6.2";
            String arg3 = Environment.GetEnvironmentVariable("SystemDrive") + "\\gnu\\gcc";
            Debug.WriteLine("arg3 = " + arg3);

            Environment.SetEnvironmentVariable("COMPILER_TOOL", compiler_tool);
            Environment.SetEnvironmentVariable("COMPILER_TOOL_VERSION_NUM", compiler_tool_version_num);
            Environment.SetEnvironmentVariable("COMPILER_TOOL_VERSION",
                Environment.GetEnvironmentVariable("COMPILER_TOOL")
                + Environment.GetEnvironmentVariable("COMPILER_TOOL_VERSION_NUM"));
            Environment.SetEnvironmentVariable("ARG3", arg3);

            Environment.SetEnvironmentVariable("TFSCONFIG", "MFConfig.xml");

            if (Environment.GetEnvironmentVariable("COMPILER_TOOL").Equals("RVDS", StringComparison.OrdinalIgnoreCase) == false
                && Environment.GetEnvironmentVariable("COMPILER_TOOL").Equals("ADS", StringComparison.OrdinalIgnoreCase) == false)
            {
                Environment.SetEnvironmentVariable("NO_ADS_WRAPPER", "1");
            }

            // この辺から init.cmd 相当の処理
            Environment.SetEnvironmentVariable("CLRROOT", Directory.GetCurrentDirectory());

            Environment.SetEnvironmentVariable("FLAVOR_WIN", "Release");
            Environment.SetEnvironmentVariable("FLAVOR_DAT", "Release");
            Environment.SetEnvironmentVariable("FLAVOR_ARM", "Release");
            Environment.SetEnvironmentVariable("FLAVOR_PLATFORM", "iMXS");
            Environment.SetEnvironmentVariable("FLAVOR_MEMORY", "Flash");
            Environment.SetEnvironmentVariable("OEM_NAME", "Microsoft");
            Environment.SetEnvironmentVariable("FX_35", Environment.GetEnvironmentVariable("WINDIR") + "\\Microsoft.NET\\Framework\\v3.5");
            Environment.SetEnvironmentVariable("FX_40", Environment.GetEnvironmentVariable("WINDIR") + "\\Microsoft.NET\\Framework\\v4.0");
            Environment.SetEnvironmentVariable("MSBUILD_EXE", Environment.GetEnvironmentVariable("FX_40") + "\\msbuild.exe");

            Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("Path") + ";" + Environment.GetEnvironmentVariable("FX_40"));

            Environment.SetEnvironmentVariable("COMMON_BUILD_ROOT", Environment.GetEnvironmentVariable("CLRROOT"));
            Environment.SetEnvironmentVariable("BUILD_ROOT_BASE", Environment.GetEnvironmentVariable("COMMON_BUILD_ROOT") + "\\BuildOutput");
            Environment.SetEnvironmentVariable("BUILD_ROOT", Environment.GetEnvironmentVariable("BUILD_ROOT_BASE") + "\\public");

            Environment.SetEnvironmentVariable("BUILD_TREE", Environment.GetEnvironmentVariable("BUILD_ROOT") + "\\"
                + Environment.GetEnvironmentVariable("FLAVOR_DAT"));
            Environment.SetEnvironmentVariable("BUILD_TREE_CLIENT", Environment.GetEnvironmentVariable("BUILD_TREE") + "\\client");
            Environment.SetEnvironmentVariable("BUILD_TREE_SERVER", Environment.GetEnvironmentVariable("BUILD_ROOT") + "\\"
                + Environment.GetEnvironmentVariable("FLAVOR_WIN") + "\\" + "server");
            Environment.SetEnvironmentVariable("DEVPATH", Environment.GetEnvironmentVariable("BUILD_TREE_SERVER") + "\\dll");

            Environment.SetEnvironmentVariable("MDP_EXE", Environment.GetEnvironmentVariable("BUILD_TREE_SERVER") + "\\dll\\MetadataProcessor.exe");
            Environment.SetEnvironmentVariable("BHL_EXE", Environment.GetEnvironmentVariable("BUILD_TREE_SERVER") + "\\dll\\BuildHelper.exe");

            Environment.SetEnvironmentVariable("BUILD_TEST_ROOT", Environment.GetEnvironmentVariable("BUILD_ROOT") + "\\"
                + Environment.GetEnvironmentVariable("FLAVOR_DAT") + "\\Test");
            Environment.SetEnvironmentVariable("BUILD_TEST_TREE", Environment.GetEnvironmentVariable("BUILD_TEST_ROOT"));
            Environment.SetEnvironmentVariable("BUILD_TEST_TREE_CLIENT", Environment.GetEnvironmentVariable("BUILD_TEST_ROOT") + "\\client");
            Environment.SetEnvironmentVariable("BUILD_TEST_TREE_SERVER", Environment.GetEnvironmentVariable("BUILD_TEST_ROOT") + "\\server");

            Environment.SetEnvironmentVariable("OEM_PATH", Environment.GetEnvironmentVariable("OEM_ROOT") + "\\" + Environment.GetEnvironmentVariable("OEM_NAME"));
            Environment.SetEnvironmentVariable("CLRLIB", Environment.GetEnvironmentVariable("CLRROOT") + "\\Tools\\Libraries");

            Environment.SetEnvironmentVariable("TARGETCURRENT", Environment.GetEnvironmentVariable("CLRROOT") + "_BUILD\\arm\\"
                + Environment.GetEnvironmentVariable("FLAVOR_MEMORY") + "\\"
                + Environment.GetEnvironmentVariable("FLAVOR_ARM") + "\\"
                + Environment.GetEnvironmentVariable("FLAVOR_PLATFORM") + "\\bin");

            // ここから再度 setenv_base.cmd

            Environment.SetEnvironmentVariable("SPOCLIENT", Environment.GetEnvironmentVariable("CLRROOT"));
            Environment.SetEnvironmentVariable("SPOROOT", Path.GetFullPath(Environment.GetEnvironmentVariable("SPOCLIENT") + "\\.."));

            Environment.SetEnvironmentVariable("NetMfTargetsBaseDir", Environment.GetEnvironmentVariable("SPOCLIENT") + "\\Framework\\IDE\\Targets\\");

            Environment.SetEnvironmentVariable("_SDROOT_", Path.GetPathRoot(Directory.GetCurrentDirectory()).TrimEnd(new char[] { '\\' }));

            if (Environment.GetEnvironmentVariable("DOTNETMF_OLD_PATH") == null)
            {
                Environment.SetEnvironmentVariable("DOTNETMF_OLD_PATH", Environment.GetEnvironmentVariable("Path"));
            }
            else
            {
                Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("DOTNETMF_OLD_PATH"));
            }

            Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("SPOROOT") + "\\tools\\NUnit;"
                + Environment.GetEnvironmentVariable("SPOROOT") + "\\tools\\SDPack;"
                + Environment.GetEnvironmentVariable("SPOROOT") + "\\bin;"
                + Environment.GetEnvironmentVariable("Path"));

            Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("SPOROOT") + "\\tools\\x86\\perl\\bin;"
                + Environment.GetEnvironmentVariable("SPOROOT") + "\\tools\\x86\\bin;"
                + Environment.GetEnvironmentVariable("CLRROOT") + "\\tools\\bin;"
                + Environment.GetEnvironmentVariable("Path") + ";"
                + Environment.GetEnvironmentVariable("CLRROOT") + "\\tools\\scripts");

            Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("CLRROOT")
                + "\\BuildOutput\\Public\\" + Environment.GetEnvironmentVariable("FLAVOR_WIN") + "\\Test\\Server\\dll;"
                + Environment.GetEnvironmentVariable("Path"));


            // vsvars32.bat でセットしている環境変数．
            // Visual Studioの存在確認はしていない．
            Environment.SetEnvironmentVariable("VCINSTALLDIR", "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VC\\");
            Environment.SetEnvironmentVariable("VSINSTALLDIR", "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\");
            Environment.SetEnvironmentVariable("WindowsSdkDir", "C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v7.0A\\");

            Environment.SetEnvironmentVariable("Framework35Version", "v3.5");
            Environment.SetEnvironmentVariable("FrameworkDir", "C:\\Windows\\Microsoft.NET\\Framework\\");
            Environment.SetEnvironmentVariable("FrameworkDIR32", "C:\\Windows\\Microsoft.NET\\Framework\\");
            Environment.SetEnvironmentVariable("FrameworkVersion", "v4.0.30319");
            Environment.SetEnvironmentVariable("FrameworkVersion32", "v4.0.30319");
            Environment.SetEnvironmentVariable("DevEnvDir", "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\Common7\\IDE\\");
            Environment.SetEnvironmentVariable("INCLUDE", "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VC\\INCLUDE;C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v7.0A\\include;");
            Environment.SetEnvironmentVariable("LIB", "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VC\\LIB;C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v7.0A\\lib;");
            Environment.SetEnvironmentVariable("LIBPATH", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319;C:\\Windows\\Microsoft.NET\\Framework\\v3.5;c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VC\\LIB;");
            Environment.SetEnvironmentVariable("Path", "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VSTSDB\\Deploy"
                + ";" + "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\Common7\\IDE\\"
                + ";" + "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VC\\BIN"
                + ";" + "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\Common7\\Tools"
                + ";" + "c:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\VC\\VCPackages"
                + ";" + "C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v7.0A\\bin\\NETFX 4.0 Tools"
                + ";" + "C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v7.0A\\bin"
                + ";" + "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319"
                + ";" + "C:\\Windows\\Microsoft.NET\\Framework\\v3.5"
                + ";" + Environment.GetEnvironmentVariable("Path"));

            // 再び setenv_base.cmd
            Environment.SetEnvironmentVariable("TINYCLR_USE_MSBUILD", "1");
            Environment.SetEnvironmentVariable("GNU_VERSION", Environment.GetEnvironmentVariable("COMPILER_TOOL_VERSION_NUM"));
            Environment.SetEnvironmentVariable("COMPILER_TOOL_VERSION_NUM", Environment.GetEnvironmentVariable("COMPILER_TOOL_VERSION_NUM").Substring(0, 3));
            Environment.SetEnvironmentVariable("COMPILER_TOOL_VERSION", Environment.GetEnvironmentVariable("COMPILER_TOOL")
                + Environment.GetEnvironmentVariable("COMPILER_TOOL_VERSION_NUM").Substring(0, 3));
            Environment.SetEnvironmentVariable("DOTNETMF_COMPILER", Environment.GetEnvironmentVariable("COMPILER_TOOL_VERSION"));

            // gccが存在するかどうか
            if (Directory.Exists(Environment.GetEnvironmentVariable("ARG3") + "\\lib\\gcc\\arm-none-eabi\\"
                + Environment.GetEnvironmentVariable("GNU_VERSION")))
            {
                Environment.SetEnvironmentVariable("ARMINC", Environment.GetEnvironmentVariable("ARG3") + "\\lib\\gcc\\arm-none-eabi\\"
                + Environment.GetEnvironmentVariable("GNU_VERSION") + "\\include");
                Environment.SetEnvironmentVariable("ARMLIB", Environment.GetEnvironmentVariable("ARG3") + "\\lib\\gcc\\arm-none-eabi\\"
                + Environment.GetEnvironmentVariable("GNU_VERSION"));

                Environment.SetEnvironmentVariable("GNU_TOOLS", Environment.GetEnvironmentVariable("ARG3"));
                Environment.SetEnvironmentVariable("GNU_TOOLS_BIN", Environment.GetEnvironmentVariable("ARG3") + "\\bin");
                Environment.SetEnvironmentVariable("GNU_TARGET", "arm-none-eabi");
            }
            else
            {
                Debug.WriteLine("Could not found " + Environment.GetEnvironmentVariable("ARG3") + "\\lib\\gcc\\arm-none-eabi\\"
                + Environment.GetEnvironmentVariable("GNU_VERSION"));
                return false;
            }

            // 確認
            // foreach (String s in Environment.GetEnvironmentVariables().Keys){Debug.WriteLine(s + "=" + Environment.GetEnvironmentVariable(s));}

            return true;
        }

        static void Main(string[] args)
        {
            // for debug
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            // 引数チェック
            if (args.Length == 0)
            {
                Debug.WriteLine("build file is needed. exit.");
                return;
            }

            // PKのプロジェクトファイル
            String netmfPkBuildFile = Path.GetFileName(args[0]);
            // PKプロジェクトファイルが存在するディレクトリ
            String netmfPkBuildDir = Path.GetDirectoryName(Path.GetFullPath(args[0]));

            // 作業フォルダを設定
            Directory.SetCurrentDirectory(netmfPkBuildDir);

            Debug.WriteLine("Project file: " + netmfPkBuildFile);
            Debug.WriteLine("Build dir(working directory): " + netmfPkBuildDir);

            // 環境変数設定
            if (!setup_environments())
            {
                Debug.WriteLine("environment variable setup error");

                // 終了する
                return;
            }

            // プロジェクト
            Project testProj;

            try
            {
                testProj = new Project(netmfPkBuildFile);

                Debug.WriteLine("----- Import -----");
                Debug.WriteLine("Root Project File: " + netmfPkBuildFile);
                Debug.WriteLine("num of imported elements: " + testProj.Imports.Count);
                foreach (ResolvedImport ri in testProj.Imports)
                {
                    Debug.WriteLine("imported: " 
                        + ri.ImportedProject.FullPath
                        + "(Condition=" + ri.ImportingElement.Condition + ")");
                }

                Debug.WriteLine("----- Target -----");
                Debug.WriteLine("num of target:" + testProj.Targets.Count);
                foreach (String s in testProj.Targets.Keys)
                {
                    Debug.WriteLine(s + "=" + testProj.Targets[s]);
                }

            }
            catch (InvalidProjectFileException e)
            {
                Debug.WriteLine(e.ToString());
            }

            /*
            testProj.Xml.AddTarget("BuildProjects");
            foreach (ProjectTargetElement pti in testProj.Xml.Targets.Where(pti => pti.Name == "BuildProjects"))
            {
                pti.AddTask("MSBuild");
            }
            testProj.Save(@"C:\testProj.proj");
 */
        }
    }
}
