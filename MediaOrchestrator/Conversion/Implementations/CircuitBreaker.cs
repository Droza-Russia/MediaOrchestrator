using System;
using System.Threading;

namespace MediaOrchestrator
{
    internal sealed class CircuitBreaker : IDisposable
    {
        private const int DefaultFailureThreshold = 3;
        private const int DefaultRecoveryTimeoutSeconds = 30;

        private readonly object _sync = new object();
        private readonly int _failureThreshold;
        private readonly int _recoveryTimeoutSeconds;

        private int _failureCount;
        private CircuitState _state;
        private DateTime _lastFailureTime;

        internal enum CircuitState
        {
            Closed,
            Open,
            HalfOpen
        }

        internal CircuitBreaker(int failureThreshold = DefaultFailureThreshold, int recoveryTimeoutSeconds = DefaultRecoveryTimeoutSeconds)
        {
            _failureThreshold = failureThreshold > 0 ? failureThreshold : DefaultFailureThreshold;
            _recoveryTimeoutSeconds = recoveryTimeoutSeconds > 0 ? recoveryTimeoutSeconds : DefaultRecoveryTimeoutSeconds;
            _state = CircuitState.Closed;
            _failureCount = 0;
            _lastFailureTime = DateTime.MinValue;
        }

        internal bool IsAllowed
        {
            get
            {
                lock (_sync)
                {
                    switch (_state)
                    {
                        case CircuitState.Closed:
                            return true;

                        case CircuitState.Open:
                            if (DateTime.UtcNow >= _lastFailureTime.AddSeconds(_recoveryTimeoutSeconds))
                            {
                                _state = CircuitState.HalfOpen;
                                return true;
                            }
                            return false;

                        case CircuitState.HalfOpen:
                            return true;

                        default:
                            return false;
                    }
                }
            }
        }

        internal void RecordSuccess()
        {
            lock (_sync)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }
        }

        internal void RecordFailure()
        {
            lock (_sync)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Open;
                }
                else if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                }
            }
        }

        internal CircuitState CurrentState
        {
            get
            {
                lock (_sync)
                {
                    if (_state == CircuitState.Open && DateTime.UtcNow >= _lastFailureTime.AddSeconds(_recoveryTimeoutSeconds))
                    {
                        return CircuitState.HalfOpen;
                    }
                    return _state;
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }
        }
    }
}