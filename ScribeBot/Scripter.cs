﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using MoonSharp.Interpreter;
using System.Diagnostics;
using System.Threading.Tasks;
using ScribeBot.Wrappers.Types;

namespace ScribeBot
{
    /// <summary>
    /// Class creating and maintaining environment for Lua scripts.
    /// </summary>
    public static class Scripter
    {
        private static Script environment = new Script();

        private static Thread luaThread;

        /// <summary>
        /// Class instance containing MoonSharp scripting session.
        /// </summary>
        public static Script Environment { get => environment; set => environment = value; }

        /// <summary>
        /// Thread scripts are executed on.
        /// </summary>
        public static Thread LuaThread { get => luaThread; set => luaThread = value; }

        /// <summary>
        /// Manual initializer.
        /// </summary>
        public static void Initialize() => Core.WriteLine(new Color(205, 205, 205), "-- SCRIPTER INITIALIZED");

        /// <summary>
        /// Static constructor initializing and sharing all vital functionality with Lua environment.
        /// </summary>
        static Scripter()
        {
            Script.WarmUp();
            Script.GlobalOptions.RethrowExceptionNested = true;

            UserData.RegisterAssembly();

            Environment.Options.DebugPrint = value => Core.Write(new Color(0, 131, 63), value + System.Environment.NewLine);
            Environment.Options.CheckThreadAccess = false;
            Environment.Options.UseLuaErrorLocations = true;
            Environment.PerformanceStats.Enabled = true;

            Directory.GetFiles($@"Data\Extensions\", "*.lua").ToList().ForEach(x => Environment.DoFile(x));

            Environment.Globals["core"] = typeof(Wrappers.Core);
            Environment.Globals["input"] = typeof(Wrappers.Input);
            Environment.Globals["interface"] = typeof(Wrappers.Interface);
            Environment.Globals["screen"] = typeof(Wrappers.Screen);
            Environment.Globals["webdriver"] = typeof(Wrappers.Proxies.WebDriver);
            Environment.Globals["audio"] = typeof(Wrappers.Proxies.Audio);
        }

        /// <summary>
        /// Execute a string of code.
        /// </summary>
        /// <param name="code">String to execute.</param>
        /// <param name="silent">Defines whether console should hide code that's being executed.</param>
        public static void Execute(string code, bool silent = true)
        {
            Core.ConsoleInputQueue.Clear();

            if (!silent)
                Core.WriteLine(new Color(0, 131, 63), $"> {code}");

            if (LuaThread != null && LuaThread.IsAlive)
                LuaThread.Abort();

            LuaThread = new Thread(() =>
            {
                Core.WriteLine(Environment.PerformanceStats.GetPerformanceLog());

                try
                {
                    Environment.DoString($"{code}");
                }
                catch (SyntaxErrorException exception)
                {
                    Core.WriteLine(new Color(177, 31, 41), $"Syntax Error: {exception.Message}");
                }
                catch (ScriptRuntimeException exception)
                {
                    Core.WriteLine(new Color(177, 31, 41), $"Runtime Error: {exception.Message}");
                }
            })
            {
                Name = "Lua Thread",
                IsBackground = true
            };
            LuaThread.Start();
        }

        /// <summary>
        /// Adds a line to queue that can be processed via core.processConsoleInput.
        /// </summary>
        /// <param name="code">Code to process.</param>
        public static void InjectLine(string code)
        {
            if (LuaThread == null || !LuaThread.IsAlive)
            {
                try
                {
                    Environment.DoString($"{code}");
                }
                catch (SyntaxErrorException exception)
                {
                    Core.WriteLine(new Color(177, 31, 41), $"Syntax Error: {exception.Message}");
                }
                catch (ScriptRuntimeException exception)
                {
                    Core.WriteLine(new Color(177, 31, 41), $"Runtime Error: {exception.Message}");
                }

                return;
            }
            
            Core.ConsoleInputQueue.Add(code);
        }

        /// <summary>
        /// Stop Lua thread, effectively killing all running scripts.
        /// </summary>
        public static void Stop()
        {
            //Crude, but effective.
            if (LuaThread != null && LuaThread.IsAlive)
                LuaThread.Abort();
        }

        /// <summary>
        /// Suspend Lua thread/environment.
        /// </summary>
        public static void Suspend()
        {
            //
        }

        /// <summary>
        /// Resume Lua thread/environment.
        /// </summary>
        public static void Resume()
        {
            //
        }

        /// <summary>
        /// Get whether Lua thread/environment is paused.
        /// </summary>
        /// <returns>Whether Lua thread is suspended.</returns>
        public static bool IsPaused() => LuaThread?.ThreadState == System.Threading.ThreadState.WaitSleepJoin;
    }
}
