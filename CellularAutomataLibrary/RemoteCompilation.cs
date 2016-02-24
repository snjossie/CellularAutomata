using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace CellularAutomataLibrary
{
    /// <summary>
    /// An implementation of the Remote Compilation service.
    /// </summary>
    public class RemoteCompilation : IRemoteCompilation
    {
        /// <summary>
        /// The number of processors on this system.
        /// </summary>
        private readonly int processorCount = System.Environment.ProcessorCount;

        // Disable the "Not assigned to" warning, because this field will
        // be assigned a value by the Managed Extensibility Framework (MEF)
#pragma warning disable 649
        /// <summary>
        /// An instance of the transition function to use to update the board.
        /// </summary>
        [Import(AllowRecomposition = true)]
        private ITransition transFunc;
#pragma warning restore 649

        /// <summary>
        /// One of the two boards that will maintain the state of the cellular automaton between steps.
        /// </summary>
        private byte[][] board1;

        /// <summary>
        /// One of the two boards that will maintain the state of the cellular automaton between steps.
        /// </summary>
        private byte[][] board2;

        /// <summary>
        /// Indicates whether to update board1 (if true), or board2 (if false).
        /// </summary>
        private bool updateBoard1 = true;

        /// <summary>
        /// Initializes static members of the <see cref="RemoteCompilation"/> class.
        /// </summary>
        static RemoteCompilation()
        {
            // force the MefInterfaces assembly to be loaded by creating
            // a class that is part of that assembly
            ToroidalArray t = new ToroidalArray(1);
        }

        /// <summary>
        /// Compiles the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>A CompilerErrors indicating whether the compilation succeeded or the errors if it failed.</returns>
        public CompilerErrors Compile(String code)
        {
            var errors = new CompilerErrors();
            var provider = new CSharpCodeProvider();

            var compilerParams = new CompilerParameters();
            compilerParams.GenerateInMemory = true;
            compilerParams.GenerateExecutable = false;
#if DEBUG
            compilerParams.CompilerOptions = "/debug";
            compilerParams.IncludeDebugInformation = true;
#else
            compilerParams.CompilerOptions = "/optimize+"; // every little bit helps, right?
#endif

            var v = AppDomain.CurrentDomain
                             .GetAssemblies()
                             .Where(a => !a.IsDynamic)
                             .Select(a => a.Location);

            compilerParams.ReferencedAssemblies.AddRange(v.ToArray());

            var results = provider.CompileAssemblyFromSource(compilerParams, new String[] { code });

            if (results.Errors.HasErrors)
            {
                errors.IsSuccess = false;
                foreach (CompilerError err in results.Errors)
                {
                    if (err.IsWarning)
                    {
                        errors.Messages.Add(new CompilerMessage(err.Line, Severity.Warning, err.ErrorText));
                    }
                    else
                    {
                        errors.Messages.Add(new CompilerMessage(err.Line, Severity.Error, err.ErrorText));
                    }
                }
            }
            else
            {
                errors.IsSuccess = true;
            }

            if (errors.IsSuccess)
            {
                AssemblyCatalog catalog = new AssemblyCatalog(results.CompiledAssembly);
                CompositionContainer container = new CompositionContainer(catalog);

                try
                {
                    container.ComposeParts(this);
                }
                catch (ChangeRejectedException)
                {
                    errors.IsSuccess = false;
                    errors.Messages.Add(new CompilerMessage(0, Severity.Error, "The uploaded code does not implement the ITransition interface--only Transition functions that implement this interface can be executed."));
                }
            }

            return errors;
        }

        /// <summary>
        /// Initializes the specified initial board.
        /// </summary>
        /// <param name="initialBoard">The initial board from the client.</param>
        public void Initialize(byte[][] initialBoard)
        {
            updateBoard1 = true;
            board1 = initialBoard;
            board2 = new byte[initialBoard.Length][];
            for (int i = 0; i < initialBoard.Length; i++)
            {
                board2[i] = new byte[initialBoard.Length];
            }
        }

        /// <summary>
        /// Does one step of the cellular automaton.
        /// </summary>
        /// <returns>A dictionary of all of the changed states.</returns>
        public ConcurrentDictionary<Tuple<int, int>, byte> Step()
        {
            ConcurrentDictionary<Tuple<int, int>, byte> delta;
            try
            {
                if (updateBoard1)
                {
                    delta = Step(board1, board2);
                }
                else
                {
                    delta = Step(board2, board1);
                }
            }
            catch (Exception)
            {
                throw new FaultException<InvalidOperationFault>(new InvalidOperationFault("One or more exceptions happened inside the uploaded transition function"));
            }

            updateBoard1 = !updateBoard1;

            return delta;
        }

        /// <summary>
        /// Does one step of the cellular automaton.
        /// </summary>
        /// <param name="current">The current board.</param>
        /// <param name="next">The next board.</param>
        /// <returns>A dictionary of all of the changed states.</returns>
        private ConcurrentDictionary<Tuple<int, int>, byte> Step(byte[][] current, byte[][] next)
        {
            ConcurrentDictionary<Tuple<int, int>, byte> dict = new ConcurrentDictionary<Tuple<int, int>, byte>(processorCount, 20);

            if (transFunc == null)
            {
                var fault = new InvalidOperationFault("Cannot step until a function is uploaded");
                throw new FaultException<InvalidOperationFault>(fault);
            }

            ToroidalArray toroid = new ToroidalArray(current);
            Parallel.For(0, current.Length, i =>
            {
                for (int j = 0; j < current[i].Length; j++)
                {
                    byte val = transFunc.TransitionFunction(toroid, i, j);
                    if (current[i][j] != val)
                    {
                        dict.GetOrAdd(Tuple.Create(i, j), val);
                    }

                    next[i][j] = val;
                }
            });

            return dict;
        }
    }
}
