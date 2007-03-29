/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    /// <summary>
    /// The config class contain global switches for controlling the behaviour 
    /// of the interpreter and compiler. 
    /// </summary>
    static class Config
    {
        /// <summary>
        /// Controls whether inferred types will be output to the console.
        /// </summary>
        public static bool gbLogTypeInference = false;
        
        /// <summary>
        /// Controls whether interpreter will run unit tests on start-up
        /// </summary>
        public static bool gbUnitTesting = false;
        
        /// <summary>
        /// Turns on tests which may throw exceptions. This is useful
        /// to trun off when in debug mode, and exception handling is 
        /// done from within the IDE.
        /// </summary>
        public static bool gbUnitTestingWithExceptions = false;
        
        /// <summary>
        /// Forces the interpreter to run the failing test set. 
        /// </summary>
        public static bool gbTestKnownIssues = false;
        
        /// <summary>
        /// Controls how names are assigned to type variable declarations and type variables
        /// </summary>
        public static bool gbSimpleTypeNames = true;
        
        /// <summary>
        /// Controls whether or not to display the welcome text.
        /// </summary>
        public static bool gbShowLogo = true;
        
        /// <summary>
        /// Controls whether type checking and type inference is used. 
        /// If you turn this off, then no type checking is done.
        /// </summary>
        public static bool gbStaticTyping = false;

        /// <summary>
        /// Determines whether the contents of the stacks is reported 
        /// after each line entry into the interpreter.
        /// </summary>
        public static bool gbOutputStack = true;

        /// <summary>
        /// If false causes imports to echo each line to the console
        /// </summary>
        public static bool gbQuietImport = false;

        /// <summary>
        /// Output the amount of time elapsed after each entry in the interpreter.
        /// </summary>
        public static bool gbOutputTimeElapsed = false;

        /// <summary>
        /// Determines whether all of the output should saved to a log file.
        /// </summary>
        public static bool gbLogSession = false;

        /// <summary>
        /// The number of worker threads that the interpreter can spawn at one time. 
        /// </summary>
        public static int gnMaxWorkerThreads = 1;

        /// <summary>
        /// The number of completion port threads (?) that the interpreter can spawn at one time. 
        /// </summary>
        public static int gnMaxCompletionPortThreads = 0;

        /// <summary>
        /// Set this to false to prevent implicit redefining existing functions. 
        /// </summary>
        public static bool gbAllowImplicitRedefines = true;

        /// <summary>
        /// Set to false to only implement point-free Cat
        /// </summary>
        public static bool gbAllowNamedParams = true;

        /// <summary>
        /// Outputs the result of performing a conversion from a function
        /// with named arguments to a point-free form.
        /// </summary>
        public static bool gbShowPointFreeConversion = true;
    }
}
