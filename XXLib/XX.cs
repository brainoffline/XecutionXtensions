using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XXLib
{
    public class XX<T> where T : class
    {
        public object Tag { get; set; }

        private int _maxRetryAttempts;
        private int _retryCount;
        private bool _cancelled;
        private Exception _lastException;
        private Action<Exception> _errorAction;
        private Action<T> _resultAction;
        private Action _timeoutAction;
        private Func<Exception, bool> _canRetryAction;
        private TimeSpan? _timeout;

        private SynchronizationContext _context;
        private CancellationTokenSource _cts;
        private TaskCompletionSource<T> _tcs;

        //private readonly Func<T> _action;

        public XX()
        {
            _context = SynchronizationContext.Current;
        }

        // COMPOSE
        public XX<T> RetryOnError(int retries)
        {
            _maxRetryAttempts = retries;
            return this;
        }

        public XX<T> Timeout(TimeSpan timespan, Action action)
        {
            _timeout = timespan;
            _timeoutAction = action;
            return this;
        }


        // EVENTS
        public XX<T> OnError(Action<Exception> action)
        {
            _errorAction = action;
            return this;
        }

        public XX<T> OnResult(Action<T> action)
        {
            _resultAction = action;
            return this;
        }

        public XX<T> CanReturn(Func<Exception, bool> action)
        {
            _canRetryAction = action;
            return this;
        }



        // MONITOR

        public Exception LastException => _lastException;
        public int RetriedCount => _retryCount;
        public bool Cancelled => _cancelled;

        public void Cancel()
        {
            _cts?.Cancel();
        }


        private CancellationTokenSource CTS => _cts ?? (_cts = new CancellationTokenSource());

        protected static void RunOn(SynchronizationContext context, Action action)
        {
            if (context == null || context == SynchronizationContext.Current)
                action?.Invoke();
            else
            {
                context.Post(obj =>
                {
                    action?.Invoke();
                }, null);
            }
        }

        public Task<T> ExecuteOnBackgroundThread(Func<T> action)
        {
            _context = SynchronizationContext.Current;

            return Task.Run(() => Execute(action), CTS.Token);
        }

        // EXECUTE
        public Task<T> Execute(Func<T> action)
        {
            _tcs = new TaskCompletionSource<T>();

            while (!_cancelled)
            {
                bool canRetry = false;
                try
                {
                    if (_timeout.HasValue)
                        CTS.CancelAfter(_timeout.Value);

                    T result = action();

                    if (_resultAction != null)
                        RunOn(_context, () => _resultAction?.Invoke(result));

                    _tcs.SetResult(result);
                    break;
                }
                catch (AggregateException ae)
                {
                    foreach (var ex in ae.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            _cancelled = true;
                            break;
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _cancelled = true;
                    _lastException = ex;

                    if (_timeoutAction != null)
                        RunOn(_context, () => _timeoutAction());
                }
                catch (Exception ex)
                {
                    //_tcs.SetException(ex);
                    _lastException = ex;

                    if (_errorAction != null)
                        RunOn(_context, () => _errorAction?.Invoke(ex));

                    if (_canRetryAction != null)
                        canRetry = _canRetryAction(ex);

                    _retryCount++;
                }

                if (canRetry)
                    continue;

                if (_cancelled || _retryCount > _maxRetryAttempts)
                {
                    _tcs.SetResult(null);
                    break;
                }
            }

            return _tcs.Task;
        }
    }
}
