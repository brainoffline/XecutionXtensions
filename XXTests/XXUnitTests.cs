using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XXLib;

namespace XXTests
{
    [TestClass]
    public class XXUnitTests
    {
        [TestInitialize]
        public void Init()
        {
            ShowThreadId();
        }

        private void ShowThreadId()
        {
            Console.WriteLine("ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
        }

        [TestMethod]
        public async Task RunSynchronously()
        {
            var value = await new XX<string>()
                .Execute(() =>
                {
                    ShowThreadId();
                    return "hello";
                });
            Console.WriteLine(value);
        }

        [TestMethod]
        public async Task RunAsynchronously()
        {
            var value = await new XX<string>()
                .ExecuteOnBackgroundThread(() =>
                {
                    ShowThreadId();
                    return "hello";
                });
            Console.WriteLine(value);
        }

        [TestMethod]
        public async Task WithError()
        {
            var value = await new XX<string>()
                .OnError(ex =>
                {
                    Console.WriteLine("Error: " + ex);
                })
                .Execute(() =>
                {
                    throw new Exception("Hello, Error");
                });

            Console.WriteLine(value);
        }

        [TestMethod]
        public async Task WithErrorOnBackgroundThread()
        {
            var value = await new XX<string>()
                .OnError(ex =>
                {
                    Console.WriteLine("Error: " + ex);
                })
                .ExecuteOnBackgroundThread(() =>
                {
                    ShowThreadId();
                    throw new Exception("Hello, Error");
                });

            ShowThreadId();
            Console.WriteLine(value);
        }
    }
}
