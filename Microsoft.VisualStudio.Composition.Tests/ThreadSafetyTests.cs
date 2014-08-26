namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using MefV1 = System.ComponentModel.Composition;

    public class ThreadSafetyTests
    {
        /// <summary>
        /// Exercises code that relies on provisionalSharedObjects
        /// to break circular dependencies in a way that tries to force
        /// thread safety issues to show themselves.
        /// </summary>
        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(RootPartWithLazyImport), typeof(SomeOtherPart))]
        public void LazyPartRequestedAcrossMultipleThreads(IContainer container)
        {
            var testFailedCancellationSource = new CancellationTokenSource();
            var timeoutCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(testFailedCancellationSource.Token);
            timeoutCancellationSource.CancelAfter(TestUtilities.ExpectedTimeout);

            const int threads = 2;
            SomeOtherPart.ImportingConstructorBlockEvent.Reset();
            SomeOtherPart.ConstructorEnteredCountdown.Reset(threads);
            SomeOtherPart.CancellationToken = testFailedCancellationSource.Token;

            Task<SomeOtherPart>[] contrivedPartTasks = new Task<SomeOtherPart>[threads];
            for (int i = 0; i < threads; i++)
            {
                contrivedPartTasks[i] = Task.Run(delegate
                {
                    IRootPart part = container.GetExportedValue<IRootPart>();
                    SomeOtherPart getExtension = part.ImportingProperty;
                    return getExtension;
                });
                contrivedPartTasks[i].ContinueWith(t => testFailedCancellationSource.Cancel(), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }

            // Wait for all threads to have reached SomeOtherPart's constructor.
            // Then unblock them all to complete.
            try
            {
                SomeOtherPart.ConstructorEnteredCountdown.Wait(timeoutCancellationSource.Token);
                Console.WriteLine("All threads invoked the part constructor.");
                SomeOtherPart.ImportingConstructorBlockEvent.Set();
            }
            catch (OperationCanceledException)
            {
                // Rethrow any exceptions that caused this to be canceled.
                var exceptions = new AggregateException(contrivedPartTasks.Where(t => t.IsFaulted).Select(t => t.Exception));
                if (exceptions.InnerExceptions.Count > 0)
                {
                    testFailedCancellationSource.Cancel();
                    throw exceptions;
                }

                // A timeout is acceptable. It suggests the container
                // is threadsafe in a manner that does not allow a shared part's constructor
                // to be invoked multiple times.
                // Make sure it was in fact only invoked once.
                Assert.Equal(threads - 1, SomeOtherPart.ConstructorEnteredCountdown.CurrentCount);
                Console.WriteLine("Only one invocation of the part constructor was allowed.");

                // Signal to unblock the one constructor invocation that we have.
                SomeOtherPart.ImportingConstructorBlockEvent.Set();
            }

            // Verify that although the constructor was started multiple times,
            // we still ended up with just one shared part satisfying all the imports.
            for (int i = 1; i < threads; i++)
            {
                Assert.Same(contrivedPartTasks[0].Result, contrivedPartTasks[1].Result);
            }
        }

        /// <summary>
        /// Exercises code that relies on provisionalSharedObjects
        /// to break circular dependencies in a way that tries to force
        /// thread safety issues to show themselves.
        /// </summary>
        [MefFact(CompositionEngines.V1Compat | CompositionEngines.V2Compat, typeof(RootPartWithNonLazyImport), typeof(SomeOtherPart))]
        public void NonLazyPartRequestedAcrossMultipleThreads(IContainer container)
        {
            var testFailedCancellationSource = new CancellationTokenSource();
            var timeoutCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(testFailedCancellationSource.Token);
            timeoutCancellationSource.CancelAfter(TestUtilities.ExpectedTimeout);

            const int threads = 2;
            SomeOtherPart.ImportingConstructorBlockEvent.Reset();
            SomeOtherPart.ConstructorEnteredCountdown.Reset(threads);
            SomeOtherPart.CancellationToken = testFailedCancellationSource.Token;

            Task<SomeOtherPart>[] contrivedPartTasks = new Task<SomeOtherPart>[threads];
            for (int i = 0; i < threads; i++)
            {
                contrivedPartTasks[i] = Task.Run(delegate
                {
                    IRootPart part = container.GetExportedValue<IRootPart>();
                    SomeOtherPart getExtension = part.ImportingProperty;
                    return getExtension;
                });
                contrivedPartTasks[i].ContinueWith(t => testFailedCancellationSource.Cancel(), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }

            // Wait for all threads to have reached SomeOtherPart's constructor.
            // Then unblock them all to complete.
            try
            {
                SomeOtherPart.ConstructorEnteredCountdown.Wait(timeoutCancellationSource.Token);
                Console.WriteLine("All threads invoked the part constructor.");
                SomeOtherPart.ImportingConstructorBlockEvent.Set();
            }
            catch (OperationCanceledException)
            {
                // Rethrow any exceptions that caused this to be canceled.
                var exceptions = new AggregateException(contrivedPartTasks.Where(t => t.IsFaulted).Select(t => t.Exception));
                if (exceptions.InnerExceptions.Count > 0)
                {
                    testFailedCancellationSource.Cancel();
                    throw exceptions;
                }

                // A timeout is acceptable. It suggests the container
                // is threadsafe in a manner that does not allow a shared part's constructor
                // to be invoked multiple times.
                // Make sure it was in fact only invoked once.
                Assert.Equal(threads - 1, SomeOtherPart.ConstructorEnteredCountdown.CurrentCount);
                Console.WriteLine("Only one invocation of the part constructor was allowed.");

                // Signal to unblock the one constructor invocation that we have.
                SomeOtherPart.ImportingConstructorBlockEvent.Set();
            }

            // Verify that although the constructor was started multiple times,
            // we still ended up with just one shared part satisfying all the imports.
            for (int i = 1; i < threads; i++)
            {
                Assert.Same(contrivedPartTasks[0].Result, contrivedPartTasks[1].Result);
            }
        }

        public interface IRootPart
        {
            SomeOtherPart ImportingProperty { get; }
        }

        [Export(typeof(IRootPart))]
        [MefV1.Export(typeof(IRootPart)), MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class RootPartWithLazyImport : IRootPart
        {
            [Import, MefV1.Import]
            public Lazy<SomeOtherPart> ImportingPropertyLazy { get; set; }

            public SomeOtherPart ImportingProperty {
                get { return this.ImportingPropertyLazy.Value; }
            }
        }

        [Export(typeof(IRootPart))]
        [MefV1.Export(typeof(IRootPart)), MefV1.PartCreationPolicy(MefV1.CreationPolicy.NonShared)]
        public class RootPartWithNonLazyImport : IRootPart
        {
            [Import, MefV1.Import]
            public SomeOtherPart ImportingProperty { get; set; }
        }

        [Export, Shared]
        [MefV1.Export]
        public class SomeOtherPart
        {
            internal static readonly ManualResetEventSlim ImportingConstructorBlockEvent = new ManualResetEventSlim();
            internal static readonly CountdownEvent ConstructorEnteredCountdown = new CountdownEvent(0);
            internal static CancellationToken CancellationToken;

            [ImportingConstructor, MefV1.ImportingConstructor]
            public SomeOtherPart(IRootPart rootPart)
            {
                Assert.NotNull(rootPart);
                ConstructorEnteredCountdown.Signal();
                ImportingConstructorBlockEvent.Wait(CancellationToken);
            }
        }
    }
}
